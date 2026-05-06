using ApWorldFactories.Graphviz;
using CreepyUtil.Archipelago.WorldFactory;
using static ApWorldFactories.PathConstants;
using static CreepyUtil.Archipelago.WorldFactory.ItemFactory.ItemClassification;
using static CreepyUtil.Archipelago.WorldFactory.PremadePython;

namespace ApWorldFactories.Games.Slime_Rancher;

public class SlimeRancher : BuildData
{
    private const string Retreat = "Ogden's Retreat";
    private const string Manor = "Mochi's Manor";
    private const string Workshop = "Viktor's Workshop";

    public override string SteamDirectory => DDrive;
    public override string ModFolderName => "SW_CreeperKing.Slimipelago";
    public override string GameName => "Slime Rancher";
    public override string ApWorldName => "slime_rancher";
    public override string GoogleSheetId => "15PdrnGmkYdocX9RU-D5U_9OgihRNN9axX71mm-jOPUQ";
    public override string WorldVersion => "0.3.1";

    public override Dictionary<string, string> SheetGids { get; } = new()
    {
        ["GameData"] = "72469245", ["NewLogic"] = "2113673017"
    };

    private UpgradeRowData[] Upgrades = [];
    private CorporateRowData[] CorporateLocations = [];
    private ItemAmountData[] ItemAmountData = [];
    private RegionUnlockRowData[] RegionUnlockData = [];
    private RegionSector[] RegionSector = [];
    private InteractableSector[] InteractableSector = [];
    private GateRowData[] GateData = [];
    private GordoRowData[] GordoData = [];

    private Dictionary<string, int> NonProgressiveUsefulItemCount = [];
    private Dictionary<string, int> ProgressiveUsefulItemCount = [];
    private Dictionary<string, int> ProgressiveProgressionItemCount = [];
    private Dictionary<string, string> LocationMap = [];

    private string[] FillerItems = [];
    private string[] PlortTypes = [];
    private Dictionary<string, List<string>> NormalPlortPlacement = [];
    private Dictionary<string, List<string>> MarketPlortPlacement = [];

