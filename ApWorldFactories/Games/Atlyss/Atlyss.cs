using CreepyUtil.Archipelago.WorldFactory;
using static CreepyUtil.Archipelago.WorldFactory.ItemFactory.ItemClassification;
using static CreepyUtil.Archipelago.WorldFactory.PremadePython;

namespace ApWorldFactories.Games.Atlyss;

public class Atlyss : BuildData
{
    public override string SteamDirectory => "";
    public override string ModFolderName => "";
    public override string GameName => "Atlyss";
    public override string ApWorldName => "atlyss";
    public override string GoogleSheetId => "15j_e_S0TrJHna8_CJPs4rqst0saaTQv36iNUke8NhUA";
    public override string WorldVersion => "0.1.0";

    public override Dictionary<string, string> SheetGids
        => new() { ["items"] = "1796499840", ["achievements"] = "254672963" };

    private LocationLevelData[] LocationLevelData = [];
    private EnemyListData[] EnemyListData = [];
    private QuestData[] QuestData = [];
    private ProfessionsData[] ProfessionsData = [];
    private MerchantData[] MerchantData = [];
    private ItemData[] ItemData = [];
    private AchievementData[] AchievementData = [];
    private Dictionary<ItemType, Dictionary<ClassType, Dictionary<int, List<ItemData>>>> EquipmentItems = [];

    public override void RunShenanigans()
    {
        GetSpreadsheet("main")
           .ReadTable(out LocationLevelData).SkipColumn()
           .ReadTable(out EnemyListData).SkipColumn()
           .ReadTable(out QuestData).SkipColumn()
           .ReadTable(out ProfessionsData).SkipColumn()
           .ReadTable(out MerchantData);
        GetSpreadsheet("items").ReadTable(out ItemData);
        GetSpreadsheet("achievements").ReadTable(out AchievementData);

        EquipmentItems.Clear();
        foreach (var item in ItemData)
        {
            if (item.ItemRarity is ItemRarity.Cosmetic) continue;
            if ((int)item.InGameClassification is < 2 or > 8) continue;
            if (!EquipmentItems.TryGetValue(item.InGameClassification, out var itemClassDict))
            {
                EquipmentItems[item.InGameClassification] = itemClassDict = [];
            }

            if (!itemClassDict.TryGetValue(item.ClassRequirement, out var itemList))
            {
                itemClassDict[item.ClassRequirement] = itemList = [];
            }

            if (!itemList.TryGetValue(item.Tier, out var itemTierList)) { itemList[item.Tier] = itemTierList = []; }

            itemTierList.Add(item);
        }
    }

    public override void Options(WorldFactory _, OptionsFactory options_fact)
    {
        options_fact.AddOption(
                         "Goal",
                         """
                         What is required to complete the game.
                         Slime Diva: Defeat the Slime Diva boss (level 10).
                         Lord Zuulneruda: Defeat Lord Zuulneruda in the Catacombs (level 12).
                         Colossus: Defeat the Colossus in Crescent Grove (level 20).
                         Galius: Defeat Galius in Bularr Fortress (level 26) - DEFAULT.
                         Lord Kaluuz: Defeat Lord Kaluuz in Catacombs Floor 3 (level 18).
                         Valdur: Defeat Valdur the dragon (level 25+).
                         All Bosses: Defeat all 6 major bosses.
                         All Quests: Complete every quest in the game.
                         Level 32: Reach the maximum level.
                         """,
                         new Choice(
                             0, "slime_diva", "lord_zuulneruda", "colossus", "galius", "lord_kaluuz", "valdur",
                             "all_bosses", "all_quests", "level_32"
                         )
                     ).AddOption(
                         "Random Portals",
                         """
                         How area portals are unlocked.
                         Off (default): Progressive Portals - find "Progressive Portal" items to unlock
                         areas in a fixed sequence. Each portal found opens the next area in order.
                         On: Random Portals - find individual portal items (e.g. "Outer Sanctum Portal",
                         "Catacombs Portal") to unlock specific areas independently.
                         """, new Toggle()
                     )
                    .AddOption(
                         "Shop Sanity",
                         """
                         Whether shop items can contain Archipelago items from other worlds.
                         When enabled, buying items from shops sends checks to other players.
                         """, new DefaultOnToggle()
                     )
                    .AddOption(
                         "Main Class", "What you chose to be as your main class",
                         new Choice(0, "fighter", "bandit", "mystic")
                     )
                    .AddOption(
                         "Secondary Class", "What you chose to be as your secondary class",
                         new Choice(0, "fighter", "bandit", "mystic", "none")
                     )
                    .InjectCodeIntoOptionsClass(classFact => classFact.AddMethod(
                             new MethodFactory("is_class")
                                .AddParams("self", "class_name")
                                .AddCode(
                                     """
                                     class_name_lower = class_name.lower()
                                     if class_name_lower == 'any': return True
                                     return class_name_lower == self.main_class.value or class_name_lower == self.secondary_class.value
                                     """
                                 )
                         )
                     )
                    .AddCheckOptions(method =>
                         method.AddCode(
                             """
                             classes = ['fighter', 'mystic', 'bandit']

                             if options.main_class.value == options.secondary_class.value:
                                 raise_yaml_error(world.player, "You cannot have the same class selected for main_class and secondary_class")
                             """
                         )
                     );
    }

