using ApWorldFactories.Graphviz;
using CreepyUtil.Archipelago.WorldFactory;
using static ApWorldFactories.PathConstants;
using static CreepyUtil.Archipelago.WorldFactory.PremadePython;

namespace ApWorldFactories.Games.Dandara;

public class Dandara : BuildData
{
    public static Dictionary<string, string> ItemIdToItemName;

    public override string SteamDirectory => DDrive;
    public override string ModFolderName => "ArchDandara";
    public override string GameName => "Dandara";
    public override string ApWorldName => "dandara";
    public override string GoogleSheetId => "1wGcbI8GqDitbmnimmlap89KbXywaAG-l5Ki7qZeTO8U";
    public override string WorldVersion => "0.1.0";

    public override Dictionary<string, string> SheetGids => new()
    {
        ["souls"] = "222134917", ["boss"] = "466616508", ["camps"] = "1401333282", ["items"] = "2111740170",
        ["rooms"] = "246003949", ["chests"] = "297586002"
    };

    public RegionRowData[] RegionRowData = [];
    public ConnectionsSector[] ConnectionsData = [];
    public SoulRowData[] SoulRowData = [];
    public BossRowData[] BossRowData = [];
    public CampsRowData[] CampsRowData = [];
    public ItemsRowData[] ItemsRowData = [];
    public WeaponDuoRowData[] WeaponDuoRowData = [];
    public WeaponReachRowData[] WeaponReachRowData = [];
    public WeaponBlastRowData[] WeaponBlastRowData = [];
    public ShopRowData[] ShopRowData = [];
    public EventItemSector[] EventItemsData = [];
    public RoomsRowData[] RoomsRowData = [];
    public ChestsSector[] OverworldChestsData = [];
    public ChestsSector[] FearworldChestsData = [];

    public Dictionary<string, string> RoomIdToRegion = [];
    public Dictionary<string, string> RoomIdToRoomName = [];

    public override void RunShenanigans()
    {
        GetSpreadsheet().ReadTable(out RegionRowData).SkipColumn()
                        .ReadTable<ConnectionRowData>(out var rawConnectionsRowData);
        GetSpreadsheet("souls").SkipColumn().ReadTable(out SoulRowData);
        GetSpreadsheet("boss").ReadTable(out BossRowData);
        GetSpreadsheet("camps").ReadTable(out CampsRowData);
        GetSpreadsheet("items").ReadTable(out ItemsRowData).SkipColumn(2)
                               .ReadTable(out WeaponDuoRowData).SkipColumn()
                               .ReadTable(out WeaponReachRowData).SkipColumn()
                               .ReadTable(out WeaponBlastRowData).SkipColumn()
                               .ReadTable(out ShopRowData).SkipColumn()
                               .ReadTable(out EventItemsRowData[] rawEventItemsRowData);
        GetSpreadsheet("rooms").SkipColumn().ReadTable(out RoomsRowData);
        GetSpreadsheet("chests").ReadTable<ChestsRowData>(out var rawOverworldChestsRowData).SkipColumn(2)
                                .ReadTable<ChestsRowData>(out var rawFearworldChestsRowData);

        RegionRowData = RegionRowData.Where(data => data.Region is not "Menu").ToArray();
        RoomIdToRegion = RoomsRowData.ToDictionary(data => data.RoomId, data => data.RoomRegion);
        RoomIdToRoomName = RoomsRowData.ToDictionary(data => data.RoomId, data => data.RoomName);
        ItemsRowData = ItemsRowData.Where(data => data.ItemName is not "" && data.ItemCount is not 0).ToArray();
        ItemIdToItemName = ItemsRowData.ToDictionary(data => data.ItemId, data => data.ItemName);

        ConnectionsData = ConnectionsSector.CreateSectorFromData(
            rawConnectionsRowData, data => new ConnectionsSector(data)
        );

        OverworldChestsData = ChestsSector.CreateSectorFromData(
            rawOverworldChestsRowData, data => new ChestsSector(data, RoomIdToRoomName)
        );

        FearworldChestsData = ChestsSector.CreateSectorFromData(
            rawFearworldChestsRowData, data => new ChestsSector(data, RoomIdToRoomName)
        );

        EventItemsData = EventItemSector.CreateSectorFromData(rawEventItemsRowData, data => new EventItemSector(data));
        
        WriteData("locationIDs", OverworldChestsData.Concat(FearworldChestsData).Select(data => $"{data.RoomId}{data.ChestName}{data.ChestContents}"));
    }

    public override void Options(WorldFactory _, OptionsFactory options_fact)
    {
        options_fact.AddOption("Goal Type", "Which Boss to defeat as goal", new Choice(0, "final_boss", "true_ending"))
                    .AddCheckOptions();
    }