    public override void RunShenanigans()
    {
        GetSpreadsheet("NewLogic")
           .SkipColumn(9)
           .ReadTable(out RegionRowData[] rawRegionData).SkipColumn()
           .ReadTable(out SlimeRowData[] rawSlimeData).SkipColumn()
           .ReadTable(out LocationNameGroupData[] rawLocationGroups);

        GetSpreadsheet()
           .ReadTable(out InteractableRowData[] rawInteractableData).SkipColumn()
           .ReadTable(new GateCreator(), out GateData).SkipColumn()
           .ReadTable(new GordoCreator(), out GordoData).SkipColumn()
           .ReadTable(new UpgradeCreator(), out Upgrades).SkipColumn()
           .ReadTable(new CorporateCreator(), out CorporateLocations);

        LocationMap = rawLocationGroups.SelectMany(data => data.Locations.Select(reg => (reg, data.Group)))
                                       .ToDictionary(t => t.reg, t => t.Group);

        PlortTypes = rawSlimeData.GroupBy(data => data.PlortDrop).Select(g => g.Key).Where(s => s is not "N/A")
                                 .Distinct().ToArray();

        NormalPlortPlacement = rawSlimeData.Where(data => data.SkipLogic is SkipLogic.None)
                                           .SelectMany(data => data.SpawnLocations
                                                                   .Select(loc => (string[])[loc, data.Slime]).ToArray()
                                            ).GroupBy(arr => arr[0]).ToDictionary(
                                                g => g.Key, g => g.Select(arr => arr[1][..^6]).ToList()
                                            );

        MarketPlortPlacement = rawSlimeData.Where(data => data.SkipLogic is SkipLogic.MarketLogic)
                                           .SelectMany(data => data.SpawnLocations
                                                                   .Select(loc => (string[])[loc, data.Slime]).ToArray()
                                            ).GroupBy(arr => arr[0]).ToDictionary(
                                                g => g.Key, g => g.Select(arr => arr[1][..^6]).ToList()
                                            );

        RegionSector = Slime_Rancher.RegionSector.CreateSectorFromData(rawRegionData, data => new RegionSector(data));
        InteractableSector = Slime_Rancher.InteractableSector.CreateSectorFromData(
            rawInteractableData, data => new InteractableSector(data)
        );

        GetSpreadsheet("GameData")
           .ReadTable(out ItemAmountData).SkipColumn()
           .ReadTable(out RegionUnlockData);

        NonProgressiveUsefulItemCount = ItemAmountData.Where(data => data.ProgType is "nonprog_useful")
                                                      .ToDictionary(data => data.Item, data => int.Parse(data.Count));

        ProgressiveUsefulItemCount = ItemAmountData.Where(data => data.ProgType is "prog_useful")
                                                   .ToDictionary(data => data.Item, data => int.Parse(data.Count));

        ProgressiveProgressionItemCount = ItemAmountData.Where(data => data.ProgType is "prog_prog")
                                                        .ToDictionary(data => data.Item, data => int.Parse(data.Count));

        FillerItems = ItemAmountData.Where(data => data.ProgType is "filler").Select(data => data.Item).ToArray();

        SlimeRancherLogicHelper.ForCompiler = false;
        WriteData(
            "Logic",
            rawInteractableData.Select(line => $"{line.Name}:{line.GenRule()}:{(int)line.SkipLogic}:{line.Region}")
                               .Where(s => s != "")
        );
        WriteData(
            "RegionLogic",
            rawRegionData.Select(line
                => $"{line.To}:{line.From}:{line.GenRule()}:{(int)line.SkipLogic}:{string.Join(';', line.PlortsRequired)}:{string.Join(';', line.RegionUnlocks)}"
            )
        );
        WriteData(
            "PlortLogic",
            rawSlimeData.Select(line
                => $"{line.PlortDrop}:{string.Join(';', line.SpawnLocations)}:{line.PlortId}:{(int)line.SkipLogic}"
            )
        );
        WriteData("Gordo", GordoData.Select(data => $"{data.Id};{data.Name}"));

        var noteLocationsFromSheet = InteractableSector.Where(sector => sector.IsNote).Select(sector => sector.Id)
                                                       .ToArray();
        var noteLocations = (File.Exists($"{WriteOutputDirectory}/NoteLocations.txt")
                                ? File.ReadAllLines($"{WriteOutputDirectory}/NoteLocations.txt").ToList() : [])
                           .Where(loc => noteLocationsFromSheet.Any(sector => sector == loc)).ToList();
        noteLocations.AddRange(noteLocationsFromSheet.Where(inter => !noteLocations.Contains(inter)));

        WriteData("NoteLocations", noteLocations);

        WriteData(
            "Zones", RegionUnlockData.Where(data => data.Include).Select(data => $"{data.ZoneId}:{data.RegionName}")
        );
        WriteData(
            "Locations",
            rawInteractableData.Select(line => line.GetText).Distinct()
        );
        WriteData("Upgrades", Upgrades.Select(line => $"{line.Name},{line.Id}"));
        WriteData("7Zee", CorporateLocations.Select(line => $"{line.Location},{line.Level}"));
        WriteData("Gates", GateData.Select(data => $"{data.Id};{data.RegionUnlock}"));

        SlimeRancherLogicHelper.ForCompiler = true;
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
                "Enable Stylish Dlc Treasure Pods",
                "note: THIS WILL NOT GIVE YOU DLC\nYOU MUST __***OWN***__ THE DLC",
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
                "Include 7z",
                "Include unlockables behind 7z as checks\nestimated to appear in sphere 2 and above",
                new Toggle()
            )
           .AddOption(
                "Plortsanity", "Selling a plort for the first time will send a check",
                new Choice(1, "off", "all_except_gold", "all")
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
           .AddOption("Include Ogden", "Include Ogden's Retreat", new Toggle())
           .AddOption("Include Mochi", "Include Mochi's Manor", new Toggle())
           .AddOption("Include Viktor", "Include Viktor's Workshop", new Toggle())
           .AddOption("Postgame", "Include Post-Credit Locations, i.e. Item Vaults", new Toggle())
           .AddOption(
                "Easy Skips", "Enable Skips that many new players end up finding on their first playthrough",
                new Toggle()
            )
           .AddOption("Precise Movement", "Enable Skips that require tighter movement than average", new Toggle())
           .AddOption(
                "Dangerous Skips", "Enable Skips that have a high chance of taking damage or getting killed",
                new Toggle()
            )
           .AddOption(
                "Obscure Locations", "Enable Skips that abuse the terrain, usually in unintuitive ways", new Toggle()
            )
           .AddOption("Largo Jumps", "Enable Skips where you jump off a largo midair", new Toggle())
           .AddOption(
                "Jetpack Boosts",
                "Enable Skips where you use the ability to get rid of jetpack's startup times through careful jumping, allowing for more energy conservation",
                new Toggle()
            )
           .AddOption("Market Logic", "Enable Logic tied to early trading when opening slime gates", new Toggle())
           .AddCheckOptions(method =>
                method.AddCode(
                    """
                    if options.goal_type == 1 and not options.include_7z:
                        raise_yaml_error(world.player, "7Zee goal type requires you to include 7Zee locations")
                    """
                ).AddCode(CreateMinimalCatch(GameName))
            );
    }