    public override void Locations(WorldFactory _, LocationFactory location_fact)
    {
        location_fact.AddLocations(
                          "quests",
                          QuestData.Where(data => data.Enabled)
                                   .Select(data => (string[])[data.Quest, data.AreaAccepted])
                      )
                     .AddLocations(
                          "levels", Enumerable.Range(1, 16).Select(i => (string[])[$"Reach Level {i * 2}", "Menu"])
                      )
                     .AddLocations(
                          "merchants",
                          MerchantData.SelectMany(data
                              => Enumerable.Range(1, 5).Select(i => (string[])
                                  [$"Buy Item #{i} from {data.Name}", data.Area]
                              )
                          )
                      ).AddLocations(
                          "professions",
                          ProfessionsData.Select(data => data.Profession).Distinct().SelectMany(s
                              => Enumerable.Range(1, 10).Select(i => (string[])[$"{s} Lv. {i}", "Menu"])
                          )
                      ).AddLocations(
                          "achievements",
                          AchievementData.Where(data => data.Enabled).Select(data => (string[])[data.Name, data.Area])
                      );
    }

    public override void Items(WorldFactory _, ItemFactory item_fact)
    {
        List<string> progressiveItemMap = [];
        List<string> progressiveItemMapMd = [];
        GetMaxProgressive("Any", ClassType.Any, progressiveItemMapMd, item_fact, progressiveItemMap, out var _);
        progressiveItemMapMd.Add("\n---\n");
        GetMaxProgressive(
            "Fighter", ClassType.Fighter, progressiveItemMapMd, item_fact, progressiveItemMap, out var addFighter
        );
        progressiveItemMapMd.Add("\n---\n");
        GetMaxProgressive(
            "Mystic", ClassType.Mystic, progressiveItemMapMd, item_fact, progressiveItemMap, out var addMystic
        );
        progressiveItemMapMd.Add("\n---\n");
        GetMaxProgressive(
            "Bandit", ClassType.Bandit, progressiveItemMapMd, item_fact, progressiveItemMap, out var addBandit
        );

        ItemData.Where(data => data.ItemPoolCount > 0)
                .GroupBy(data => data.Classification)
                .ToDictionary(
                     g => g.Key, g => g.ToDictionary(data => data.Name, data => data.ItemPoolCount)
                 ).Aggregate(
                     item_fact,
                     (factory, pair) => factory.AddItemCountVariable(
                         $"item_counts_{pair.Key}".ToLower(), pair.Value, pair.Key, addToList: false
                     )
                 );

        ItemData.GroupBy(data => data.Classification).Aggregate(
            item_fact,
            (factory, data) => factory.AddItemListVariable(
                $"{data.Key}_items".ToLower(), data.Key, list: data.Select(d => d.Name).ToArray()
            )
        );

        item_fact.AddItemCountVariable(
                      "filler_weights",
                      ItemData.Where(data => data.FillerWeight > 0).ToDictionary(
                          data => data.Name, data => data.FillerWeight
                      ),
                      Deprioritized, addToList: false
                  )
                 .AddItemListVariable(
                      "portals", Progression,
                      list: LocationLevelData
                           .Select(data
                                => $"{(data.Area.StartsWith("Sanctum Catacombs") ? "Catacombs" : data.Area)} Portal"
                            )
                           .Distinct()
                           .ToArray()
                  )
                 .AddItem("Progressive Portal", Progression)
                 .AddCreateItems(factory =>
                      factory
                         .AddCode("random = world.random")
                         .AddCode(CreateItemsFromMapCountGenCode("any_progressives"))
                         .AddCode(addFighter)
                         .AddCode(addMystic)
                         .AddCode(addBandit)
                         .AddCode(CreateItemsFromMapCountGenCode("item_counts_useful"))
                         .AddCode(CreateItemsFromMapCountGenCode("item_counts_filler"))
                         .AddCode(CreateItemsFromMapCountGenCode("item_counts_progression"))
                         .AddCode(
                              new IfFactory("options.random_portals").AddCode(CreateItemsFromList("portals")).SetElse(
                                  new CodeBlockFactory().AddCode(
                                      CreateItemsFromCountGenCode(
                                          $"{LocationLevelData.Max(data => data.ProgressivePortalCount)}",
                                          "Progressive Portal"
                                      )
                                  )
                              )
                          )
                         .AddCode("filler_items = [key for key, value in filler_weights.items()]")
                         .AddCode("filler_weightings = [value for key, value in filler_weights.items()]")
                         .AddCode(
                              CreateItemsFillRemainingWithItem(
                                  "random.choices(filler_items, filler_weightings)[0]", false
                              )
                          )
                  );

        WriteData("progressiveItemMap", progressiveItemMap.Select(s => s.Trim('"')));
        WriteData("progressiveItemMap", progressiveItemMapMd, "md");
    }

