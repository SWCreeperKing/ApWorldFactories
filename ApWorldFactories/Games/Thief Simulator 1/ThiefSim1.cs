using CreepyUtil.Archipelago.WorldFactory;
using WorldBuilder;
using static ApWorldFactories.PathConstants;
using static CreepyUtil.Archipelago.WorldFactory.PremadePython;

namespace ApWorldFactories.Games.Thief_Simulator_1;

public class ThiefSim1 : BuildData
{
    public override string SteamDirectory => DDrive;
    public override string ModFolderName => "SW_CreeperKing.Thiefipelago";
    public override string GameName => "Thief Simulator 1";
    public override string ApWorldName => "thief_sim_1";
    public override string WorldVersion => "0.1.0";
    public override string GoogleSheetId => "1eDRX4ly-hzH9aKezsbf-aVXdSyUzIEfjWYXbrulWxzI";

    public override Dictionary<string, string> SheetGids => new()
    {
        ["greenview"] = "529708973", ["richie"] = "312901529", ["industrial"] = "1351921694"
    };

    public RegionSector[] RegionSectors = [];
    public ItemRowData[] ItemNames = [];
    public LootTypeRowData[] LootTypes = [];
    public LocationRowData[] LocationRowData = [];

    public override void RunShenanigans()
    {
        GetSpreadsheet()
           .ReadTable(out ItemNames).SkipColumn()
           .ReadTable(out LootTypes);

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
    }

    public override void Locations(WorldFactory _, LocationFactory location_fact)
    {
        location_fact.AddLocations("locs", [.. LocationRowData.Select(loc => (string[])[loc.Name, loc.Region])]);
    }

    public override void Items(WorldFactory _, ItemFactory item_fact)
    {
        item_fact.AddItemListVariable(
                      "items", ItemFactory.ItemClassification.Progression, list: [.. ItemNames.Select(s => s.RawName)]
                  )
                 .AddCreateItems(method => method.AddCode(CreateItemsFromList("items")));
    }

    public override void Rules(WorldFactory _, RuleFactory rule_fact)
    {
        rule_fact.ForEachOf(
            ItemNames,
            name => rule_fact.AddCompoundLogicFunction(name.Name, $"has_{name.Name}", $"has[\"{name.Name}\"]")
        ).AddLogicRules(LocationRowData.ToDictionary(loc => loc.Name, loc => loc.GenRule()));
    }

    public override void Regions(WorldFactory _, RegionFactory region_fact)
    {
        region_fact
           .AddRegions(regions: [.. RegionSectors.Select(r => r.Name).DistinctBy(s => s)])
           .ForEachOf(
                RegionSectors,
                region => region_fact.AddConnectionCompiledRule(region.From, region.Name, region.GenRule())
            )
           .AddLocationsFromList("locs");
    }

    public override void Init(WorldFactory _, WorldInitFactory init_fact) { }

    public override string GenerateGraphViz(WorldFactory worldFactory, Dictionary<string, string> associations,
        Func<string, string> getRule, string[][] locationDoubleArrays)
        => new GraphBuilder(GameName)
          .ForEachOf(RegionSectors, (b, region) => b.AddConnection(region.From, region.Name, region.GenRule()))
          .AddLocationsFromDoubleArray(locationDoubleArrays, getRule)
          .GenString();

}