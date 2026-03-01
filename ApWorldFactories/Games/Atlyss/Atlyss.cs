using System.Text;
using CreepyUtil.Archipelago.WorldFactory;
using CreepyUtil.ClrCnsl;
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
    public override Dictionary<string, string> SheetGids => new() { ["items"] = "1796499840" };

    private LocationLevelData[] LocationLevelData = [];
    private EnemyListData[] EnemyListData = [];
    private QuestData[] QuestData = [];
    private ProfessionsData[] ProfessionsData = [];
    private MerchantData[] MerchantData = [];
    private ItemData[] ItemData = [];
    private Dictionary<ItemType, Dictionary<ClassType, Dictionary<int, List<ItemData>>>> EquipmentItems = [];

    public override void RunShenanigans()
    {
        GetSpreadsheet("main")
           .ToFactory()
           .ReadTable(out LocationLevelData).SkipColumn()
           .ReadTable(out EnemyListData).SkipColumn()
           .ReadTable(out QuestData).SkipColumn()
           .ReadTable(out ProfessionsData).SkipColumn()
           .ReadTable(out MerchantData);
        GetSpreadsheet("items").ToFactory().ReadTable(out ItemData);

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

        // var color1 = new Color(87, 153, 209);
        // var color2 = new Color(49, 119, 180);
        // var color3 = new Color(33, 79, 120);
        // var color4 = new Color(6, 40, 60);
        // var folder = new Folder("Equipment") { Color = color1 };
        // foreach (var (itemType, itemClassDict) in EquipmentItems)
        // {
        //     var itemListFolder = new Folder($"{itemType} ({itemClassDict.Count})");
        //     itemListFolder.Color = color2;
        //     foreach (var (classType, itemList) in itemClassDict )
        //     {
        //         var tierListFolder = new Folder($"{classType} ({itemList.Count})");
        //         tierListFolder.Color = color3;
        //         foreach (var (tier, tierList) in itemList.OrderBy(item => item.Key))
        //         {
        //             var tierFolder = new Folder($"Tier {tier} ({tierList.Count})");
        //             tierFolder.Color = color4;
        //             foreach (var item in tierList.OrderBy(item => item.LevelReq).ThenBy(item => item.Name))
        //             {
        //                 tierFolder.Items.Add($"{item.Name} (lv. {item.LevelReq})");
        //             }
        //             
        //             tierListFolder.SubFolders.Add(tierFolder);
        //         }
        //         
        //         itemListFolder.SubFolders.Add(tierListFolder);
        //     }
        //
        //     folder.SubFolders.Add(itemListFolder);
        // }
        // folder.Display();
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
                         "Equipment Progression",
                         """
                         How equipment is distributed.
                         Gated (default): Equipment has level requirements. Higher tier gear only
                         appears at locations accessible at appropriate levels. Tier 1 gear (lv 1-5)
                         can appear anywhere; Tier 5 gear (lv 21-26) only at endgame locations.
                         Random: Equipment can appear anywhere with no level gating. You may find
                         endgame weapons in early spheres — chaotic but fun.
                         """, new Choice(0, "gated", "random")
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
                         new Choice(0, "random", "fighter", "bandit", "mystic")
                     )
                    .AddOption(
                         "Secondary Class", "What you chose to be as your secondary class",
                         new Choice(0, "random", "fighter", "bandit", "mystic", "none")
//                      )
//                     .AddOption(
//                          "Class Filler",
//                          """
//                          Filter equipment in the item pool to only include gear for specific classes.
//                          Weapons are filtered by scaling stat (STR=Fighter, MIND=Mystic, DEX=Bandit).
//                          Class-locked armor is filtered by its required class.
//                          Shields are included for Fighter and Mystic (one-handed weapon users).
//                          Universal armor (helms, capes, trinkets, cosmetics) is always included.
//
//                          false (default): No filtering, all equipment is in the pool.
//                          Single class: Only that class's weapons and armor are included.
//                          true: Chosen classes' equipment is included.
//                          """,
//                          new Toggle()
                     ).AddCheckOptions();
    }

    public override void Locations(WorldFactory _, LocationFactory location_fact)
    {
        location_fact.AddLocations(
                          "quests",
                          QuestData.Where(data => data.Enabled)
                                   .Select(data => (string[])[data.Quest, data.AreaAccepted])
                      )
                     .AddLocations("levels", Enumerable.Range(1, 16).Select(i => $"Reach Level {i * 2}"))
                     .AddLocations(
                          "merchants",
                          MerchantData.SelectMany(data
                              => Enumerable.Range(1, 5).Select(i => (string[])
                                  [$"Buy Item #{i} from {data.Name}", data.Area]
                              )
                          )
                      );
    }

    public override void Items(WorldFactory _, ItemFactory item_fact)
    {
        List<string> progressiveItemMap = [];
        List<string> progressiveItemMapMd = [];
        GetMaxProgressive("Any", ClassType.Any);
        progressiveItemMapMd.Add("\n---\n");
        GetMaxProgressive("Fighter", ClassType.Fighter);
        progressiveItemMapMd.Add("\n---\n");
        GetMaxProgressive("Mystic", ClassType.Mystic);
        progressiveItemMapMd.Add("\n---\n");
        GetMaxProgressive("Bandit", ClassType.Bandit);

        WriteData("progressiveItemMap", progressiveItemMap);
        WriteData("progressiveItemMap", progressiveItemMapMd, "md");
        return;

        void GetMaxProgressive(string className, ClassType classType)
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
                    ItemFactory.ItemClassification.ProgressionSkipBalancing
                );

            progressiveItemMapMd.Add("</details>");
        }
    }

    public override void Rules(WorldFactory _, RuleFactory rule_fact)
    {
        rule_fact
           .AddLogicFunction(
                "grind", "can_grind",
                """
                if level > 26: return can_grind(26)
                if level <= 1: return True

                for area in area_data:
                    if not has_area(area[0]): continue
                    if area[1] <= level <= area[2]: return can_grind(area[1] - 1)
                    
                return False
                """, "level", "area_data"
            )
           .AddLogicFunction("area", "has_area", StateHas("area", stringify: false), "area")
           .AddLogicFunction("quest", "has_quest", StateHas("f\"Complete: {quest}\"", stringify: false), "quest")
           .AddCompoundLogicFunction("level", "can_grind_level", "grind[level, location_grind_data]", "level");
    }

    public override void Regions(WorldFactory worldFactory, RegionFactory region_fact)
    {
        region_fact.AddRegions(LocationLevelData.Select(data => data.Area).ToArray());
        LocationLevelData.Aggregate(
            region_fact,
            (factory, data) => factory.AddConnectionCompiledRule(data.Connection, data.Area, data.GenRule())
        );
    }

    public override void Init(WorldFactory _, WorldInitFactory init_fact)
    {
        init_fact.UseInitFunction(method =>
            method
               .AddCode(new Variable("primary_class", "\"\""))
               .AddCode(new Variable("secondary_class", "\"\""))
        );
    }

    public override void GenerateJson(WorldFactory worldFactory) => worldFactory.GenerateArchipelagoJson(
        ArchipelagoVersion, WorldVersion, "Azrael0534", "Nichologeam", "Sterlia", "SW_CreeperKing"
    );

    public override void GenerateLocations(out string[] locationList, LocationFactory locationFactory)
    {
        locationFactory.GenerateLocationFile(
            out locationList,
            injectCode: factory1 => factory1.AddObject(
                new ListedVariable<string>(
                    "location_grind_data",
                    LocationLevelData.Select(data => $"[\"{data.Area}\", {data.LevelMin}, {data.LevelMax}]")
                )
            )
        );
    }

    public override void ProcessLocationList(string[] locationList) => WriteData("locations", locationList);
    public override void ProcessItemList(string[] itemList) => WriteData("items", itemList);
}