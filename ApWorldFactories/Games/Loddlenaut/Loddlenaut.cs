using ApWorldFactories.Graphviz;
using CreepyUtil.Archipelago.WorldFactory;
using static ApWorldFactories.PathConstants;
using static CreepyUtil.Archipelago.WorldFactory.PremadePython;

namespace ApWorldFactories.Games.Loddlenaut;

public class Loddlenaut : BuildData
{
    public override string SteamDirectory => FDrive;
    public override string ModFolderName => "SW_CreeperKing.Loddlepelago";
    public override string GameName => "Loddlenaut";
    public override string ApWorldName => "loddlenaut";
    public override string GoogleSheetId => "1_EI5ahH1SeW4HrX7HdRrBHWcOwlh854Flaoovdwosow";
    public override string WorldVersion => "0.1.0";

    public RegionRowData[] RegionRowData = [];
    public RegionConnectionsRowData[] RegionConnectionsRowData = [];
    public ItemsRowData[] ItemsRowData = [];
    public PlantRowData[] PlantRowData = [];
    public BadgeRowData[] BadgeRowData = [];
    public CookingRowData[] CookingRowData = [];
    public EvolutionRowData[] EvolutionRowData = [];

    public Dictionary<int, string> RegionIdMap = [];
    public Dictionary<int, bool> RegionIdRequiresDepth = [];

    public override void RunShenanigans()
    {
        GetSpreadsheet()
           .ReadTable(out RegionRowData).SkipColumn()
           .ReadTable(out RegionConnectionsRowData).SkipColumn()
           .ReadTable(out ItemsRowData).SkipColumn()
           .ReadTable(out PlantRowData).SkipColumn()
           .ReadTable(out BadgeRowData).SkipColumn()
           .ReadTable(out CookingRowData).SkipColumn()
           .ReadTable(out EvolutionRowData);


        RegionIdMap = RegionRowData.ToDictionary(data => data.Id, data => data.Region);
        RegionIdRequiresDepth = RegionRowData.ToDictionary(data => data.Id, data => data.RequiresDepth);

        var locatedItems = RegionRowData.SelectMany(data => data.AvailableUpgrades).ToHashSet();
        ItemsRowData = [.. ItemsRowData.Where(data => !locatedItems.Contains(data.ItemName))];
    }

    public override void Locations(WorldFactory _, LocationFactory location_fact)
    {
        location_fact
           .AddLocations(
                "clean_biomes",
                RegionRowData.Where(data => data.IsBiome)
                             .Select(data => (string[])[$"Clean {data.Region}", data.Region])
            ).AddLocations(
                "find_biomes",
                RegionRowData.Select(data => (string[])[$"Discovered {data.Region}", data.Region])
            )
           .AddLocations("upgrades", ItemsRowData.Select(data => (string[])[$"Purchase {data.ItemName}", "Upgrades"]))
           .AddLocations(
                "biome_upgrades",
                [
                    .. RegionRowData.Where(data => data.AvailableUpgrades.Any())
                                    .SelectMany(data => data.AvailableUpgrades.Select(up => (string[])[up, data.Region])
                                     ),
                ]
            )
           .AddLocations("badges", BadgeRowData.Select(data => (string[])[data.Name, RegionIdMap[data.RegionId]]))
           .AddLocations("evolutions", EvolutionRowData.Select(data => (string[])[data.Name, "Evolutions"]))
           .AddLocations("recipes", CookingRowData.Select(data => (string[])[$"Cook: {data.Food}", "Cooking"]));
    }

    public override void Items(WorldFactory _, ItemFactory item_fact)
    {
        item_fact.ForEachOf(
                      ItemsRowData.GroupBy(data => data.Classification),
                      (b, group) => b.AddItemCountVariable(
                          $"{group.Key}_items".ToLower(), group.ToDictionary(data => data.ItemName, data => data.Count),
                          group.Key
                      )
                  )
                 .AddItem("Potato Chip", ItemFactory.ItemClassification.Filler)
                 .AddCreateItems(method =>
                      method
                         .AddCode(CreateItemsFromMapCountGenCode("progression_items"))
                         .AddCode(CreateItemsFromMapCountGenCode("useful_items"))
                         .AddCode(CreateItemsFillRemainingWithItem("Potato Chip"))
                  );
    }

    public override void Rules(WorldFactory _, RuleFactory rule_fact)
    {
        rule_fact.AddCompoundLogicFunction("depth", "has_depth", "has[\"Depth Resistance Module\"]")
                 .AddLogicRules(CookingRowData.ToDictionary(data => $"Cook: {data.Food}", data => data.GenRule()))
                 .AddLogicRules(EvolutionRowData.ToDictionary(data => data.Name, data => data.GenRule()));
    }

