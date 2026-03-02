using CreepyUtil.Archipelago.WorldFactory;
using static CreepyUtil.Archipelago.WorldFactory.ItemFactory.ItemClassification;
using static CreepyUtil.Archipelago.WorldFactory.PremadePython;

namespace ApWorldFactories.Games.Slime_Rancher;

public class SlimeRancher : BuildData
{
    public override string SteamDirectory => DDrive;
    public override string ModFolderName => "SW_CreeperKing.Slimipelago";
    public override string GameName => "Slime Rancher";
    public override string ApWorldName => "slime_rancher";
    public override string GoogleSheetId => "15PdrnGmkYdocX9RU-D5U_9OgihRNN9axX71mm-jOPUQ";
    public override string WorldVersion => "0.2.3";

    public override Dictionary<string, string> SheetGids { get; }
        = new() { ["GameData"] = "72469245", ["NewLogic"] = "2113673017" };

    private InteractableRowData[] RawInteractables = [];
    private GateRowData[] Gates = [];
    private GordoRowData[] Gordos = [];
    private UpgradeRowData[] Upgrades = [];
    private CorporateRowData[] CorporateLocations = [];
    private RegionData[] RegionData = [];
    private ItemAmountData[] ItemAmountData = [];

    private string[] Zones = [];
    private Dictionary<string, string[]> BackwardsConnections = [];
    private Dictionary<string, int> NonProgressiveUsefulItemCount = [];
    private Dictionary<string, int> ProgressiveUsefulItemCount = [];
    private Dictionary<string, int> ProgressiveProgressionItemCount = [];
    private string[] FillerItems = [];
    private InteractableRowData[] Interactables = [];
    private InteractableRowData[] DlcInteractables = [];

    public override void RunShenanigans()
    {
        GetSpreadsheet("GameData")
           .ToFactory()
           .ReadTable(new RegionDataCreator(), out RegionData).SkipColumn()
           .ReadTable(out ItemAmountData);

        Zones = RegionData.Select(data => data.Region).ToArray();
        BackwardsConnections = RegionData.ToDictionary(data => data.Region, data => data.BackConnections);

        NonProgressiveUsefulItemCount = ItemAmountData.Where(data => data.ProgType is "nonprog_useful")
                                                      .ToDictionary(
                                                           data => data.Item, data => int.Parse(data.Count)
                                                       );

        ProgressiveUsefulItemCount = ItemAmountData.Where(data => data.ProgType is "prog_useful")
                                                   .ToDictionary(data => data.Item, data => int.Parse(data.Count));
        ProgressiveProgressionItemCount = ItemAmountData.Where(data => data.ProgType is "prog_prog")
                                                        .ToDictionary(
                                                             data => data.Item, data => int.Parse(data.Count)
                                                         );
        FillerItems = ItemAmountData.Where(data => data.ProgType is "filler").Select(data => data.Item).ToArray();

        GetSpreadsheet("main")
           .ToFactory()
           .ReadTable(new InteractableCreator(Zones), out RawInteractables).SkipColumn()
           .ReadTable(new GateCreator(), out Gates).SkipColumn()
           .ReadTable(new GordoCreator(), out Gordos).SkipColumn()
           .ReadTable(new UpgradeCreator(), out Upgrades).SkipColumn()
           .ReadTable(new CorporateCreator(), out CorporateLocations);

        Interactables = RawInteractables.Where(line => !line.IsSecretStyle).ToArray();
        DlcInteractables = RawInteractables.Where(line => line.IsSecretStyle).ToArray();

        WriteData(
            "Logic",
            Interactables.Concat(DlcInteractables).Select(line => $"{line.Name}:{line.GenRule(false)}:{line.Area}")
                         .Where(s => s != "")
        );

        var noteLocations = File.Exists($"{WriteOutputDirectory}/NoteLocations.txt")
            ? File.ReadAllLines($"{WriteOutputDirectory}/NoteLocations.txt").ToList() : [];
        noteLocations.AddRange(
            Interactables.Where(inter => inter.IsNote && !noteLocations.Contains(inter.Id)).Select(inter => inter.Id)
        );
        WriteData("NoteLocations", noteLocations);

        WriteData(
            "Locations",
            RawInteractables.Select(line => line.GetText).Concat(Gates.Select(line => line.GetText))
                            .Concat(Gordos.Select(line => line.GetText))
        );
        WriteData("Upgrades", Upgrades.Select(line => $"{line.Name},{line.Id}"));
        WriteData("7Zee", CorporateLocations.Select(line => $"{line.Location},{line.Level}"));
    }