    public override void Rules(WorldFactory _, RuleFactory rule_fact)
    {
        rule_fact
           .AddLogicFunction(
                "area", "has_area",
                $$"""
                  if not state.multiworld.worlds[player].options.random_portals:
                    return {{StateHas("Progressive Portal", "portal_counts[area]", true, false)}}
                  if area.startswith("Sanctum Catacombs"):
                    area = "Catacombs"
                  portal = f"{area} Portal"
                  {{StateHas("portal", stringify: false)}}
                  """, "area"
            )
           .AddLogicFunction(
                "any_area", "has_any_area", "return any(has_area(state, player, area) for area in areas)", "areas"
            )
           .AddLogicFunction(
                "all_areas", "has_all_areas", "return all(has_area(state, player, area) for area in areas)", "areas"
            )
           .AddLogicFunction("quest", "has_quest", StateHas("f\"Complete: {quest}\"", stringify: false), "quest")
           .AddLogicFunction(
                "grind", "can_grind",
                """
                if level > 30: return can_grind(state, player, 30, area_data)
                if level <= 1: return True

                for area in area_data:
                    if not has_area(state, player, area[0]): continue
                    if area[1] <= level <= area[2]: return can_grind(state, player, area[1] - 1, area_data)
                    
                return False
                """, "level", "area_data"
            )
           .AddCompoundLogicFunction("level", "can_grind_level", "grind[level, location_grind_data]", "level")
           .AddCompoundLogicFunction("fish", "can_grind_fish", "grind[level, fishing_grind_data]", "level")
           .AddCompoundLogicFunction("mine", "can_grind_mine", "grind[level, mining_grind_data]", "level")
           .AddCompoundLogicFunction(
                "enemy", "can_beat_enemy", "level[enemy_data[enemy_name][0]] and any_area[enemy_data[enemy_name][1]]",
                "enemy_name"
            )
           .AddLogicFunction("item", "has_item", StateHas("item", "count", false), "item", "count")
           .AddLogicRules(
                QuestData.Where(data => data.Enabled).ToDictionary(data => data.Quest, data => data.GenRule())
            )
           .AddLogicRules(Enumerable.Range(1, 16).ToDictionary(i => $"Reach Level {i * 2}", i => $"level[{i * 2}]"))
           .AddLogicRules(
                MerchantData.SelectMany(data
                    => Enumerable.Range(1, 5).Select(i =>
                        ($"Buy Item #{i} from {data.Name}", $"area[\"{data.Area}\"]")
                    )
                ).ToDictionary(t => t.Item1, t => t.Item2)
            ).AddLogicRules(
                Enumerable.Range(1, 10).Select(i => ($"Fishing Lv. {i}", $"fish[{i}]"))
                          .ToDictionary(t => t.Item1, t => t.Item2)
            ).AddLogicRules(
                Enumerable.Range(1, 10).Select(i => ($"Mining Lv. {i}", $"mine[{i}]"))
                          .ToDictionary(t => t.Item1, t => t.Item2)
            ).AddLogicRules(
                AchievementData.Where(data => data.Enabled).ToDictionary(data => data.Name, data => data.GenRule())
            );
    }

