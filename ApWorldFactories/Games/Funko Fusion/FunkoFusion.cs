using CreepyUtil.Archipelago.WorldFactory;
using WorldBuilder;
using static CreepyUtil.Archipelago.WorldFactory.ItemFactory.ItemClassification;
using Range = CreepyUtil.Archipelago.WorldFactory.Range;

namespace ApWorldFactories.Games.Funko_Fusion;

public class FunkoFusion : BuildData
{
    public override string SteamDirectory => "";
    public override string ModFolderName => "";
    public override string GameName => "Manual_Funko_Fusion";
    public override string ApWorldName => "manual_funko_fusion";
    public override string GoogleSheetId => "1hjUyRCn3czFVLNzGGxrm4r8Ff5_gili88jXbZOjMVG0";
    public override string WorldVersion => "0.1.0";
    public override bool IsManual => true;

    public override Dictionary<string, string> SheetGids { get; } = new() { ["locations"] = "2036061975" };

    public RegionRowData[] RegionData = [];
    public ItemsRowData[] BlueprintData = [];
    public ItemsRowData[] AbilityData = [];
    public ItemsRowData[] CollectableData = [];
    public CharacterRowData[] CharacterData = [];
    public LocationSector[] LocationSector = [];
    public Dictionary<string, string[]> WorldLevelMap = [];
    public int CrownCount;

    public override void RunShenanigans()
    {
        GetSpreadsheet().ReadTable(out RegionData).SkipColumn()
                        .ReadTable(out BlueprintData)
                        .ReadTable(out AbilityData)
                        .ReadTable(out CollectableData).SkipColumn()
                        .ReadTable(out CharacterData);

        LocationRowData[] rawLocationData = [];
        var locationSheet = GetSpreadsheet("locations").ReadTable(out rawLocationData);
        for (var i = 0; i < 8; i++)
        {
            locationSheet.SkipColumn().ReadTable(out LocationRowData[] addedLocationData);
            rawLocationData = [.. rawLocationData, .. addedLocationData];
        }

        LocationSector = Funko_Fusion.LocationSector.CreateSectorFromData(
            rawLocationData, data => new LocationSector(data)
        );

        CrownCount = LocationSector.Count(l => l.CollectableType is CollectableType.GoldCrown);
        WorldLevelMap = RegionData.Where(r => r.IsSubArea).GroupBy(r => r.FromRegion)
                                  .ToDictionary(r => r.Key, r => (string[])[.. r.Select(subR => subR.Region)]);
    }

    public override void Options(WorldFactory _, OptionsFactory options_fact)
    {
        options_fact
           .AddOption("World Unlocks", "Require a World Unlock item to access a world", new DefaultOnToggle())
           .AddOption("Level Unlocks", "Require a Level Unlock item to access a level", new Toggle())
           .AddOption(
                "Goal Crowns", "How many Gold Crowns needed to goal", new Range(CrownCount, CrownCount / 2, CrownCount)
            )
           .AddCheckOptions(method =>
                method
                   .AddCode("world.starting_world = random.choice(list(world_level_map.keys()))")
                   .AddCode("world.starting_level = random.choice(world_level_map[world.starting_world])")
                   .AddCode("world.starting_character = random.choice(important_characters_list)")
                   .AddCode("world.starting_filler_character = random.choice(blah_characters_list)")
                   .CreateMinimalCatch()
            );
    }

    public override void Locations(WorldFactory _, LocationFactory location_fact)
    {
        location_fact.AddLocations("locations", LocationSector.Select(sector => (string[])[sector.Name, sector.Region]))
                     .AddIndependentVariable(
                          new StringArray(
                              "important_characters_list",
                              [
                                  .. CharacterData.Where(chars => chars.Abilities.Length > 0 || chars.IsQoL)
                                                  .Select(chars => chars.Character)
                              ]
                          )
                      ).AddIndependentVariable(
                          new StringArray(
                              "blah_characters_list",
                              [
                                  .. CharacterData.Where(chars => chars.Abilities.Length == 0 && !chars.IsQoL)
                                                  .Select(chars => chars.Character)
                              ]
                          )
                      )
                     .AddIndependentVariable(new StringArrayMap("world_level_map", WorldLevelMap))
                     .ForEachOf(
                          AbilityData,
                          (f, ability) =>
                          {
                              f.AddIndependentVariable(
                                  new StringArray(
                                      $"character_{ability.CleanName}",
                                      [
                                          .. CharacterData.Where(chars => chars.Abilities.Contains(ability.Name))
                                                          .Select(chars => chars.Character),
                                      ]
                                  )
                              );
                          }
                      );
    }