    public override void Locations(WorldFactory _, LocationFactory location_fact)
    {
        location_fact
           .AddLocations("upgrades", Upgrades.Select(line => (string[])[line.Name, line.Area]))
           .AddLocations(
                "interactables",
                InteractableSector.Where(sector => !sector.IsSecretStyle && sector.HasNoOption)
                                  .Select(line => (string[])[line.VagueName, line.Region])
            )
           .AddLocations(
                "dlc_interactables",
                InteractableSector.Where(sector => sector.IsSecretStyle && sector.HasNoOption)
                                  .Select(line => (string[])[line.VagueName, line.Region])
            )
           .AddLocations(
                "corporate_locations",
                CorporateLocations.Select(line => (string[])[line.Location, line.Area])
            )
           .AddLocations("gates", GateData.Select(line => (string[])[line.Name, line.ToArea]))
           .AddLocations("plorts", PlortTypes.Select(plort => $"Sell a {plort}"));
    }

    public override void Items(WorldFactory _, ItemFactory item_fact)
    {
        item_fact
           .AddItemListVariable(
                "region_unlocks", Progression, true, true,
                RegionUnlockData.Where(data => data.Include).Select(zone => $"Region Unlock: {zone.RegionName}")
                                .ToArray()
            )
           .AddItemCountVariable("non_progressive_useful_items", NonProgressiveUsefulItemCount, Useful)
           .AddItemCountVariable("progressive_useful_item_count", ProgressiveUsefulItemCount, Useful)
           .AddItemCountVariable("progressive_progression_item_count", ProgressiveProgressionItemCount, Progression)
           .AddItemListVariable("filler_items", Filler, true, true, FillerItems)
           .AddItem("Trap Slime", Trap)
           .AddCreateItems(func =>
                func.AddCode(
                         """
                         for zone in region_unlocks:
                             if "Reef" in zone and options.start_with_dry_reef: continue
                             if "Wilds" in zone and not options.include_ogden: continue
                             if "Retreat" in zone and not options.include_ogden: continue
                             if "Nimble" in zone and not options.include_mochi: continue
                             if "Manor" in zone and not options.include_mochi: continue
                             if "Slimeulations" in zone and not options.include_viktor: continue
                             if "Workshop" in zone and not options.include_viktor: continue
                             world.location_count -= 1
                             pool.append(world.create_item(zone))
                         """
                     ).AddNewLine()
                    .AddCode(CreateItemsFromMapCountGenCode("non_progressive_useful_items")).AddNewLine()
                    .AddCode(CreateItemsFromMapCountGenCode("progressive_useful_item_count")).AddNewLine()
                    .AddCode(CreateItemsFromMapCountGenCode("progressive_progression_item_count")).AddNewLine()
                    .AddCode(
                         CreateItemsFromCountGenCode(
                             "int(world.location_count * (options.trap_percent / 100))", "Trap Slime"
                         )
                     ).AddNewLine()
                    .AddCode(CreateItemsFillRemainingWith("filler_items"))
            )
           .AddIndependentVariable(
                new StringArray(
                    "credits_unlocks", RegionUnlockData
                                      .Where(data => data is { Include: true, ForCreditsGoal: true })
                                      .Select(zone => $"Region Unlock: {zone.RegionName}")
                                      .ToArray()
                )
            );
    }