    public override void Regions(WorldFactory worldFactory, RegionFactory region_fact)
    {
        region_fact.AddRegions(LocationLevelData.Select(data => data.Area).ToArray());
        LocationLevelData.Aggregate(
            region_fact,
            (factory, data) => factory.AddConnectionCompiledRule(data.Connection, data.Area, data.GenRule())
        );

        region_fact.AddLocationsFromList("merchants", condition: "options.shop_sanity")
                   .AddLocationsFromList("quests")
                   .AddLocationsFromList("levels")
                   .AddLocationsFromList("professions")
                   .AddLocations(
                        "",
                        AchievementData
                           .Where(data => data.Enabled && data.Class is ClassType.Any)
                           .Select(data => new LocationData(data.Area, data.Name))
                           .ToArray()
                    )
                   .AddEventLocationsFromList(
                        "quests", "f\"Quest Completion: {location[0]}\"", "f\"Complete: {location[0]}\""
                    );

        AchievementData.Where(data => data is { Enabled: true, Class: not ClassType.Any }).GroupBy(data => data.Class)
                       .Aggregate(
                            region_fact,
                            (factory, g) => factory.AddLocations(
                                $"options.is_class(\"{g.Key}\")".ToLower(),
                                g.Select(data => new LocationData(data.Area, data.Name)).ToArray()
                            )
                        );
    }

    public override void Init(WorldFactory factory, WorldInitFactory init_fact)
    {
        var rule_fact = factory.GetRuleFactory();
        init_fact.UseInitFunction()
                 .UseGenerateEarly()
                 .AddUseUniversalTrackerPassthrough(yamlNeeded: false)
                 .UseCreateRegions()
                 .AddCreateItems()
                 .UseSetRules(method =>
                      method.AddCode(
                          $"""
                           options = self.options
                           match options.goal:
                              case 0: #silme_diva
                                  {CreateGoalCondition("enemy['Slime Diva']", rule_fact)}
                              case 1: #lord_zuulneruda
                                  {CreateGoalCondition("enemy['Lord Zuulneruda']", rule_fact)}
                              case 2: #colossus
                                  {CreateGoalCondition("enemy['Colossus']", rule_fact)}
                              case 3: #galius
                                  {CreateGoalCondition("enemy['Galius']", rule_fact)}
                              case 4: #lord_kaluuz
                                  {CreateGoalCondition("enemy['Lord Kaluuz']", rule_fact)}
                              case 5: #valdur
                                  {CreateGoalCondition("enemy['Valdur']", rule_fact)}
                              case 6: #all_bosses
                                  {CreateGoalCondition(string.Join(" and ", EnemyListData.Where(data => data.IsBoss)
                                     .Select(data => $"enemy[\"{data.Name}\"]")), rule_fact)}
                              case 7: #all_quests
                                  {CreateGoalCondition(string.Join(" and ", QuestData.Where(data => data.Enabled && QuestData.All(qData => qData.PrevQuest != data.Quest))
                                     .Select(data => $"quest[\"{data.Quest}\"]")), rule_fact)}
                              case 8: #level_32
                                  {CreateGoalCondition("level[32]", rule_fact)}
                           """
                      )
                  )
                 .UseFillSlotData()
                 .InjectCodeIntoWorld(world => world.AddVariable(new Variable("gen_puml", "False")))
                 .UseGenerateOutput(method => method.AddCode(PumlGenCode()));
    }

