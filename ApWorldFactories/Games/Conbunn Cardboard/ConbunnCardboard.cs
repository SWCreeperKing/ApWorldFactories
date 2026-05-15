using ApWorldFactories.Games.ConbunnCardboard;
using ApWorldFactories.Graphviz;
using CreepyUtil.Archipelago.WorldFactory;
using static ApWorldFactories.PathConstants;
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
    public override string WorldVersion => "0.1.2";

    private RegionRowData[] RegionData = [];
    private ConnectionRowData[] ConnectionRowData = [];
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
           .ReadTable(out RegionData).SkipColumn()
           .ReadTable(out ConnectionRowData).SkipColumn()
           .ReadTable(out LocationData).SkipColumn()
           .ReadTable(out AbilityData).SkipColumn()
           .ReadTable(out SkinData);

        RegionData = RegionData.Where(data => data.Region is not "Menu").ToArray();
        RegionMap = RegionData.ToDictionary(data => data.Region, data => data.RegionName);

        Unlocks = ConnectionRowData.Where(data => data.HasTransition).DistinctBy(data => data.To).ToDictionary(
            data => data.To, data => $"Transition Unlock: {RegionMap.GetValueOrDefault(data.To, data.To)}"
        );

        foreach (var location in LocationData)
        {
            if (!Counter.ContainsKey(location.Region)) Counter[location.Region] = [1, 1];
            LocationIdMap[location.Id]
                = $"{location.Region} {(location.IsCoin ? "Coin" : "CD")} #{Counter[location.Region][location.IsCoin ? 0 : 1]++}";
        }

        WriteData("LocationIds", LocationIdMap.Select(kv => $"{kv.Key}:{kv.Value}"));
        WriteData(
            "TransitionIds",
            ConnectionRowData.Where(data => data.HasTransition).Select(data => $"{data.TransitionName}:{RegionMap.GetValueOrDefault(data.To, data.To)}")
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
           .AddLocations("npcs", ["Talk to the Museum Book", "Cardbun Museum"])
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
           .AddCompoundLogicFunction("unlock", "has_unlock", "has[f\"Transition Unlock: {transition}\"]", "transition")
           .AddCompoundLogicFunction("cablecar", "has_cable_car", "has['Cable Car']")
           .AddCompoundLogicFunction("dash", "has_dash", "has['Dash']")
           .AddCompoundLogicFunction("pads", "has_bounce_pads", "has['Bounce Pads']")
           .AddCompoundLogicFunction("coin", "has_coin_count", "hasN['Real Coin', amt]", "amt")
           .AddLogicRules(LocationData.ToDictionary(data => LocationIdMap[data.Id], data => data.GenRule()))
           .AddLogicRules(SkinData.ToDictionary(data => data.Name, data => data.GenRule(RegionMap)));
    }

    public override void Regions(WorldFactory _, RegionFactory region_fact)
    {
        region_fact.AddRegions("", RegionData.Select(data => data.Region).ToArray())
                   .ForEachOf(
                        ConnectionRowData, (b, data) =>
                        {
                            var rule = data.GenRule(RegionMap);
                            var to = RegionMap.GetValueOrDefault(data.To, data.To);
                            if (rule is not "") b.AddConnectionCompiledRule(data.From, to, rule);
                            else b.AddConnection(data.From, to);
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
           .UseSetRules(method => method.AddCode(
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

    public override string GenerateGraphViz(WorldFactory worldFactory, Dictionary<string, string> associations,
        Func<string, string> getRule,
        string[][] locationDoubleArrays)
    {
        return new GraphBuilder(GameName)
              .ForEachOf(ConnectionRowData, (b, data) =>
                   {
                       b.AddConnection(data.From, RegionMap.GetValueOrDefault(data.To, data.To), data.GenRule(RegionMap));
                   }
               )
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