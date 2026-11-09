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

    public static int[] CrateNumbers = [];
    public static BoneLabel[] Labels = [];
    public static Dictionary<string, BoneLabel[]> DinoBoneDict = [];
    public static Dictionary<string, int> BoneToCrate = [];

    public override void RunShenanigans()
    {
        Labels = ReadData("Dinobones", s => new BoneLabel(s.Split('|')));
        var paths = ReadData("DinobonePaths", s => new BonePath(s.Split('|')));

        DinoBoneDict = Labels.GroupBy(b => b.Species).ToDictionary(g => g.Key, g => g.ToArray());
        BoneToCrate = paths.ToDictionary(b => b.Label.Name, b => b.CrateNumber);
        CrateNumbers = [.. Enumerable.Range(1, BoneToCrate.Values.Max())];

        WriteData("bonepaths", paths.Select(p => $"{p.Crate}|{p.Object}").DistinctBy(s => s));
    }

    public override void Locations(WorldFactory _, LocationFactory location_fact)
    {
        location_fact.AddLocations(
            "bones",
            Labels.Select(b => (string[])
                [
                    b.Name, BoneToCrate.TryGetValue(b.Name, out var region) ? $"Crate {region}" : "Menu"
                ]
            )
        );
    }

    public override void Items(WorldFactory _, ItemFactory item_fact)
    {
        item_fact
           .AddItemListVariable(
                "crates", ItemFactory.ItemClassification.Progression, list: [.. CrateNumbers.Select(i => $"Crate {i}")]
            )
           .AddItem("Dust", ItemFactory.ItemClassification.Filler)
           .AddCreateItems(method => method.CreateItemsFromList("crates").FillRemainingWithItem("Dust"));
    }

    public override void Rules(WorldFactory _, RuleFactory rule_fact)
    {
        rule_fact.AddCompoundLogicFunction("crate", "has_crate", "has[f\"Crate {num}\"]", "num");
    }

    public override void Regions(WorldFactory _, RegionFactory region_fact)
    {
        region_fact
           .ForEachOf(
                CrateNumbers,
                i => region_fact.AddRegion($"Crate {i}").AddConnectionCompiledRule("Menu", $"Crate {i}", $"crate[{i}]")
            )
           .AddLocationsFromList("bones");
    }

    public override void Init(WorldFactory world_fact, WorldInitFactory init_fact)
    {
        init_fact
           .UseInitFunction()
           .AddUseUniversalTrackerPassthrough(yamlNeeded: false)
           .UseCreateRegions()
           .AddCreateItems()
           .UseSetRules(method => method.Raw_CreateGoalCompiledCondition(
                    string.Join(" and ", CrateNumbers.Select(i => $"crate[{i}]"))
                )
            )
           .UseFillSlotData(
                new Dictionary<string, string> { ["uuid"] = "str(shuffled)" },
                method => method.CreateUniqueId()
            )
           .UseGetFillerItemName("Dust")
           .InjectCodeIntoWorld(world => world.AddVariable(new Variable("gen_puml", "False")))
           .UseGenerateOutput(method => method.PumlGenCode());
    }

    public override string GenerateGraphViz(WorldFactory worldFactory, Dictionary<string, string> associations,
        Func<string, string> getRule,
        string[][] locationDoubleArrays)
        => new GraphBuilder(GameName)
          .ForEachOf(
               CrateNumbers,
               (b, i) => b.AddConnection("Menu", $"Crate {i}", $"crate[{i}]")
           )
          .AddLocationsFromDoubleArray(locationDoubleArrays, getRule)
          .GenString();
}