using ApWorldFactories.Games.ConbunnCardboard;
using ApWorldFactories.Graphviz;
using CreepyUtil.Archipelago.WorldFactory;
using static CreepyUtil.Archipelago.WorldFactory.ItemFactory.ItemClassification;
using static CreepyUtil.Archipelago.WorldFactory.PremadePython;
using Range = CreepyUtil.Archipelago.WorldFactory.Range;

namespace ApWorldFactories.Games.Conbunn_Cardboard;

public class ConbunnCardboard : BuildData
{
    public override string SteamDirectory => FDrive;
    public override string ModFolderName => "SW_CreeperKing.Conbunnipelago";
    public override string GameName => "Conbunn Cardboard";
    public override string ApWorldName => "conbunn_cardboard";
    public override string GoogleSheetId => "1T4Gk3olQCz_J6dkZXPU1BtXysywf3wvbPeJHVYvnYic";
    public override string WorldVersion => "0.1.1";

    private Games.ConbunnCardboard.RegionData[] RegionData = [];
    private LocData[] LocationData = [];
    private AbilityData[] AbilityData = [];
    private SkinData[] SkinData = [];

    private Dictionary<string, string> Unlocks = [];
    private Dictionary<string, int[]> Counter = [];
    private Dictionary<string, string> LocationIdMap = [];
    private Dictionary<string, string> RegionMap = [];

    public override void RunShenanigans()
    {
        GetSpreadsheet()
           .ReadTable(new RegionDataCreator(), out RegionData)
           .ReadTable(out LocationData).SkipColumn()
           .ReadTable(out AbilityData).SkipColumn()
           .ReadTable(out SkinData);

        Unlocks = RegionData.Where(data => data.HasTransition).ToDictionary(
            data => data.Region, data => $"Transition Unlock: {data.Region}"
        );
        RegionMap = RegionData.ToDictionary(data => data.RawRegion, data => data.Region);

        foreach (var location in LocationData)
        {
            if (!Counter.ContainsKey(location.Region)) Counter[location.Region] = [1, 1];
            LocationIdMap[location.Id]
                = $"{location.Region} {(location.IsCoin ? "Coin" : "CD")} #{Counter[location.Region][location.IsCoin ? 0 : 1]++}";
        }

        WriteData("LocationIds", LocationIdMap.Select(kv => $"{kv.Key}:{kv.Value}"));
        WriteData(
            "TransitionIds",
            RegionData.Where(data => data.HasTransition).Select(data => $"{data.TransitionName}:{data.Region}")
        );
        WriteData(
            "LocationDoors", RegionData.Where(data => data.HasDoor).Select(data => $"{data.Region},{data.DoorFrame}")
        );
        WriteData(
            "SkinData",
            SkinData.OrderBy(data => int.Parse(data.Id)).Select(data => $"{data.Name},{data.LocalInt},{data.GloablInt}")
        );
    }

    public override void Options(WorldFactory _, OptionsFactory options_fact)
    {
        options_fact
           .AddOption("CDs Required To Goal", "The amount of CDs required to goal", new Range(25, 5, 40))
           .AddCheckOptions(method => method.AddCode(CreateMinimalCatch(GameName)));
    }

    public override void Locations(WorldFactory _, LocationFactory location_fact)
    {
        location_fact
           .AddLocations(
                "coins",
                LocationData.Where(data => data.IsCoin)
                            .Select(data => (string[])[LocationIdMap[data.Id], RegionMap[data.Region]])
            )
           .AddLocations(
                "cds",
                LocationData.Where(data => !data.IsCoin)
                            .Select(data => (string[])[LocationIdMap[data.Id], RegionMap[data.Region]])
            )
           .AddLocations("skins", SkinData.Select(data => (string[])[data.Name, RegionMap[data.Region]]));
    }

    public override void Items(WorldFactory _, ItemFactory item_fact)
    {
        item_fact
           .AddItemListVariable("unlocks", Progression, list: Unlocks.Values.ToArray())
           .AddItemListVariable("abilities", Progression, list: AbilityData.Select(data => data.Name).ToArray())
           .AddItemCountVariable("CDs", new Dictionary<string, int> { ["CD"] = 40 }, Progression)
           .AddItem("Cardboard Coin", Filler)
           .AddCreateItems(method => method.AddCode(CreateItemsFromList("unlocks"))
                                           .AddCode(CreateItemsFromList("abilities"))
                                           .AddCode(CreateItemsFromMapCountGenCode("CDs"))
                                           .AddCode(CreateItemsFillRemainingWithItem("Cardboard Coin"))
            );
    }

    public override void Rules(WorldFactory _, RuleFactory rule_fact)
    {
        rule_fact
           .AddLogicFunction(
                "unlock", "has_unlock", StateHas("f\"Transition Unlock: {transition}\"", stringify: false),
                "transition"
            )
           .AddLogicFunction("dash", "has_dash", StateHas("Dash"))
           .AddLogicFunction("pads", "has_bounce_pads", StateHas("Bounce Pads"))
           .AddLogicFunction("coin", "has_coin_count", StateHas("Real Coin", "amt"), "amt")
           .AddLogicRules(LocationData.ToDictionary(data => LocationIdMap[data.Id], data => data.GenRule))
           .AddLogicRules(SkinData.ToDictionary(data => data.Name, data => data.GenRule(RegionMap)));
    }

    public override void Regions(WorldFactory _, RegionFactory region_fact)
    {
        region_fact.AddRegions(RegionData.Select(data => data.Region).ToArray())
                   .ForEachOf(
                        RegionData, (b, data) =>
                        {
                            var rule = data.GenRule;
                            if (rule is not "") b.AddConnectionCompiledRule(data.BackRegion, data.Region, rule);
                            else b.AddConnection(data.BackRegion, data.Region);
                        }
                    )
                   .AddLocationsFromList("coins")
                   .AddLocationsFromList("cds")
                   .AddLocationsFromList("skins")
                   .AddEventLocationsFromList("coins", item: "\"Real Coin\"");
    }

    public override void Init(WorldFactory _, WorldInitFactory init_fact)
    {
        init_fact
           .UseInitFunction()
           .AddUseUniversalTrackerPassthrough(yamlNeeded: false)
           .UseCreateRegions()
           .AddCreateItems()
           .UseSetRules(method
                => method.AddCode(
                    CreateGoalCondition(StateHas("CD", "self.options.cds_required_to_goal", returnValue: false))
                )
            )
           .UseFillSlotData(
                new Dictionary<string, string> { ["uuid"] = "str(shuffled)" },
                method => method.AddCode(CreateUniqueId())
            )
           .InjectCodeIntoWorld(world => world.AddVariable(new Variable("gen_puml", "False")))
           .UseGenerateOutput(method => method.AddCode(PumlGenCode()));
    }

    public override string GenerateGraphViz(
        WorldFactory worldFactory, Dictionary<string, string> associations, Func<string, string> getRule,
        string[][] locationDoubleArrays
    )
    {
        return new GraphBuilder(GameName)
              .ForEachOf(RegionData, (b, data) => b.AddConnection(data.BackRegion, data.Region, data.GenRule))
              .AddLocationsFromDoubleArray(locationDoubleArrays, getRule).ForEachOf(
                   LocationData.Where(data => data.IsCoin),
                   (b, data) => b.AddEventLocation(
                       RegionMap[data.Region], getRule, $"Event: {LocationIdMap[data.Id]}", LocationIdMap[data.Id],
                       "Real Coin"
                   )
               )
              .GenString();
    }
}