    public override void Locations(WorldFactory _, LocationFactory location_fact)
    {
        location_fact
           .AddLocations(
                "overworld_chest",
                OverworldChestsData.Select(data => (string[])[data.Name, RoomIdToRegion[data.RoomId]])
            ).AddLocations(
                "fearworld_chest",
                FearworldChestsData.Select(data => (string[])[data.Name, RoomIdToRegion[data.RoomId]])
            );
    }

    public override void Items(WorldFactory _, ItemFactory item_fact)
    {
        var itemGroups = ItemsRowData.GroupBy(data => data.ItemType).ToArray();
        item_fact.ForEachOf(
                      itemGroups,
                      (b, data) =>
                      {
                          b.AddItemCountVariable(
                              $"{data.Key}_items".ToLower(), data.ToDictionary(d => d.ItemName, d => d.ItemCount),
                              data.Key
                          );
                      }
                  )
                 .AddItem("Salt", ItemFactory.ItemClassification.Filler)
                 .AddCreateItems(method =>
                      method
                         .ForEachOf(
                              itemGroups,
                              (b, data) =>
                                  b.AddCode(CreateItemsFromMapCountGenCode($"{data.Key}_items".ToLower()))
                          )
                         .AddCode(CreateItemsFillRemainingWithItem("Salt"))
                  );
    }

    public override void Rules(WorldFactory _, RuleFactory rule_fact)
    {
        rule_fact
           .AddLogicFunction(
                "duo", "has_weapon_duo",
                $"return state.has_from_list_unique([{string.Join(", ", WeaponDuoRowData.Select(id => $"\"{ItemIdToItemName[id.ItemId]}\""))}], player, 2)"
            )
           .AddCompoundLogicFunction(
                "blast", "has_blast",
                string.Join(" or ", WeaponBlastRowData.Select(data => $"has[\"{ItemIdToItemName[data.ItemId]}\"]"))
            )
           .AddCompoundLogicFunction(
                "reach", "has_reach",
                string.Join(" or ", WeaponReachRowData.Select(data => $"has[\"{ItemIdToItemName[data.ItemId]}\"]"))
            )
           .AddLogicRules(OverworldChestsData.ToDictionary(data => data.Name, data => data.GenRule()))
           .AddLogicRules(FearworldChestsData.ToDictionary(data => data.Name, data => data.GenRule()))
           .AddLogicRules(EventItemsData.ToDictionary(data => data.EventName, data => data.GenRule()));
    }

    public override void Regions(WorldFactory _, RegionFactory region_fact)
    {
        var regionNames = RegionRowData.Select(data => data.Region).ToArray();
        region_fact.AddRegions("", regionNames)
                   .ForEachOf(
                        ConnectionsData,
                        (b, sector) => b.AddConnectionCompiledRule(sector.From, sector.To, sector.GenRule())
                    )
                   .AddLocationsFromList("overworld_chest")
                   .AddLocationsFromList("fearworld_chest")
                   .AddEventLocations(
                        "",
                        EventItemsData.Select(data => new EventLocationData(
                                RoomIdToRegion[data.RoomId], data.EventName, data.EventItem, data.EventName
                            )
                        ).ToArray()
                    );
    }

    public override void Init(WorldFactory world_fact, WorldInitFactory init_fact)
    {
        init_fact
           .UseInitFunction()
           .AddUseUniversalTrackerPassthrough(yamlNeeded: false)
           .UseCreateRegions()
           .AddCreateItems()
           .UseSetRules(method => method.AddCode(
                    new MatchFactory("options.goal_type")
                       .AddCase("0", CreateGoalCondition("has[\"FinalBoss_Kill\"]", world_fact.GetRuleFactory()))
                       .AddCase("1", CreateGoalCondition("has[\"DLCF_FearEnded\"]", world_fact.GetRuleFactory()))
                )
            )
           .InjectCodeIntoWorld(world => world.AddVariable(new Variable("gen_puml", "False")))
           .UseGenerateOutput(method => method.AddCode(PumlGenCode()));
    }

    public override string GenerateGraphViz(WorldFactory worldFactory, Dictionary<string, string> associations,
        Func<string, string> getRule,
        string[][] locationDoubleArrays)
    {
        var regionNames = RegionRowData.Select(data => data.Region).ToArray();
        return new GraphBuilder(GameName)
              .AddRegions(regionNames)
              .ForEachOf(
                   ConnectionsData, (b, sector) =>
                       b.AddConnection(sector.From, sector.To, sector.GenRule())
               )
              .ForEachOf(
                   EventItemsData,
                   (b, data) => b.AddEventLocation(
                       RoomIdToRegion[data.RoomId], getRule, data.EventName, data.EventName, data.EventItem
                   )
               )
              .AddLocationsFromDoubleArray(locationDoubleArrays, getRule).GenString();
    }

    public override void GenerateJson(WorldFactory worldFactory) => worldFactory.GenerateArchipelagoJson(ArchipelagoVersion, WorldVersion, "Smores9000", "SW_CreeperKing");
}