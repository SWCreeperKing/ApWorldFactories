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
    public override string WorldVersion => "0.1.1";

    public RegionRowData[] RegionRowData = [];
    public RegionRowData[] BiomeRowData = [];
    public string[] RegionsWithUnlock = [];

    public RegionConnectionsRowData[] RegionConnectionsRowData = [];
    public ItemsRowData[] ItemsRowData = [];
    public PlantRowData[] PlantRowData = [];
    public BadgeRowData[] BadgeRowData = [];
    public CookingRowData[] CookingRowData = [];
    public EvolutionRowData[] EvolutionRowData = [];

    public Dictionary<int, string> RegionIdMap = [];
    public Dictionary<int, RegionRowData> RegionRegionDataMap = [];
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
        RegionRegionDataMap = RegionRowData.ToDictionary(data => data.Id, data => data);
        RegionIdRequiresDepth = RegionRowData.ToDictionary(data => data.Id, data => data.RequiresDepth);
        BiomeRowData = [.. RegionRowData.Where(data => data.IsBiome)];
        RegionsWithUnlock =
        [
            .. RegionRowData.Where(r => r.Region is not "Home Cave"
                                        && RegionConnectionsRowData.Where(c => c.B == r.Id).All(c => c.A is not 3)
            ).Select(data => $"{data.Region}")
        ];

        WriteData("Biomes", BiomeRowData.Select(b => b.Region));
        WriteData("RegionIdMap", RegionIdMap.Select(kv => $"{kv.Key}|{kv.Value}"));
        WriteData("BadgeIdMap", BadgeRowData.Select(b => $"{b.Name}|{b.Id}"));
        WriteData("ItemIdMap", ItemsRowData.Select(i => $"{i.ItemName}|{i.Id}"));
        WriteData("CookingIdMap", CookingRowData.Select(c => $"{c.Food}|{c.Id}"));
        WriteData("EvolutionIdMap", EvolutionRowData.Select(e => $"{e.Name}|{e.LoddleId}"));
    }

    public override void Options(WorldFactory _, OptionsFactory options_fact)
    {
        options_fact
           .AddOption("Evolutionsanity", "Loddle Evolutions are checks", new Toggle())
           .AddOption("Cooksanity", "Cooking Recipes are checks", new Toggle())
           .AddCheckOptions();
    }

    public override void Locations(WorldFactory _, LocationFactory location_fact)
    {
        int[] percentArr = [25, 50, 75, 100];
        location_fact
           .AddLocations(
                "clean_biomes",
                BiomeRowData.SelectMany(data
                    => percentArr.Select(i => (string[])[$"Clean {data.Region} {i}%", data.Region])
                )
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
           .AddLocations(
                "evolutions", EvolutionRowData.Select(data => (string[])[$"Evolve: {data.Name}", "Evolutions"])
            )
           .AddLocations("recipes", CookingRowData.Select(data => (string[])[$"Cook: {data.Food}", "Cooking"]));
    }

    public override void Items(WorldFactory _, ItemFactory item_fact)
    {
        item_fact.ForEachOf(
                      ItemsRowData.GroupBy(data => data.Classification),
                      (b, group) => b.AddItemCountVariable(
                          $"{group.Key}_items".ToLowerInvariant(),
                          group.ToDictionary(data => data.ItemName, data => data.Count),
                          group.Key
                      )
                  )
                 .AddItemListVariable(
                      "region_unlocks", ItemFactory.ItemClassification.Progression, list: RegionsWithUnlock
                  )
                 .AddCreateItems(method =>
                      method
                         .AddCode(
                              // CreateItemsFromList("region_unlocks", exclusionCondition: "item == world.starting_region")
                              CreateItemsFromList("region_unlocks")
                          )
                         .AddCode(CreateItemsFromMapCountGenCode("progression_items"))
                         .AddCode(CreateItemsFromMapCountGenCode("useful_items"))
                         .AddCode(CreateItemsFillRemainingWith("list(filler_items.keys())"))
                  );
    }

    public override void Rules(WorldFactory _, RuleFactory rule_fact)
    {
        int[] percentArr = [25, 50, 75, 100];
        rule_fact.AddCompoundLogicFunction("depth", "has_depth", "has[\"Depth Resistance Module\"]")
                 .AddLogicRules(
                      BiomeRowData
                         .SelectMany(data
                              => percentArr.Select(i => (string[])[$"Clean {data.Region} {i}%", data.GenRule()])
                          ).ToDictionary(a => a[0], a => a[1])
                  )
                 .AddLogicRules(CookingRowData.ToDictionary(data => $"Cook: {data.Food}", data => data.GenRule()))
                 .AddLogicRules(EvolutionRowData.ToDictionary(data => $"Evolve: {data.Name}", data => data.GenRule()));
    }

    public override void Regions(WorldFactory _, RegionFactory region_fact)
    {
        region_fact
           .AddRegions(regions: ["Upgrades", .. RegionRowData.Select(data => data.Region)])
           .AddRegion("Cooking", "options.cooksanity")
           .AddRegion("Evolutions", "options.evolutionsanity")
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
           .AddLocationsFromList("recipes")
           .AddLocationsFromList("evolutions")
           .AddEventLocations(
                locations:
                [
                    .. BiomeRowData.Select(biome => new EventLocationData(
                            biome.Region, $"Clean the {biome.Region}", "Biomes Cleaned", $"Clean {biome.Region} 100%"
                        )
                    ),
                ]
            )
           .AddEventLocations(
                locations:
                [
                    .. RegionRowData.SelectMany(data => data.Plants.Select(plant => new EventLocationData(
                                data.Region, $"Event Pick: {plant} ({data.Region})", plant
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
            if (RegionsWithUnlock.Contains(RegionIdMap[regionB]) && RegionIdMap[regionA] is not "Home Cave")
                rules.Add($"has[\"{RegionIdMap[regionB]}\"]");

            if ( RegionRegionDataMap[regionB].RegionRequirements.Length != 0)
                rules.AddRange(RegionRegionDataMap[regionB].RegionRequirements.Select(r => $"has[\"{r}\"]"));

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
            // .UseInitFunction(method => method.AddCode(new Variable("self.starting_region", "\"\"")))
           .UseInitFunction(method => method.AddCode(new Variable("self.starting_region", "\"\"")))
           .AddUseUniversalTrackerPassthrough(
                // method =>
                //     method.AddCode(CreateUtPassthrough("\"starting_region\"", "self.starting_region")),
                yamlNeeded: false
            )
            // .UseGenerateEarly(method => method.AddCode(CreatePushPrecollected("self.starting_region", stringify: false)))
           .UseCreateRegions()
           .AddCreateItems()
           .UseSetRules(method => method
               .AddCode(CreateGoalCondition(StateHas("Biomes Cleaned", $"{BiomeRowData.Length}", returnValue: false)))
            )
           .UseFillSlotData(
                new Dictionary<string, string> { ["uuid"] = "str(shuffled)" },
                method => method.AddCode(CreateUniqueId())
            )
           .UseGetFillerItemNames(
                [
                    .. ItemsRowData.Where(i => i.Classification is ItemFactory.ItemClassification.Filler)
                                   .Select(i => i.ItemName)
                ]
            )
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
            if (RegionsWithUnlock.Contains(RegionIdMap[regionB]) && RegionIdMap[regionA] is not "Home Cave")
                rules.Add($"has[\"{RegionIdMap[regionB]}\"]");

            if (RegionRegionDataMap[regionB].RegionRequirements.Length != 0)
                rules.AddRange(RegionRegionDataMap[regionB].RegionRequirements.Select(r => $"has[\"{r}\"]"));

            b.AddConnection(RegionIdMap[regionA], RegionIdMap[regionB], string.Join(" and ", rules));
        }
    }
}