    public override void Options(WorldFactory _, OptionsFactory options_fact)
    {
        options_fact
           .AddOption(
                "Goal Type", """
                             What criteria to goal
                             notes = read all notes
                             7Zee = buy all 7Zee ranks
                             credits = get credits
                             """, new Choice(0, "notes", "7Zee", "credits")
            )
           .AddOption("Start With Dry Reef", "Start with the Dry Reef unlocked", new DefaultOnToggle())
           .AddOption(
                "Enable Stylish Dlc Treasure Pods", "note: THIS WILL NOT GIVE YOU DLC\nYOU MUST __***OWN***__ THE DLC",
                new Toggle()
            )
           .AddOption(
                "Treasure Cracker Checks", """
                                           which levels of the treasure cracker is considered as checks
                                           default: level 1
                                           level 1 requires crafting 1 gadget
                                           level 2 requires crafting 20 gadgets
                                           level 3 requires crafting 50 gadgets
                                           """, new CreepyUtil.Archipelago.WorldFactory.Range(1, 0, 3)
            )
           .AddOption(
                "Include 7z", "Include unlockables behind 7z as checks\nestimated to appear in sphere 2 and above",
                new Toggle()
            )
           .AddOption(
                "Fix Market Rates", """
                                    Overrides the default market behavior:
                                    instead of https://slimerancher.fandom.com/wiki/Plort_Market_(Slime_Rancher)
                                    it will make all plort prices 150% base value, base value listed in the above link
                                    """, new DefaultOnToggle()
            )
           .AddOption("Start With Drone", "Start with a Drone", new DefaultOnToggle())
           .AddOption(
                "Trap Percent", "what percent of filler should be replaced with traps",
                new CreepyUtil.Archipelago.WorldFactory.Range(15, 0, 100)
            )
           .AddCheckOptions(method =>
                method.AddCode(
                    """
                    if options.goal_type == 1 and not options.include_7z:
                        raise_yaml_error(world.player, "7Zee goal type requires you to include 7Zee locations")
                    """
                )
            );
    }

    public override void Locations(WorldFactory _, LocationFactory location_fact)
    {
        location_fact
            // .AddLocations("zones", Zones, addToFinalList: false)
            // .AddLocations("backwards_connections", BackwardsConnections, addToFinalList: false)
           .AddLocations("upgrades", Upgrades.Select(line => line.Name))
           .AddLocations(
                "upgrades_7z", Upgrades.Where(up => up.Rule is "7z").Select(up => up.Name), addToFinalList: false
            )
           .AddLocations("interactables", Interactables.Select(line => (string[])[line.Name, line.Area]))
           .AddLocations("dlc_interactables", DlcInteractables.Select(line => (string[])[line.Name, line.Area]))
           .AddLocations(
                "corporate_locations", CorporateLocations.Select(line => (string[])[line.Location, line.Area])
            );
    }

    public override void Items(WorldFactory _, ItemFactory item_fact)
    {
        item_fact
           .AddItemListVariable(
                "region_unlocks", Progression, true, true, Zones.Skip(2).Select(zone => $"Region Unlock: {zone}").ToArray()
            )
           .AddItemCountVariable("non_progressive_useful_items", NonProgressiveUsefulItemCount, Useful)
           .AddItemCountVariable("progressive_useful_item_count", ProgressiveUsefulItemCount, Useful)
           .AddItemCountVariable("progressive_progression_item_count", ProgressiveProgressionItemCount, Progression)
           .AddItemListVariable("filler_items", Filler, true, true, FillerItems)
           .AddItem("Trap Slime", Trap)
           .AddCreateItems(func => func.AddCode(
                                            """
                                            for zone in region_unlocks:
                                                if "Reef" in zone and options.start_with_dry_reef: continue
                                                world.location_count -= 1
                                                pool.append(world.create_item(zone))
                                            """
                                        ).AddNewLine()
                                       .AddCode(CreateItemsFromMapCountGenCode("non_progressive_useful_items"))
                                       .AddNewLine()
                                       .AddCode(CreateItemsFromMapCountGenCode("progressive_useful_item_count"))
                                       .AddNewLine()
                                       .AddCode(CreateItemsFromMapCountGenCode("progressive_progression_item_count"))
                                       .AddNewLine()
                                       .AddCode(
                                            CreateItemsFromCountGenCode(
                                                "int(world.location_count * (options.trap_percent / 100))", "Trap Slime"
                                            )
                                        ).AddNewLine()
                                       .AddCode(CreateItemsFillRemainingWith("filler_items"))
            );
    }

