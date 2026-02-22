using CreepyUtil.Archipelago.WorldFactory;
using static CreepyUtil.Archipelago.WorldFactory.ItemFactory.ItemClassification;
using static CreepyUtil.Archipelago.WorldFactory.PremadePython;

namespace ApWorldFactories.Games;

public class SlimeRancher() : BuildData(
    DDrive, "Slime Rancher", "SW_CreeperKing.Slimipelago", "slime_rancher",
    "15PdrnGmkYdocX9RU-D5U_9OgihRNN9axX71mm-jOPUQ", "0.2.3"
)
{
    public override Dictionary<string, string> SheetGids { get; }
        = new() { ["GameData"] = "72469245", ["NewLogic"] = "2113673017" };

    public override void RunShenanigans(WorldFactory factory)
    {
        GetSpreadsheet("GameData")
           .ToFactory()
           .ReadTable(new RegionDataCreator(), 2, out var regionData).SkipColumn()
           .ReadTable(new DataCreator<ItemAmountData>(), 3, out var itemAmountData);

        var zones = regionData.Select(data => data.Region).ToArray();
        var backwardsConnections = regionData.ToDictionary(data => data.Region, data => data.BackConnections);

        var nonProgressiveUsefulItemCount = itemAmountData.Where(data => data.ProgType is "nonprog_useful")
                                                          .ToDictionary(
                                                               data => data.Item, data => int.Parse(data.Count)
                                                           );
        var progressiveUsefulItemCount = itemAmountData.Where(data => data.ProgType is "prog_useful")
                                                       .ToDictionary(data => data.Item, data => int.Parse(data.Count));
        var progressiveProgressionItemCount = itemAmountData.Where(data => data.ProgType is "prog_prog")
                                                            .ToDictionary(
                                                                 data => data.Item, data => int.Parse(data.Count)
                                                             );
        var fillerItems = itemAmountData.Where(data => data.ProgType is "filler").Select(data => data.Item).ToArray();

        GetSpreadsheet("main")
           .ToFactory()
           .ReadTable(new InteractableCreator(zones), 7, out var rawInteractables).SkipColumn()
           .ReadTable(new GateCreator(), 5, out var gates).SkipColumn()
           .ReadTable(new GordoCreator(), 8, out var gordos).SkipColumn()
           .ReadTable(new UpgradeCreator(), 3, out var upgrades).SkipColumn()
           .ReadTable(new CorporateCreator(), 3, out var corporateLocations);
        
        var interactables = rawInteractables.Where(line => !line.IsSecretStyle).ToArray();
        var dlcInteractables = rawInteractables.Where(line => line.IsSecretStyle).ToArray();

        factory
           .GetOptionsFactory(GitLink)
           .AddOption(
                "Goal Type", """
                             What criteria to goal
                             notes = read all notes
                             7Zee = buy all 7Zee ranks
                             credits = get credits
                             """, new Choice("notes", "7Zee", "credits")
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
            )
           .GenerateOptionFile();

        factory
           .GetLocationFactory(GitLink)
            // .AddLocations("zones", Zones, addToFinalList: false)
            // .AddLocations("backwards_connections", BackwardsConnections, addToFinalList: false)
           .AddLocations("upgrades", upgrades.Select(line => line.Name))
           .AddLocations(
                "upgrades_7z", upgrades.Where(up => up.Rule is "7z").Select(up => up.Name), addToFinalList: false
            )
           .AddLocations("interactables", interactables.Select(line => (string[])[line.Name, line.Area]))
           .AddLocations("dlc_interactables", dlcInteractables.Select(line => (string[])[line.Name, line.Area]))
           .AddLocations("corporate_locations", corporateLocations.Select(line => (string[])[line.Location, line.Area]))
           .GenerateLocationFile();
        
        factory
           .GetRuleFactory(GitLink)
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
           .AddLogicRules(rawInteractables.ToDictionary(inter => inter.Name, inter => inter.GenRule(true)))
           .AddLogicRules(upgrades.ToDictionary(up => up.Name, up => up.Rule))
           .GenerateRulesFile();
        
        factory
           .GetItemFactory(GitLink)
           .AddItemListVariable(
                "region_unlocks", Progression, true, zones.Skip(2).Select(zone => $"Region Unlock: {zone}").ToArray()
            )
           .AddItemCountVariable("non_progressive_useful_items", nonProgressiveUsefulItemCount, Useful)
           .AddItemCountVariable("progressive_useful_item_count", progressiveUsefulItemCount, Useful)
           .AddItemCountVariable("progressive_progression_item_count", progressiveProgressionItemCount, Progression)
           .AddItemListVariable("filler_items", Filler, true, fillerItems)
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
            )
           .GenerateItemsFile();

        var regionFactory = factory
                           .GetRegionFactory(GitLink)
                           .AddRegions(backwardsConnections.Keys.ToArray());

        backwardsConnections
           .Aggregate(
                regionFactory, (factory1, zoneKv) =>
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

        foreach (var upgrade in upgrades)
        {
            var condition = "";

            if (upgrade.Rule is "7z") { condition = "world.options.include_7z"; }
            else if (upgrade.Name.Contains("Treasure Cracker"))
            {
                condition
                    = $"\"{upgrade.Name}\" > f\"Buy Personal Upgrade (Treasure Cracker lv.{{world.options.treasure_cracker_checks}})\"";
            }

            regionFactory.AddLocation(new LocationData("Upgrades", upgrade.Name), condition);
        }

        regionFactory
           .AddLocationsFromList("interactables")
           .AddEventLocations(
                "world.options.goal_type == 0",
                interactables
                   .Where(inter => inter.Name.Contains("Hobson's Note"))
                   .Select(inter => new EventLocationData(inter.Area, $"Read: {inter.Name}", "Note Read", inter.Name))
                   .ToArray()
            )
           .AddLocationsFromList("dlc_interactables", condition: "world.options.enable_stylish_dlc_treasure_pods")
           .AddLocationsFromList("corporate_locations", condition: "world.options.include_7z")
           .AddEventLocationsFromList(
                "corporate_locations", "f\"Bought: {location[0]}\"", "\"7Zee Bought\"",
                condition: "world.options.include_7z and world.options.goal_type == 1"
            )
           .GenerateRegionFile();

        factory.GetHostSettingsFactory(GitLink).GenerateHostSettingsFile();
        
        factory
           .GetInitFactory(GitLink)
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
           .UseGenerateOutput(method => method.AddCode(PumlGenCode()))
           .GenerateInitFile();

        WriteData(
            "Logic",
            interactables.Concat(dlcInteractables).Select(line => $"{line.Name}:{line.GenRule(false)}:{line.Area}")
                         .Where(s => s != "")
        );


        var noteLocations = File.Exists($"{ModDataPath}/NoteLocations.txt")
            ? File.ReadAllLines($"{ModDataPath}/NoteLocations.txt").ToList() : [];
        noteLocations.AddRange(
            interactables.Where(inter => inter.IsNote && !noteLocations.Contains(inter.Id)).Select(inter => inter.Id)
        );
        WriteData("NoteLocations", noteLocations);

        WriteData(
            "Locations",
            rawInteractables.Select(line => line.GetText).Concat(gates.Select(line => line.GetText))
                            .Concat(gordos.Select(line => line.GetText))
        );
        WriteData("Upgrades", upgrades.Select(line => $"{line.Name},{line.Id}"));
        WriteData("7Zee", corporateLocations.Select(line => $"{line.Location},{line.Level}"));
    }
}