    public override void Regions(WorldFactory _, RegionFactory region_fact)
    {
        region_fact
           .AddRegions(regions: ["Upgrades", "Evolutions", "Cooking"])
           .AddRegions(regions: [.. RegionRowData.Select(data => data.Region)])
           .ForEachOf(
                RegionConnectionsRowData, (b, data) =>
                {
                    AddConnection(data.A, data.B);
                    AddConnection(data.B, data.A);
                }
            )
           .AddConnection("Menu", "Upgrades")
           .AddConnectionCompiledRule(
                "Menu", "Evolutions",
                $"any[[{string.Join(", ", RegionRowData.Where(data => data.IsBiome).Select(data => $"\"{data.Region}\""))}]]"
            )
           .AddConnectionCompiledRule("Menu", "Cooking", "has[\"Cooking Module\"]")
           .AddConnection("Menu", "Home Cave")
           .AddLocationsFromList("clean_biomes")
           .AddLocationsFromList("find_biomes")
           .AddLocationsFromList("upgrades")
           .AddLocationsFromList("badges")
           .AddLocationsFromList("evolutions")
           .AddEventLocations(
                locations:
                [
                    .. RegionRowData
                       .SelectMany(data => data.Plants.Select(plant => new EventLocationData(
                                    data.Region, $"Pick: {plant}", plant, $"Pick: {plant}"
                                )
                            )
                        ),
                ]
            )
           .AddEventLocationsFromList("recipes", item: "location[0].replace('Cook: ', '')");

        return;

        void AddConnection(int regionA, int regionB)
        {
            List<string> rules = [];

            if (RegionIdRequiresDepth[regionB]) rules.Add("depth");
            if (RegionIdMap[regionB] is not "Home Cave") rules.Add($"has[\"Region: {RegionIdMap[regionB]}\"]");

            if (rules.Count != 0)
                region_fact.AddConnectionCompiledRule(
                    RegionIdMap[regionA], RegionIdMap[regionB], string.Join(" and ", rules)
                );
            else region_fact.AddConnection(RegionIdMap[regionA], RegionIdMap[regionB]);
        }
    }

    public override void Init(WorldFactory _, WorldInitFactory init_fact)
    {
        init_fact
           .UseInitFunction()
           .AddUseUniversalTrackerPassthrough(yamlNeeded: false)
           .UseCreateRegions()
           .AddCreateItems()
           .UseSetRules(method => method
               .AddCode(CreateGoalCondition(StateHas("Nights Survived", "7", returnValue: false)))
            )
           .UseFillSlotData()
           .InjectCodeIntoWorld(world => world.AddVariable(new Variable("gen_puml", "False")))
           .UseGenerateOutput(method => method.AddCode(PumlGenCode()));
    }

    public override string GenerateGraphViz(WorldFactory worldFactory, Dictionary<string, string> associations,
        Func<string, string> getRule, string[][] locationDoubleArrays)
    {
        return new GraphBuilder(GameName)
              .AddRegions(regions: ["Upgrades", "Evolutions"])
              .AddRegions(regions: [.. RegionRowData.Select(data => data.Region)])
              .ForEachOf(
                   RegionConnectionsRowData, (b, data) =>
                   {
                       AddConnection(b, data.A, data.B);
                       AddConnection(b, data.B, data.A);
                   }
               )
              .AddConnection("Menu", "Upgrades")
              .AddConnection("Menu", "Cooking", "has[\"Cooking Module\"]")
              .AddConnection(
                   "Menu", "Evolutions",
                   $"any[[{string.Join(", ", RegionRowData.Where(data => data.IsBiome).Select(data => $"\"{data.Region}\""))}]]"
               )
              .AddConnection("Menu", "Home Cave")
              .AddLocationsFromDoubleArray(locationDoubleArrays, getRule)
              .ForEachOf(
                   RegionRowData,
                   (b, data) => b.ForEachOf(
                       data.Plants, (_, plant) => b.AddEventLocation(data.Region, getRule, $"Pick: {plant}", "", plant)
                   )
               )
              .ForEachOf(
                   CookingRowData,
                   (b, data) => b.AddEventLocation(
                       "Cooking", getRule, $"Event: Cook: {data.Food}", $"Cook: {data.Food}", data.Food
                   )
               )
              .GenString();

        void AddConnection(GraphBuilder b, int regionA, int regionB)
        {
            List<string> rules = [];

            if (RegionIdRequiresDepth[regionB]) rules.Add("depth");
            if (RegionIdMap[regionB] is not "Home Cave") rules.Add($"has[\"Region: {RegionIdMap[regionB]}\"]");

            b.AddConnection(RegionIdMap[regionA], RegionIdMap[regionB], string.Join(" and ", rules));
        }
    }
}