    public override void Items(WorldFactory _, ItemFactory item_fact)
    {
        item_fact
           .AddItemListVariable(
                "world_unlocks", Progression,
                list: [.. RegionData.Where(r => r is { IsSubArea: false, Region: not "Hub" }).Select(r => r.Region)]
            ).AddItemListVariable(
                "level_unlocks", Progression, list: [.. RegionData.Where(r => r.IsSubArea).Select(r => r.Region)]
            )
           .AddItemListVariable("blueprint_items", Progression, list: [.. BlueprintData.Select(bp => bp.Name)])
           .AddItemListVariable(
                "important_characters", Progression,
                list:
                [
                    .. CharacterData.Where(chars => chars.Abilities.Length > 0 || chars.IsQoL)
                                    .Select(chars => chars.Character)
                ]
            ).AddItemListVariable(
                "blah_characters", Filler,
                list:
                [
                    .. CharacterData.Where(chars => chars.Abilities.Length == 0 && !chars.IsQoL)
                                    .Select(chars => chars.Character)
                ]
            ).AddItem("Gold Crown", Progression).AddItem("Vinyl", Filler)
           .AddCreateItems(method =>
                method
                   .CreateItemsFromCountGenCode($"{CrownCount}", "Gold Crown")
                   .AddCode(
                        new IfFactory("options.world_unlocks").AddCode(
                            method.Raw_CreateItemsFromList(
                                "world_unlocks", exclusionCondition: "item == world.starting_world"
                            )
                        )
                    )
                   .AddCode(
                        new IfFactory("options.level_unlocks").AddCode(
                            method.Raw_CreateItemsFromList(
                                "level_unlocks", exclusionCondition: "item == world.starting_level"
                            )
                        )
                    )
                   .CreateItemsFromList("blueprint_items", exclusionCondition: " item == \"Bounce Pad\"")
                   .CreateItemsFromList("important_characters", exclusionCondition: "item == world.starting_character")
                   .CreateItemsFromList(
                        "blah_characters", exclusionCondition: "item == world.starting_filler_character"
                    )
                   .FillRemainingWithItem("Vinyl")
            );
    }

    public override void Rules(WorldFactory _, RuleFactory rule_fact)
    {
        rule_fact
           .AddCompoundLogicFunction("world", "has_world_unlock", "not yaml[\"world_unlocks\"] or has[world]", "world")
           .AddCompoundLogicFunction("level", "has_level_unlock", "not yaml[\"level_unlocks\"] or has[level]", "level")
           .ForEachOf(
                AbilityData, (f, ability) =>
                    f.AddCompoundLogicFunction(
                        $"{ability.CleanName}", $"has_character_ability_{ability.CleanName}",
                        $"any[character_{ability.CleanName}]"
                    )
            ).ForEachOf(
                BlueprintData,
                (f, bp) => f.AddCompoundLogicFunction(
                    $"{bp.CleanName}", $"has_blueprint_{bp.CleanName}", $"has[\"{bp.Name}\"]"
                )
            ).AddLogicRules(LocationSector.ToDictionary(sector => sector.Name, sector => sector.GenRule()));
    }

    public override void Regions(WorldFactory _, RegionFactory region_fact)
    {
        region_fact
           .AddRegions("", [.. RegionData.Select(f => f.Region)])
           .ForEachOf(
                RegionData, (f, region) =>
                {
                    f.AddConnectionCompiledRule(
                        region.FromRegion is "" ? "Menu" : region.FromRegion, region.Region,
                        region.Region is not "Hub" ? $"{(region.IsSubArea ? "level" : "world")}[\"{region.Region}\"]"
                            : ""
                    );
                }
            ).AddLocationsFromList("locations");
    }

    public override void Init(WorldFactory _, WorldInitFactory init_fact)
    {
        init_fact
           .UseInitFunction(method => method
                                     .AddCode(new Variable("self.starting_world", "\"\""))
                                     .AddCode(new Variable("self.starting_level", "\"\""))
                                     .AddCode(new Variable("self.starting_character", "\"\""))
                                     .AddCode(new Variable("self.starting_filler_character", "\"\""))
            )
           .AddUseUniversalTrackerPassthrough(
                method =>
                    method.CreateUtPassthrough("\"starting_world\"", "self.starting_world")
                          .CreateUtPassthrough("\"starting_level\"", "self.starting_level")
                          .CreateUtPassthrough("\"starting_character\"", "self.starting_character")
                          .CreateUtPassthrough("\"starting_filler_character\"", "self.starting_filler_character")
                , yamlNeeded: false
            )
           .UseGenerateEarly(method =>
                method.CreatePushPrecollected("Bounce Pad")
                      .CreatePushPrecollected("self.starting_world", stringify: false)
                      .CreatePushPrecollected("self.starting_level", stringify: false)
                      .CreatePushPrecollected("self.starting_character", stringify: false)
                      .CreatePushPrecollected("self.starting_filler_character", stringify: false)
            )
           .UseSetRules(method => method.CreateGoalCompiledCondition("hasN[\"Gold Crown\", options.goal_crowns]"))
           .UseGetFillerItemName("Vinyl")
           .UseFillSlotData(
                new Dictionary<string, string>
                {
                    ["Foxxo"] = "str(\"Best Boi\")", ["starting_world"] = "str(self.starting_world)",
                    ["starting_level"] = "str(self.starting_level)",
                    ["starting_character"] = "str(self.starting_character)",
                    ["starting_filler_character"] = "str(self.starting_filler_character)",
                }
            )
           .InjectCodeIntoWorld(world => world.AddVariable(new Variable("gen_puml", "False")))
           .UseGenerateOutput(method => method.PumlGenCode());
    }

    public override string GenerateGraphViz(WorldFactory worldFactory, Dictionary<string, string> associations,
        Func<string, string> getRule, string[][] locationDoubleArrays)
        => new GraphBuilder(GameName)
          .ForEachOf(
               RegionData,
               (b, sector) => b.AddConnection(
                   sector.FromRegion is "" ? "Menu" : sector.FromRegion, sector.Region,
                   sector.Region is not "Hub" ? $"{(sector.IsSubArea ? "level" : "world")}[\"{sector.Region}\"]" : ""
               )
           )
          .AddLocationsFromDoubleArray(locationDoubleArrays, getRule).GenString("LR");
}