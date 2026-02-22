using CreepyUtil.Archipelago.WorldFactory;
using static CreepyUtil.Archipelago.WorldFactory.PremadePython;

namespace ApWorldFactories.Games;

public class WereCleaner() : BuildData(
    DDrive, "The WereCleaner", "SW_CreeperKing.Werepelago", "the_werecleaner", "1wrzYGdzRh6-fmsBK-dAzhe72PdNrhvEZKUcQJCXDaJ4", "0.1.2"
)
{
    public override void RunShenanigans(WorldFactory factory)
    {
        GetSpreadsheet("main")
           .ToFactory()
           .ReadTable(new DataCreator<LevelData>(), 3, out var levelData).SkipColumn()
           .ReadTable(new DataCreator<ItemData>(), 3, out var itemData).SkipColumn()
           .ReadTable(new DataCreator<NpcData>(), 3, out var npcData);

        var rawNpcs = npcData.SelectMany(data => data.Npcs).Where(s => s.Trim() is not "").ToHashSet().ToArray();
        var allNpcs = rawNpcs.Select(data => $"Kill {data}").ToArray();
        var days = levelData.Select(data => $"Survive {data.LevelName} Night").ToArray();
        var dayUnlocks = levelData.Select(data => $"Unlock {data.LevelName} Night").ToArray();
        var collectibles = itemData.Select(data => data.Collectible).ToArray();
        var abilities = itemData.Select(data => data.Ability).Where(s => s is not "").ToArray();

        factory.GetOptionsFactory(GitLink)
                // .AddOption("Kill Sanity", "Killing each unique npc sends a check", new Toggle())
               .AddCheckOptions()
               .GenerateOptionFile();

        factory.GetLocationFactory(GitLink)
               .AddLocations(
                    "starting_checks",
                    [["Starting Check (Washer)", "Menu"], ["Starting Check (Unlock Monday Night)", "Menu"]]
                )
               .AddLocations("collectibles", collectibles.Select(s => (string[])[s, "Collectibles"]))
               .AddLocations("npcs", allNpcs.Select(s => (string[])[s, "Killsanity"]))
               .AddLocations("levels", days.Select(s => (string[])[s, "Levels"]))
               .GenerateLocationFile();

        factory.GetItemFactory(GitLink)
               .AddItems(ItemFactory.ItemClassification.Progression, items: abilities)
               .AddItems(ItemFactory.ItemClassification.Progression, items: dayUnlocks)
               .AddItem("Floor Penny", ItemFactory.ItemClassification.Filler)
               .AddCreateItems(method => method
                                        .AddCode(CreateItemsFromClassificationList())
                                         // .AddCode("""
                                         //                 for item, classification in item_table.items():
                                         //                     if item != "Unlock Monday Night":
                                         //                         world.location_count -= 1
                                         //                         pool.append(world.create_item(item))
                                         //                 """)
                                        .AddCode(CreateItemsFillRemainingWithItem("Floor Penny"))
                )
               .GenerateItemsFile();

        factory.GetRuleFactory(GitLink)
               .AddLogicFunction("level", "has_level", StateHas("f\"Unlock {level} Night\"", stringify:false), "level")
               .AddLogicFunction("Washer", "has_washer", StateHas("Washer"))
               .AddLogicFunction("Vacuum", "has_vacuum", StateHas("Vacuum"))
               .AddLogicFunction("Knapper", "has_knapper", StateHas("Knapper"))
               .AddLogicRules(
                    collectibles.ToDictionary(
                        s => s, s => string.Join(
                            " or ", levelData.Where(data => data.Collectibles.Contains(s))
                                             .Select(s => $"level[\"{s.LevelName}\"]")
                        )
                    )
                ).AddLogicRules(
                    rawNpcs.ToDictionary(
                        s => $"Kill {s}", s => string.Join(
                            " or ", npcData.Where(data => data.Npcs.Contains(s))
                                           .Select(s => $"level[\"{s.LevelName}\"]")
                        )
                    )
                ).AddLogicRules(
                    levelData.ToDictionary(
                        data => $"Survive {data.LevelName} Night",
                        data => $"level[\"{data.LevelName}\"] and {string.Join(" and ", data.Abilities)}"
                    )
                )
               .GenerateRulesFile();

        factory.GetRegionFactory(GitLink)
               .AddRegions("Levels", "Collectibles", "Killsanity")
               .AddConnection("Menu", "Levels")
               .AddConnection("Menu", "Collectibles")
                // .AddConnection("Menu", "Killsanity", condition: "world.options.kill_sanity")
               .AddConnection("Menu", "Killsanity")
               .AddLocationsFromList("starting_checks")
               .AddLocationsFromList("collectibles")
               .AddLocationsFromList("levels")
                // .AddLocationsFromList("npcs", condition: "world.options.kill_sanity")
               .AddLocationsFromList("npcs")
               .AddEventLocationsFromList(
                    "levels", "f\"Beat: {location[0]}\"", "\"Nights Survived\""
                )
               .GenerateRegionFile();

        factory.GetInitFactory(GitLink)
               .UseInitFunction(method => method.AddCode(new Variable("self.starting_stage", "\"\"")))
               .AddUseUniversalTrackerPassthrough(yamlNeeded: false)
                // .UseGenerateEarly(method => method.AddCode(CreatePushPrecollected("Unlock Monday Night")))
               .UseCreateRegions()
               .AddCreateItems()
               .UseSetRules(method => method
                   .AddCode(CreateGoalCondition(StateHas("Nights Survived", "7", returnValue:false)))
                )
               .UseFillSlotData(new Dictionary<string, string> { ["Kyle"] = "str(\"Best Boi\")" })
               .InjectCodeIntoWorld(world => world.AddVariable(new Variable("gen_puml", "False")))
               .UseGenerateOutput(method => method.AddCode(PumlGenCode()))
               .GenerateInitFile();

        WriteData("levelIds", npcData.Select(data => $"{data.LevelName}:{data.LevelId}"));
        WriteData("itemIds", itemData.Select(data => $"{data.Collectible}:{data.CollectibleId}"));
    }
}

public readonly struct LevelData(string[] param)
{
    public readonly string LevelName = param[0];
    public readonly string[] Collectibles = param[1].Split(',').Select(s => s.Trim()).ToArray();
    public readonly string[] Abilities = param[2].Split(',').Select(s => s.Trim()).ToArray();
}

public readonly struct ItemData(string[] param)
{
    public readonly string Collectible = param[0];
    public readonly string CollectibleId = param[1];
    public readonly string Ability = param[2];
}

public readonly struct NpcData(string[] param)
{
    public readonly string LevelName = param[0];
    public readonly string LevelId = param[1];
    public readonly string[] Npcs = param[2].Split(',').Select(s => s.Trim()).ToArray();
}