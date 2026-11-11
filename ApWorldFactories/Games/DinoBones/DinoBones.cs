using CreepyUtil.Archipelago.WorldFactory;
using WorldBuilder;

namespace ApWorldFactories.Games.DinoBones;

public class DinoBones : BuildData
{
    public override string SteamDirectory => PathConstants.DDrive;
    public override string ModFolderName => "SW_CreeperKing.DinoBonipelago";
    public override string GameName => "DinoBones Tidy Up the Museum";
    public override string ApWorldName => "dinobones_tidy_up_the_museum";
    public override string WorldVersion => "0.1.0";
    public override LocalFillSettings LocalFillSettings => new(80, 55, 100);

    public static int[] CrateNumbers = [];
    public static BoneLabel[] Labels = [];
    public static Dictionary<string, int> BoneToCrate = [];

    public override void RunShenanigans()
    {
        Labels = ReadData("Dinobones", s => new BoneLabel(s.Split('|')));
        var paths = ReadData("DinobonePaths", s => new BonePath(s.Split('|')));

        BoneToCrate = paths.ToDictionary(b => b.Label.Name, b => b.CrateNumber);
        CrateNumbers = [.. Enumerable.Range(1, BoneToCrate.Values.Max())];

        WriteData("bonepaths", paths.Select(p => $"{p.Crate}|{p.Object}").DistinctBy(s => s));
        WriteData("bonecrates", BoneToCrate.Select(kv => $"{kv.Key}|{kv.Value}"));
    }

    public override void Locations(LocationFactory location_fact)
    {
        location_fact
           .AddLocations(
                "bones_pallet",
                Labels.Where(l => !BoneToCrate.ContainsKey(l.Name)).Select(b => (string[])[b.Name, "Menu",])
            )
           .AddLocations(
                "bones_crate",
                Labels.Where(l => BoneToCrate.ContainsKey(l.Name))
                      .Select(b => (string[])
                           [
                               $"{b.Name} (Crate {BoneToCrate[b.Name]})", $"Crate {BoneToCrate[b.Name]}",
                           ]
                       )
            );
    }

    public override void Items(ItemFactory item_fact)
    {
        item_fact
           .AddItemListVariable(
                "crates", ItemFactory.ItemClassification.Progression, list: [.. CrateNumbers.Select(i => $"Crate {i}")]
            )
           .AddItem("Dust", ItemFactory.ItemClassification.Filler)
           .AddCreateItems(method =>
                method.CreateItemsFromList("crates")
                      .BalanceLocalFill()
                      .FillRemainingWithItem("Dust")
            );
    }

    public override void Rules(RuleFactory rule_fact)
    {
        rule_fact.AddCompoundLogicFunction("crate", "has_crate", "has[f\"Crate {num}\"]", "num");
    }

    public override void Regions(RegionFactory region_fact)
    {
        region_fact
           .ForEachOf(
                CrateNumbers,
                i => region_fact.AddRegion($"Crate {i}").AddConnectionCompiledRule("Menu", $"Crate {i}", $"crate[{i}]")
            )
           .AddLocationsFromList("bones_pallet", isLocalFillable: false)
           .AddLocationsFromList("bones_crate");
    }

    public override void Init(WorldInitFactory init_fact)
    {
        init_fact
           .UseItemGroups(new Dictionary<string, string> { ["crates"] = "crates" })
           .AddUseUniversalTrackerPassthrough(yamlNeeded: false)
           .UseSetRules(method => method.CreateGoalCompiledCondition(
                    string.Join(" and ", CrateNumbers.Select(i => $"crate[{i}]"))
                )
            )
           .UsePreFill(method => method.CreateLocalFill())
           .UseFillSlotData(
                new Dictionary<string, string> { ["uuid"] = "str(shuffled)" },
                method => method.CreateUniqueId()
            )
           .UseGetFillerItemName("Dust")
           .InjectCodeIntoWorld(world => world.AddVariable(new Variable("gen_puml", "False")))
           .UseGenerateOutput(method => method.PumlGenCode());
    }

    public override GraphBuilder GenerateGraphViz(WorldFactory worldFactory, GraphBuilder builder) => builder;
}