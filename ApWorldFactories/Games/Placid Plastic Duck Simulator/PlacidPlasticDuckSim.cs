using ApWorldFactories.Graphviz;
using CreepyUtil.Archipelago.WorldFactory;
using static ApWorldFactories.PathConstants;
using static CreepyUtil.Archipelago.WorldFactory.ItemFactory.ItemClassification;
using static CreepyUtil.Archipelago.WorldFactory.PremadePython;

namespace ApWorldFactories.Games.Placid_Plastic_Duck_Simulator;

public class PlacidPlasticDuckSim : BuildData
{
    public override string SteamDirectory => DDrive;
    public override string ModFolderName => "SW_CreeperKing.Duckipelago";
    public override string GameName => "Placid Plastic Duck Simulator";
    public override string ApWorldName => "placidplasticducksim";
    public override string GoogleSheetId => "1xOW8OJ-Mya3Lgp_vYhXJTQ4B8r41K80tyesA-TwNOXY";
    public override string WorldVersion => "0.3.2";

    public DuckRowData[] DuckRowData = [];
    public DuckLogicRowData[] DuckLogicRowData = [];
    public MapRowData[] MapRowData = [];
    public Dictionary<DlcType, string> DlcTypeToName = [];
    public DlcType[] DlcItems = Enum.GetValues<DlcType>().Where(dlc => dlc.ItemName() is not "").ToArray();
    public int[] Columns = Enumerable.Range(1, 10).ToArray();

    public override void RunShenanigans()
    {
        GetSpreadsheet()
           .ReadTable(out DuckRowData).SkipColumn()
           .ReadTable(out DuckLogicRowData).SkipColumn()
           .ReadTable(out MapRowData).SkipColumn()
           .ReadTable(out DlcNameRowData[] dlcNames);

        DuckRowData = DuckRowData.Where(data => data.Include).ToArray();
        DlcTypeToName = dlcNames.ToDictionary(data => data.DlcType, data => data.DlcName);

        WriteData(
            "ducks", DuckRowData.Select(data => $"{data.DuckName}|{data.DuckId}|{(int)data.DlcType}|{data.Column}")
        );
    }

    public override void Options(WorldFactory _, OptionsFactory options_fact) =>
        options_fact
            // .AddOption(
            //      "Allow Unpredictable Ducks",
            //      "Allow ducks that have random, unpredictables spawns to be checks", new Toggle()
            //  )
           .AddOption("Ducks Please", "Enable ducks from the Ducks, Please DLC", new Toggle())
            // .AddOption("Quacking the Ice", "Enable the stage from the Qucking the Ice DLC", new Toggle())
           .AddOption("Duck Addiction", "Enable ducks from the Duck Addiction DLC", new Toggle())
            // .AddOption("Hippospace Download", "Enable the stage from the Hippospace Download DLC", new Toggle())
           .AddOption("So Many Ducks", "Enable ducks from the So Many Ducks DLC", new Toggle())
            // .AddOption("Rooftop One Percent", "Enable the stage from the Rooftop One Percent DLC", new Toggle())
           .AddOption("Ducks Galore", "Enable the ducks from the Ducks Galore DLC", new Toggle())
           .AddOption("Ducklings", "Enable the ducks from the Ducklings DLC", new Toggle())
            // .AddOption("Virtual Thermae", "Enable the stage from the Virtual Thermae DLC", new Toggle())
           .AddCheckOptions();

    public override void Locations(WorldFactory _, LocationFactory location_fact)
    {
        location_fact.ForEachOf(
            DuckRowData.GroupBy(data => data.DlcType),
            (b, group) => b.AddLocations(
                group.Key.OptionName(), group.Select(data => (string[])[data.DuckName, $"Column {data.Column}"])
            )
        );
    }

    public override void Items(WorldFactory _, ItemFactory item_fact)
    {
        item_fact.AddItem("Progressive Column Unlock", Progression)
                 .AddItem("Progressive Spawn Speed Upgrade", Useful)
                 .AddItem("Random Duck", Filler)
                 .AddItemListVariable("dlc_items", Progression, list: DlcItems.Select(dlc => dlc.ItemName()).ToArray())
                 .AddCreateItems(method =>
                      method.AddCode(CreateItemsFromCountGenCode("9", "Progressive Column Unlock"))
                            .AddCode(CreateItemsFromCountGenCode("9", "Progressive Spawn Speed Upgrade"))
                            .ForEachOf(
                                 DlcItems, (b, dlc) => b.AddCode(
                                     new IfFactory($"options.{dlc.OptionName()}")
                                        .AddCode(CreateItem(dlc.ItemName()))
                                 )
                             )
                            .AddCode(CreateItemsFillRemainingWithItem("Random Duck"))
                  );
    }

    public override void Rules(WorldFactory _, RuleFactory rule_fact) 
        => rule_fact.AddCompoundLogicFunction(
        "col", "has_column", "hasN[\"Progressive Column Unlock\", number - 1]", "number"
    )
   .AddLogicRules(
        DuckRowData.Where(data => data.DlcType is not DlcType.BaseGame).ToDictionary(data => data.DuckName, data => $"has[\"{data.DlcType.ItemName()}\"]")
        );

    public override void Regions(WorldFactory _, RegionFactory region_fact)
    {
        region_fact.AddRegions(regions: Columns.Select(i => $"Column {i}").ToArray())
                   .ForEachOf(
                        Columns,
                        (b, i) => b.AddConnectionCompiledRule(
                            i == 1 ? "Menu" : $"Column {i - 1}", $"Column {i}", $"col[{i}]"
                        )
                    )
                   .AddLocationsFromList("base_game")
                   .ForEachOf(
                        DlcItems,
                        (b, dlc) => b.AddLocationsFromList(dlc.OptionName(), condition: $"options.{dlc.OptionName()}")
                    );
    }

    public override void Init(WorldFactory world_fact, WorldInitFactory init_fact)
    {
        init_fact
           .UseItemGroups(new Dictionary<string, string> { ["column"] = "[\"Progressive Column Unlock\"]" })
           .UseInitFunction()
           .AddUseUniversalTrackerPassthrough(yamlNeeded: false)
           .UseCreateRegions()
           .AddCreateItems()
           .UseSetRules(method => method.AddCode(CreateGoalCondition("col[10]", world_fact.GetRuleFactory())))
           .UseFillSlotData()
           .InjectCodeIntoWorld(world => world.AddVariable(new Variable("gen_puml", "False")))
           .UseGenerateOutput(method => method.AddCode(PumlGenCode()));
    }

    public override string GenerateGraphViz(WorldFactory worldFactory, Dictionary<string, string> associations,
        Func<string, string> getRule,
        string[][] locationDoubleArrays)
    {
        return new GraphBuilder(GameName)
              .ForEachOf(
                   Columns, (b, i) => b.AddConnection(i == 1 ? "Menu" : $"Column {i - 1}", $"Column {i}", $"col[{i}]")
               )
              .AddLocationsFromDoubleArray(locationDoubleArrays, getRule)
              .GenString();
    }
}