using CreepyUtil.Archipelago.WorldFactory;
using WorldBuilder;
using static ApWorldFactories.PathConstants;

namespace ApWorldFactories.Games.Thief_Simulator_1;

public class ThiefSim1 : BuildData
{
    public override string SteamDirectory => DDrive;
    public override string ModFolderName => "SW_CreeperKing.Thiefipelago";
    public override string GameName => "Thief Simulator 1";
    public override string GamePath => $"{SteamDirectory}/Thief Simulator";
    public override string ApWorldName => "thief_simulator_1";
    public override string WorldVersion => "0.1.0";
    public override string GoogleSheetId => "1eDRX4ly-hzH9aKezsbf-aVXdSyUzIEfjWYXbrulWxzI";

    public override Dictionary<string, string> SheetGids => new()
    {
        ["greenview"] = "529708973", ["richie"] = "312901529", ["industrial"] = "1351921694"
    };

    public RegionSector[] RegionSectors = [];

    public ItemRowData[] ItemNames = [];

    // public LootTypeRowData[] LootTypes = [];
    public LocationRowData[] LocationRowData = [];
    public LocationRowData[] EventLocationRowData = [];
    public string[] RegionUnlocks = [];

    public override void RunShenanigans()
    {
        GetSpreadsheet()
           .ReadTable(out ItemNames).SkipColumn()
            // .ReadTable(out LootTypes)
            ;

        List<RegionRowData> regionRowData = [];
        foreach (var id in (string[])["greenview"])
        {
            GetSpreadsheet(id).ReadTable(out RegionRowData[] regionsData).SkipColumn()
                              .ReadTable(out LocationRowData[] locationsData);
            regionRowData.AddRange(regionsData);
            LocationRowData =
            [
                .. LocationRowData,
                .. locationsData.Where(loc => loc.Id is not "" && loc.Region is not "" && !loc.DoNotUse)
            ];
        }

        RegionSectors = RegionSector.CreateSectorFromData([.. regionRowData], data => new RegionSector(data));
        RegionUnlocks = [.. RegionSectors.Where(r => r.IsRegionUnlockable).Select(r => r.Name)];
        EventLocationRowData = [.. LocationRowData.Where(l => l.LocationItem is not "")];
        WriteData("locationIds", LocationRowData.Select(l => $"{l.Name}|{l.Id}"));
    }

    public override void Locations(LocationFactory location_fact)
    {
        location_fact.AddLocations(
            "locs",
            [
                .. LocationRowData.Where(l => !EventLocationRowData.Contains(l))
                                  .Select(loc => (string[])[loc.Name, loc.Region])
            ]
        );
    }

    public override void Items(ItemFactory item_fact)
    {
        string[] eventItems = [.. EventLocationRowData.Select(l => l.LocationItem)];
        item_fact
           .AddItemListVariable("unlocks", ItemFactory.ItemClassification.Progression, list: RegionUnlocks)
           .AddItemListVariable(
                "items", ItemFactory.ItemClassification.Progression,
                list: [.. ItemNames.Select(s => s.RawName).Where(s => !eventItems.Contains(s))]
            ).AddItem("$15", ItemFactory.ItemClassification.Filler)
           .AddItem("Diamond", ItemFactory.ItemClassification.Progression)
           .AddCreateItems(method =>
                method
                   .CreateItemsFromList("unlocks")
                   .CreateItemsFromList("items")
                   .CreateItemsFromCountGenCode("50", "Diamond")
                   .FillRemainingWithItem("$15")
            );
    }

    public override void Rules(RuleFactory rule_fact)
    {
        rule_fact
           .ForEachOf(
                ItemNames,
                name => rule_fact.AddCompoundLogicFunction(name.Name, $"has_{name.Name}", $"has[\"{name.RawName}\"]")
            ).AddLogicRules(LocationRowData.ToDictionary(loc => loc.Name, loc => loc.GenRule()));
    }

    public override void Regions(RegionFactory region_fact)
    {
        region_fact
           .AddRegions(regions: [.. RegionSectors.Select(r => r.Name).DistinctBy(s => s)])
           .ForEachOf(
                RegionSectors,
                region => region_fact.AddConnectionCompiledRule(region.From, region.Name, region.GenRule())
            )
           .AddLocationsFromList("locs")
           .AddEventLocations(locations: [.. EventLocationRowData.Select(l 
                => new EventLocationData(l.Region, $"Event: [{l.Name}]", l.LocationItem, l.Name))]);
    }

    public override void Init(WorldInitFactory init_fact)
    {
        init_fact
           .AddUseUniversalTrackerPassthrough(yamlNeeded: false)
           .UseSetRules(method => method.CreateGoalCompiledCondition("hasN[\"Diamond\", 40]"))
           .UseGetFillerItemName("$15")
           .InjectCodeIntoWorld(world => world.AddVariable(new Variable("gen_puml", "False")))
           .UseGenerateOutput(method => method.PumlGenCode());
    }

    public override string GenerateGraphViz(WorldFactory worldFactory, Dictionary<string, string> associations,
        Func<string, string> getRule, string[][] locationDoubleArrays)
        => new GraphBuilder(GameName)
          .ForEachOf(RegionSectors, (b, region) => b.AddConnection(region.From, region.Name, region.GenRule()))
          .AddLocationsFromDoubleArray(locationDoubleArrays, getRule)
          .GenString();

}