    public override void Rules(WorldFactory _, RuleFactory rule_fact)
    {
        rule_fact
           .AddCompoundLogicFunction("cracker", "has_cracker", "hasN['Progressive Treasure Cracker', level]", "level")
           .AddCompoundLogicFunction("energy", "has_energy", "hasN['Progressive Max Energy', amount]", "amount")
           .AddCompoundLogicFunction("jetpack", "has_jetpack", "has['Progressive Jetpack']")
           .AddCompoundLogicFunction("region", "has_region", "has[f\"Region Unlock: {region}\"]", "region")
           .AddCompoundLogicFunction("gate", "has_gate", "has[f\"Opened Gate: {gate}\"]", "gate")
           .AddLogicRules(
                Upgrades.Where(up => up.UnlockNeed is not "").ToDictionary(
                    up => up.Name, up => $"region[\"{up.UnlockNeed}\"]"
                )
            )
           .AddLogicRules(InteractableSector.ToDictionary(inter => inter.VagueName, inter => inter.GenRule()))
           .AddLogicRules(PlortTypes.ToDictionary(plort => $"Sell a {plort}", plort => $"has[\"{plort}\"]"));
    }

    public override void Regions(WorldFactory _, RegionFactory region_fact)
    {
        var noConditionZones = RegionSector
                              .Where(sector => sector.HasNoOption)
                              .SelectMany(zone => ((string[])[zone.From, zone.To]).Distinct())
                              .Where(zone => zone is not "Menu"
                                             && LocationMap[zone] is not (Retreat or Manor or Workshop)
                               ).Distinct().ToArray();

        var conditionZones = RegionSector
                            .Where(sector => !sector.HasNoOption
                                             || LocationMap[sector.From] is Retreat or Manor or Workshop
                             )
                            .SelectMany(zone => ((string, string)[])
                                 [
                                     (zone.From, zone.GenOption()), (zone.To, zone.GenOption()),
                                 ]
                             ).Where(t => !noConditionZones.Contains(t.Item1))
                            .GroupBy(t => t.Item1)
                            .Select(g => (g.Key, string.Join(" or ", g.Select(t => t.Item2))));

        region_fact.AddRegions("", noConditionZones)
                   .ForEachOf(
                        conditionZones,
                        (b, t) => b.AddRegion(
                            t.Key,
                            LocationMap[t.Key] switch
                            {
                                Retreat => "options.include_ogden", Manor => "options.include_mochi",
                                Workshop => "options.include_viktor", _ => t.Item2
                            }
                        )
                    )
                   .ForEachOf(
                        RegionSector.Where(data => data.HasNoOption),
                        (b, sector) => b.AddConnectionCompiledRule(sector.From, sector.To, sector.GenRule())
                    ).ForEachOf(
                        RegionSector.Where(data => !data.HasNoOption), (b, sector) => b
                           .AddConnectionCompiledRule(
                                sector.From, sector.To, sector.GenRule(), condition: sector.GenOption()
                            )
                    )
                   .ForEachOf(
                        Upgrades, (b, upgrade) =>
                        {
                            var condition = "";

                            if (upgrade.Is7ZeeUpgrade) condition = "world.options.include_7z";
                            else if (upgrade.Name.Contains("Treasure Cracker"))
                            {
                                var num = int.Parse($"{upgrade.Name[^2]}");
                                condition = $"{num} <= world.options.treasure_cracker_checks";
                            }

                            b.AddLocation(new LocationData(upgrade.Area, upgrade.Name), condition);
                        }
                    )
                   .AddLocationsFromList("interactables")
                   .AddEventLocations(
                        "world.options.goal_type == 0",
                        InteractableSector
                           .Where(inter => inter.IsNote)
                           .Select(inter => new EventLocationData(
                                    inter.Region, $"Read: {inter.VagueName}", "Note Read",
                                    inter.VagueName
                                )
                            )
                           .ToArray()
                    )
                   .AddLocationsFromList(
                        "dlc_interactables",
                        condition: "world.options.enable_stylish_dlc_treasure_pods"
                    )
                   .AddLocationsFromList("corporate_locations", condition: "world.options.include_7z")
                   .AddEventLocationsFromList(
                        "corporate_locations", "f\"Bought: {location[0]}\"", "\"7Zee Bought\"",
                        condition: "world.options.include_7z and world.options.goal_type == 1"
                    )
                   .AddEventLocations(
                        locations: NormalPlortPlacement.SelectMany(kv => kv.Value.Select(slime => new EventLocationData(
                                    kv.Key, $"{slime} ({kv.Key})", $"{slime} Plort", "''"
                                )
                            )
                        ).ToArray()
                    )
                   .AddEventLocations(
                        locations: MarketPlortPlacement.SelectMany(kv => kv.Value.Select(slime => new EventLocationData(
                                    kv.Key, $"ML_{slime} ({kv.Key})", $"{slime} Plort", "''"
                                )
                            )
                        ).ToArray()
                    )
                   .AddEventLocationsFromList("gates", item: "f\"Opened Gate: {location[0]}\"")
                   .AddLocations(
                        "options.plortsanity > 0",
                        PlortTypes.Where(plort => plort is not ("Gold Plort" or "Saber Plort"))
                                  .Select(plort => new LocationData("Menu", $"Sell a {plort}")).ToArray()
                    )
                   .AddLocation(
                        new LocationData("Menu", "Sell a Saber Plort"),
                        "options.include_ogden and options.plortsanity > 0"
                    )
                   .AddLocation(new LocationData("Menu", "Sell a Gold Plort"), "options.plortsanity > 1");
    }

