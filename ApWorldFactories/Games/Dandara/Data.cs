using CreepyUtil.Archipelago.WorldFactory;
using CreepyUtil.ClrCnsl;

namespace ApWorldFactories.Games.Dandara;

public readonly struct RegionRowData(DataArray param)
{
    [Mark] public readonly int Number = param;
    [Mark] public readonly string Region = param;
}

public readonly struct ConnectionRowData(DataArray param) : ILogicSectorDataType<string, ConnectionRowData>
{
    [Mark] public readonly string From = param;
    [Mark] public readonly string To = param;
    [Mark] public readonly string[] Items = param;

    public bool IsNoOption() => true;
    public string GenRule() => DandaraGenRule.GenRule(Items, 0);
    public bool IsMatch(ConnectionRowData matchAgainst) => From == matchAgainst.From && To == matchAgainst.To;
    public string Print() => $"Connection| {From},{To},[{string.Join(',', Items)}]";
    public string GenOption() => "";
    public string GetIdentifier() => string.Join(", ", Items);
}

public readonly struct SoulRowData(DataArray param)
{
    [Mark] public readonly string RoomId = param;
    [Mark] public readonly string Name = param;
    [Mark] public readonly string Death = param;
    [Mark] public readonly string Type = param;
    [Mark] public readonly int Amount = param;
    [Mark] public readonly string[] Items = param;
    [Mark] public readonly string Position = param;
}

public readonly struct BossRowData(DataArray param)
{
    [Mark] public readonly string RoomId = param;
    [Mark] public readonly string StoryEvent = param;
    [Mark] public readonly string Name = param;
    [Mark] public readonly string[] Items = param;
}

public readonly struct CampsRowData(DataArray param)
{
    [Mark] public readonly int Check = param;
    [Mark] public readonly string RoomId = param;
    [Mark] public readonly string[] Items = param;
    [Mark] public readonly string CampOrFlag = param;
}

public readonly struct ItemsRowData(DataArray param)
{
    [Mark] public readonly string ItemId = param;
    [Mark] public readonly string ItemName = param;

    [Mark] public readonly ItemFactory.ItemClassification ItemType = ((string)param)switch
    {
        "Progression" => ItemFactory.ItemClassification.Progression,
        "Useful" => ItemFactory.ItemClassification.Useful, _ => ItemFactory.ItemClassification.Filler,
    };

    [Mark] public readonly int ItemCount = param;
    [Mark] public readonly string Desc = param;
}

public readonly struct WeaponDuoRowData(DataArray param)
{
    [Mark] public readonly string ItemId = param;
}

public readonly struct WeaponReachRowData(DataArray param)
{
    [Mark] public readonly string ItemId = param;
}

public readonly struct WeaponBlastRowData(DataArray param)
{
    [Mark] public readonly string ItemId = param;
}

public readonly struct ShopRowData(DataArray param)
{
    [Mark] public readonly string Upgrade = param;
    [Mark] public readonly string UpgradeId = param;
    [Mark] public readonly int CheckCount = param;
}

public readonly struct EventItemsRowData(DataArray param) : ILogicSectorDataType<string, EventItemsRowData>
{
    [Mark] public readonly string EventName = param;
    [Mark] public readonly string RoomId = param;
    [Mark] public readonly string EventItem = param;
    [Mark] public readonly string[] Items = param;

    public bool IsNoOption() => true;
    public string GenRule() => DandaraGenRule.GenRule(Items, 0);
    public string GetIdentifier() => string.Join(", ", Items);

    public bool IsMatch(EventItemsRowData matchAgainst) => EventName == matchAgainst.EventName
                                                           && RoomId == matchAgainst.RoomId
                                                           && EventItem == matchAgainst.EventItem;

    public string GenOption() => "";
    public string Print() => $"EventItem| {EventName},{RoomId},{EventItem},[{string.Join(", ", Items)}]";
}

public readonly struct RoomsRowData(DataArray param)
{
    [Mark] public readonly string RoomId = param;
    [Mark] public readonly string RoomName = param;
    [Mark] public readonly string RoomRegion = param;
    [Mark] public readonly string GameRegion = param;
}

public readonly struct ChestsRowData(DataArray param) : ILogicSectorDataType<string, ChestsRowData>
{
    [Mark] public readonly string CheckType = param;
    [Mark] public readonly string RoomId = param;
    [Mark] public readonly string ChestName = param;
    [Mark] public readonly string LocationNameOverride = param;
    [Mark] public readonly string ChestContents = param;
    [Mark] public readonly string[] Items = param;
    [Mark] public readonly string Position = param;
    [Mark] public readonly int HealthUpgradeRequirements = param;

    public bool IsNoOption() => true;
    public string GenRule() => DandaraGenRule.GenRule(Items, HealthUpgradeRequirements);

    public bool IsMatch(ChestsRowData matchAgainst) => CheckType == matchAgainst.CheckType
                                                       && RoomId == matchAgainst.RoomId
                                                       && ChestContents == matchAgainst.ChestContents
                                                       && Position == matchAgainst.Position;

    public string Print()
        => $"OverworldChest| {CheckType},{RoomId},{ChestName},{ChestContents},[{string.Join(',', Items)}],{Position}";

    public string GenOption() => "";
    public string GetIdentifier() => string.Join(", ", Items);

    public string GetName(Dictionary<string, string> roomIdToName) => LocationNameOverride.Trim() is not ""
        ? LocationNameOverride.Trim()
        : $"{roomIdToName[RoomId]} ({CheckType})";
}

public static class DandaraGenRule
{
    public static string GenRule(string[] items, int healthUpgradesNeeded)
    {
        if (items.Contains("None") || items.Length == 0) return "";

        List<string> rules = [];

        if (healthUpgradesNeeded > 0) rules.Add($"hasN[\"Heart of the Great Salt\", {healthUpgradesNeeded}]");
        if (items.Contains("Blast")) rules.Add("blast");
        if (items.Contains("Reach")) rules.Add("reach");
        if (items.Contains("Weapon_Duo")) rules.Add("duo");

        rules.AddRange(
            items.Where(item => item is not ("Blast" or "Reach" or "Weapon_Duo"))
                 .Select(item => $"has[\"{Dandara.ItemIdToItemName.GetValueOrDefault(item, item)}\"]")
        );

        return string.Join(" and ", rules);
    }
}