    public override void Rules(WorldFactory _, RuleFactory rule_fact)
    {
        rule_fact
           .AddLogicFunction("cracker", "has_cracker", StateHas("Progressive Treasure Cracker", "level"), "level")
           .AddLogicFunction("energy", "has_energy", StateHas("Progressive Max Energy", "amount"), "amount")
           .AddLogicFunction("jetpack", "has_jetpack", StateHas("Progressive Jetpack"))
           .AddLogicFunction("region", "has_region", StateHas("f'Region Unlock: {region}'", stringify: false), "region")
           .AddCompoundLogicFunction("Reef", "can_access_dry_reef", "region['Dry Reef']")
           .AddCompoundLogicFunction(
                "ToRuins", "can_access_to_ruins_from_trans",
                "region['Indigo Quarry'] and region['Moss Blanket'] and region['Ancient Ruins']"
            )
           .AddCompoundLogicFunction(
                "7z", "can_access_7zee",
                "Reef and ToRuins"
            )
           .AddCompoundLogicFunction("Lab", "can_access_lab", "Reef and region['Indigo Quarry'] and region['The Lab']")
           .AddLogicRules(RawInteractables.ToDictionary(inter => inter.Name, inter => inter.GenRule(true)))
           .AddLogicRules(Upgrades.ToDictionary(up => up.Name, up => up.Rule));
    }

    public override void Regions(WorldFactory _, RegionFactory region_fact)
    {
        region_fact.AddRegions(BackwardsConnections.Keys.ToArray());

        BackwardsConnections
           .Aggregate(
                region_fact, (factory1, zoneKv) =>
                {
                    if (zoneKv.Key is "Ancient Ruins")
                    {
                        return factory1.AddConnectionCompiledRule(zoneKv.Value[0], zoneKv.Key, "ToRuins");
                    }

                    foreach (var backConnection in zoneKv.Value)
                    {
                        if (backConnection is "Menu")
                        {
                            factory1.AddConnection("Menu", zoneKv.Key);
                            continue;
                        }

                        factory1.AddConnectionCompiledRule(backConnection, zoneKv.Key, $"region[\"{zoneKv.Key}\"]");
                    }

                    return factory1;
                }
            );

        foreach (var upgrade in Upgrades)
        {
            var condition = "";

            if (upgrade.Rule is "7z") { condition = "world.options.include_7z"; }
            else if (upgrade.Name.Contains("Treasure Cracker"))
            {
                condition
                    = $"\"{upgrade.Name}\" > f\"Buy Personal Upgrade (Treasure Cracker lv.{{world.options.treasure_cracker_checks}})\"";
            }

            region_fact.AddLocation(new LocationData("Upgrades", upgrade.Name), condition);
        }

        region_fact
           .AddLocationsFromList("interactables")
           .AddEventLocations(
                "world.options.goal_type == 0",
                Interactables
                   .Where(inter => inter.Name.Contains("Hobson's Note"))
                   .Select(inter => new EventLocationData(inter.Area, $"Read: {inter.Name}", "Note Read", inter.Name))
                   .ToArray()
            )
           .AddLocationsFromList("dlc_interactables", condition: "world.options.enable_stylish_dlc_treasure_pods")
           .AddLocationsFromList("corporate_locations", condition: "world.options.include_7z")
           .AddEventLocationsFromList(
                "corporate_locations", "f\"Bought: {location[0]}\"", "\"7Zee Bought\"",
                condition: "world.options.include_7z and world.options.goal_type == 1"
            );
    }

    public override void Init(WorldFactory _, WorldInitFactory init_fact)
    {
        init_fact
           .AddItemNameGroups(new Dictionary<string, string> { ["unlocks"] = "region_unlocks" })
           .UseInitFunction()
           .UseGenerateEarly()
           .AddUseUniversalTrackerPassthrough(yamlNeeded: false)
           .UseGenerateEarly(method =>
                method.AddCode(CreatePushPrecollected("Region Unlock: Dry Reef", "self.options.start_with_dry_reef"))
                      .AddCode(CreatePushPrecollected("Drone", "self.options.start_with_drone"))
            )
           .UseCreateRegions()
           .AddCreateItems()
           .UseSetRules(method =>
                method.AddCode(
                    $"""
                     if self.options.goal_type == 0:
                         {CreateGoalCondition(StateHas("Note Read", "28", returnValue: false))}
                     elif self.options.goal_type == 1:
                         {CreateGoalCondition(StateHas("7Zee Bought", "len(corporate_locations)", returnValue: false))}
                     elif self.options.goal_type == 2:
                         {CreateGoalCondition(StateHasAll("region_unlocks[3:]", false, false))}
                     """
                )
            )
           .UseFillSlotData(
                new Dictionary<string, string> { ["uuid"] = "str(shuffled)" },
                method => method.AddCode(CreateUniqueId())
            )
           .InjectCodeIntoWorld(world => world.AddVariable(new Variable("gen_puml", "False")))
           .UseGenerateOutput(method => method.AddCode(PumlGenCode()));
    }
}