    public override void Init(WorldFactory _, WorldInitFactory init_fact)
    {
        init_fact
           .UseItemGroups(new Dictionary<string, string> { ["unlocks"] = "region_unlocks" })
           .UseLocationGroups(
                InteractableSector.Select(data => (LocationMap[data.Region], data.VagueName))
                                  .GroupBy(t => t.Item1)
                                  .ToDictionary(
                                       g => g.Key,
                                       g => (ICollection)new StringCollection(g.Select(t => t.VagueName).ToArray())
                                   )
            )
           .UseInitFunction()
           .UseGenerateEarly()
           .AddUseUniversalTrackerPassthrough(yamlNeeded: false)
           .UseGenerateEarly(method =>
                method.AddCode(CreatePushPrecollected("Region Unlock: Dry Reef", "self.options.start_with_dry_reef"))
                      .AddCode(CreatePushPrecollected("Drone", "self.options.start_with_drone"))
                      .AddCode(
                           CreatePushPrecollected("Region Unlock: Ogden's Retreat", "not self.options.include_ogden")
                       )
                      .AddCode(CreatePushPrecollected("Region Unlock: The Wilds", "not self.options.include_ogden"))
                      .AddCode(CreatePushPrecollected("Region Unlock: Mochi's Manor", "not self.options.include_mochi"))
                      .AddCode(CreatePushPrecollected("Region Unlock: Nimble Valley", "not self.options.include_mochi"))
                      .AddCode(
                           CreatePushPrecollected("Region Unlock: Viktor's Workshop", "not self.options.include_viktor")
                       )
                      .AddCode(
                           CreatePushPrecollected("Region Unlock: The Slimeulations", "not self.options.include_viktor")
                       )
            )
           .UseCreateRegions()
           .AddCreateItems()
           .UseSetRules(method =>
                method.AddCode(
                    new MatchFactory("self.options.goal_type")
                       .AddCase("0", CreateGoalCondition(StateHas("Note Read", "28", returnValue: false)))
                       .AddCase(
                            "1",
                            CreateGoalCondition(StateHas("7Zee Bought", "len(corporate_locations)", returnValue: false))
                        )
                       .AddCase("2", CreateGoalCondition(StateHasAll("credits_unlocks", false)))
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
              .ForEachOf(RegionSector, (b, sector) => b.AddConnection(sector.From, sector.To, sector.GenRule()))
              .AddLocationsFromDoubleArray(locationDoubleArrays, getRule)
              .ForEachOf(
                   InteractableSector
                      .Where(inter => inter.IsNote),
                   (b, inter) => b.AddEventLocation(
                       inter.Region, getRule, $"Read: {inter.VagueName}", inter.VagueName,
                       "Note Read"
                   )
               )
              .ForEachOf(
                   CorporateLocations,
                   (b, line) => b.AddEventLocation(
                       line.Area, getRule, $"Bought: {line.Location}", line.Location,
                       "7Zee Bought"
                   )
               )
              .ForEachOf(
                   NormalPlortPlacement,
                   (b, kv) => b.ForEachOf(
                       kv.Value, (_, slime) => b.AddEventLocation(kv.Key, getRule, slime, "", $"{slime} Plort")
                   )
               )
              .ForEachOf(
                   MarketPlortPlacement,
                   (b, kv) => b.ForEachOf(
                       kv.Value, (_, slime) => b.AddEventLocation(kv.Key, getRule, $"ML_{slime}", "", $"{slime} Plort")
                   )
               )
              .ForEachOf(PlortTypes, (b, plort) => b.AddLocation("Menu", getRule, $"Sell a {plort}"))
              .GenString();
    }
}