file readonly struct InteractableRowData(string[] line)
{
    public readonly string Id = line[0];
    public readonly string Name = line[1].Trim();
    public readonly string Area = line[2];
    public readonly string CrackerLevel = line[3].Trim();
    public readonly bool NeedsJetpack = line[4] == "Yes";
    public readonly int MinJetpackEnergy = int.TryParse(line[5].Split(' ')[0], out var energy) ? energy : 100;
    public readonly string Summary = line[6];
    public bool IsSecretStyle => CrackerLevel == "Secret Style";
    public bool IsNote => CrackerLevel == "Note";
    public string GetText => $"{Id},{Name},{Summary}";

    public string GenRule(bool forCompiler)
    {
        List<string> rules = [];

        if (CrackerLevel.Contains("Treasure Cracker"))
        {
            var level = Math.Max(1, CrackerLevel.Count(c => c == 'I'));
            rules.Add(forCompiler ? $"cracker[{level}]" : string.Join("", Enumerable.Repeat('c', level)));
        }

        if (NeedsJetpack) { rules.Add(forCompiler ? "jetpack" : "j"); }

        if (MinJetpackEnergy > 100)
        {
            var energyLevel = (int)Math.Ceiling(MinJetpackEnergy / 50f - 2f);
            rules.Add(forCompiler ? $"energy[{energyLevel}]" : string.Join("", Enumerable.Repeat('e', energyLevel)));
        }

        return forCompiler ? string.Join(" and ", rules) : string.Join("", rules);
    }

    public static implicit operator LocationData(InteractableRowData data) => new(data.Area, data.Name);
}