    public override void GenerateJson(WorldFactory worldFactory) => worldFactory.GenerateArchipelagoJson(
        ArchipelagoVersion, WorldVersion, "Azrael0534", "Nichologeam", "Sterlia", "SW_CreeperKing"
    );

    public override void GenerateLocations(out string[] locationList, LocationFactory locationFactory)
    {
        locationFactory.GenerateLocationFile(
            out locationList,
            injectCode: factory1 =>
                factory1
                   .AddObject(GetGrindData("location", LocationLevelData.Cast<IFarmingNode>().ToArray()))
                   .AddObject(
                        GetGrindData(
                            "fishing",
                            ProfessionsData.Where(data => data.Profession is "Fishing")
                                           .SelectMany(data => data.GetNodes()).ToArray()
                        )
                    ).AddObject(
                        GetGrindData(
                            "mining",
                            ProfessionsData.Where(data => data.Profession is "Mining")
                                           .SelectMany(data => data.GetNodes()).ToArray()
                        )
                    ).AddObject(
                        new MappedVariable<string, string>(
                            "enemy_data",
                            EnemyListData.ToDictionary(
                                data => $"\"{data.Name}\"",
                                data => $"[{data.Level}, [{string.Join(", ", data.Areas.Select(s => $"\"{s}\""))}]]"
                            )
                        )
                    ).AddObject(
                        new MappedVariable<string, string>(
                            "portal_counts",
                            LocationLevelData.ToDictionary(
                                data => $"\"{data.Area}\"", data => $"{data.ProgressivePortalCount}"
                            )
                        )
                    )
        );

        return;

        ListedVariable<string> GetGrindData(string name, IFarmingNode[] nodes)
        {
            return new ListedVariable<string>($"{name}_grind_data", nodes.Select(node => node.FarmAreaMinMaxLevel()));
        }
    }

    public override void ProcessLocationList(string[] locationList) => WriteData("locations", locationList);
    public override void ProcessItemList(string[] itemList) => WriteData("items", itemList);

    private void GetMaxProgressive(
        string className, ClassType classType, List<string> progressiveItemMapMd, ItemFactory item_fact,
        List<string> progressiveItemMap, out IfFactory addCode
    )
    {
        progressiveItemMapMd.AddRange(
            "<details>", $"<summary><h1 style=\"display: inline\">{className} Progressive Items</h1></summary>"
        );
        item_fact
           .AddItemCountVariable(
                $"{className.ToLower()}_progressives",
                EquipmentItems
                   .Select(kv1 =>
                        {
                            if (!kv1.Value.TryGetValue(classType, out var dict)) return ("", 0);
                            var tiers = dict.Keys.ToArray();

                            if (tiers.Length == 0) return ("", 0);

                            progressiveItemMapMd.AddRange(
                                "<details>",
                                $"<summary><h2 style=\"display: inline\">Progressive {kv1.Key.Str()}</h2></summary>"
                            );
                            progressiveItemMap.AddRange(
                                dict
                                   .Select(kv => kv.Value)
                                   .Select((itemList, progressiveTier)
                                            =>
                                        {
                                            progressiveItemMapMd.AddRange(
                                                "<details>",
                                                $"<summary><h3 style=\"display: inline\">Tier #{progressiveTier + 1}</h3></summary>\n"
                                            );
                                            progressiveItemMapMd.AddRange(
                                                itemList.Select(item => $"  - {item.Name} (lv. {item.LevelReq})")
                                            );

                                            progressiveItemMapMd.Add("</details>");
                                            return
                                                $"Progressive {className} {kv1.Key.Str()}|{progressiveTier + 1}|{string.Join(",", itemList.Select(item => item.Name))}";
                                        }
                                    )
                            );

                            progressiveItemMapMd.Add("</details>");

                            return ($"Progressive {className} {kv1.Key.Str()}", tiers.Length);
                        }
                    )
                   .Where(t => t.Item2 > 0)
                   .ToDictionary(t => t.Item1, t => t.Item2),
                Useful
            );

        progressiveItemMapMd.Add("</details>");
        addCode =
            new IfFactory($"options.is_class('{classType}')".ToLower()).AddCode(
                CreateItemsFromMapCountGenCode($"{classType}_progressives".ToLower())
            );
    }

}