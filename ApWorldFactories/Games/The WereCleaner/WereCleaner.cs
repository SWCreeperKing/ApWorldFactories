using CreepyUtil.Archipelago.WorldFactory;
using static CreepyUtil.Archipelago.WorldFactory.PremadePython;

namespace ApWorldFactories.Games.The_WereCleaner;

public class WereCleaner() : BuildData(
    DDrive, "The WereCleaner", "SW_CreeperKing.Werepelago", "the_werecleaner",
    "1wrzYGdzRh6-fmsBK-dAzhe72PdNrhvEZKUcQJCXDaJ4", "0.1.2"
)
{
    private LevelData[] LevelData = [];
    private ItemData[] ItemData = [];
    private NpcData[] NpcData = [];

    private string[] RawNpcs = [];
    private string[] AllNpcs = [];
    private string[] Days = [];
    private string[] DayUnlocks = [];
    private string[] Collectibles = [];
    private string[] Abilities = [];

    public override void RunShenanigans()
    {
        GetSpreadsheet("main")
           .ToFactory()
           .ReadTable(new DataCreator<LevelData>(), 3, out LevelData).SkipColumn()
           .ReadTable(new DataCreator<ItemData>(), 3, out ItemData).SkipColumn()
           .ReadTable(new DataCreator<NpcData>(), 3, out NpcData);

        RawNpcs = NpcData.SelectMany(data => data.Npcs).Where(s => s.Trim() is not "").ToHashSet().ToArray();
        AllNpcs = RawNpcs.Select(data => $"Kill {data}").ToArray();
        Days = LevelData.Select(data => $"Survive {data.LevelName} Night").ToArray();
        DayUnlocks = LevelData.Select(data => $"Unlock {data.LevelName} Night").ToArray();
        Collectibles = ItemData.Select(data => data.Collectible).ToArray();
        Abilities = ItemData.Select(data => data.Ability).Where(s => s is not "").ToArray();

        WriteData("levelIds", NpcData.Select(data => $"{data.LevelName}:{data.LevelId}"));
        WriteData("itemIds", ItemData.Select(data => $"{data.Collectible}:{data.CollectibleId}"));
    }

    public override void Options(WorldFactory _, OptionsFactory options_fact)
    {
        // options_fact.AddOption("Kill Sanity", "Killing each unique npc sends a check", new Toggle())
        options_fact.AddCheckOptions();
    }

    public override void Locations(WorldFactory _, LocationFactory location_fact)
    {
        location_fact
           .AddLocations(
                "starting_checks",
                [["Starting Check (Washer)", "Menu"], ["Starting Check (Unlock Monday Night)", "Menu"]]
            )
           .AddLocations("collectibles", Collectibles.Select(s => (string[])[s, "Collectibles"]))
           .AddLocations("npcs", AllNpcs.Select(s => (string[])[s, "Killsanity"]))
           .AddLocations("levels", Days.Select(s => (string[])[s, "Levels"]));
    }

    public override void Items(WorldFactory _, ItemFactory item_fact)
    {
        item_fact
           .AddItems(ItemFactory.ItemClassification.Progression, items: Abilities)
           .AddItems(ItemFactory.ItemClassification.Progression, items: DayUnlocks)
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
            );
    }

    public override void Rules(WorldFactory _, RuleFactory rule_fact)
    {
        rule_fact
           .AddLogicFunction("level", "has_level", StateHas("f\"Unlock {level} Night\"", stringify: false), "level")
           .AddLogicFunction("Washer", "has_washer", StateHas("Washer"))
           .AddLogicFunction("Vacuum", "has_vacuum", StateHas("Vacuum"))
           .AddLogicFunction("Knapper", "has_knapper", StateHas("Knapper"))
           .AddLogicRules(
                Collectibles.ToDictionary(
                    s => s, s => string.Join(
                        " or ", LevelData.Where(data => data.Collectibles.Contains(s))
                                         .Select(s => $"level[\"{s.LevelName}\"]")
                    )
                )
            ).AddLogicRules(
                RawNpcs.ToDictionary(
                    s => $"Kill {s}", s => string.Join(
                        " or ", NpcData.Where(data => data.Npcs.Contains(s))
                                       .Select(s => $"level[\"{s.LevelName}\"]")
                    )
                )
            ).AddLogicRules(
                LevelData.ToDictionary(
                    data => $"Survive {data.LevelName} Night",
                    data => $"level[\"{data.LevelName}\"] and {string.Join(" and ", data.Abilities)}"
                )
            );
    }

    public override void Regions(WorldFactory _, RegionFactory region_fact)
    {
        region_fact
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
            );
    }

    public override void Init(WorldFactory _, WorldInitFactory init_fact)
    {
        init_fact
           .UseInitFunction(method => method.AddCode(new Variable("self.starting_stage", "\"\"")))
           .AddUseUniversalTrackerPassthrough(yamlNeeded: false)
            // .UseGenerateEarly(method => method.AddCode(CreatePushPrecollected("Unlock Monday Night")))
           .UseCreateRegions()
           .AddCreateItems()
           .UseSetRules(method => method
               .AddCode(CreateGoalCondition(StateHas("Nights Survived", "7", returnValue: false)))
            )
           .UseFillSlotData(new Dictionary<string, string> { ["Kyle"] = "str(\"Best Boi\")" })
           .InjectCodeIntoWorld(world => world.AddVariable(new Variable("gen_puml", "False")))
           .UseGenerateOutput(method => method.AddCode(PumlGenCode()));
    }
}