file readonly struct GateRowData(string[] line)
{
    public readonly string Id = line[0];
    public readonly string Name = line[1];
    public readonly string FromArea = line[2];
    public readonly string ToArea = line[3];
    public readonly string SkippableWithJetpack = line[4];
    public string GetText => $"{Id},{Name}";
}

file readonly struct GordoRowData(string[] line)
{
    public readonly string Id = line[0];
    public readonly string Name = line[1];
    public readonly string Area = line[2];
    public readonly string Contents = line[3];
    public readonly string TeleporterLocation = line[4];
    public readonly string JetpackRequirement = line[5];
    public readonly string NormalFoodRequirement = line[6];
    public readonly string FavoriteFood = line[7];
    public string GetText => $"{Id},{Name},Favorite: {FavoriteFood}";
}

file readonly struct UpgradeRowData(string[] line)
{
    public readonly string Name = line[0];
    public readonly string Id = line[1];
    public readonly string Rule = line[2];
}

file readonly struct CorporateRowData(string[] line)
{
    public readonly string Location = line[0].Trim();
    public readonly int Level = line[0] != "" ? int.Parse(line[0].Split(':')[0].Split('.')[1]) : -1;
    public readonly string Price = line[1];
    public readonly string Area = line[2];

    public static implicit operator LocationData(CorporateRowData data) => new(data.Area, data.Location);
}

file readonly struct RegionData(string[] param)
{
    public readonly string Region = param[0];
    public readonly string[] BackConnections = param[1].SplitAndTrim(',');
}

file readonly struct ItemAmountData(string[] param)
{
    public readonly string Item = param[0];
    public readonly string Count = param[1];
    public readonly string ProgType = param[2];
}

file class InteractableCreator(string[] zones) : DataCreator<InteractableRowData>
{
    public override bool IsValidData(InteractableRowData t) => zones.Contains(t.Area);
}

file class GateCreator : DataCreator<GateRowData>
{
    public override bool IsValidData(GateRowData t) => t.Id != "";
}

file class GordoCreator : DataCreator<GordoRowData>
{
    public override bool IsValidData(GordoRowData t) => t.Id != "";
}

file class UpgradeCreator : DataCreator<UpgradeRowData>
{
    public override bool IsValidData(UpgradeRowData t) => t.Name != "";
}

file class CorporateCreator : DataCreator<CorporateRowData>
{
    public override bool IsValidData(CorporateRowData t) => t.Location != "";
}

file class RegionDataCreator : DataCreator<RegionData>
{
    public override bool IsValidData(RegionData t) => t.BackConnections.Length != 0 && t.Region is not "Menu";
}
