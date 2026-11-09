using WorldBuilder;

namespace ApWorldFactories.Games.Thief_Simulator_1;

public readonly struct ItemRowData(DataArray param)
{
    [Mark] public readonly string RawName = param.Get(false).LowerReplace();
    public readonly string Name = param.Get().LowerReplace();
}

public readonly struct LootTypeRowData(DataArray param)
{
    [Mark] public readonly string Type = param;
    [Mark] public readonly SeasonalLoot Season = param.GetEnum(SeasonalLoot.None);
}

public readonly struct RegionRowData(DataArray param) : ILogicSectorDataType<string, RegionRowData>
{
    [Mark] public readonly string Name = param;
    [Mark] public readonly string ConnectsFrom = param.GetOrDefault(s => s is "" ? "Menu" : s);
    [Mark] public readonly string[] Requirements = [.. ((string[])param).Select(s => s.LowerReplace())];

    public string GetIdentifier() => string.Join(",", Requirements);

    public bool IsMatch(RegionRowData matchAgainst)
        => Name == matchAgainst.Name && ConnectsFrom == matchAgainst.ConnectsFrom;

    public string GenRule() => $"{string.Join(" and ", Requirements)}";
    public string Print() => $"Region| {Name}, {ConnectsFrom}| [{string.Join(", ", Requirements)}]";
    public bool IsNoOption() => true;
    public string GenOption() => "";
}

public readonly struct LocationRowData(DataArray param)
{
    [Mark] public readonly string RawName = param;
    [Mark] public readonly string Region = param;
    [Mark] public readonly string[] Requirements = [.. ((string[])param).Select(s => s.LowerReplace())];
    [Mark] public readonly string LootType = param;
    [Mark] public readonly string Id = param;
    [Mark] public readonly string LocationItem = param;
    [Mark] public readonly bool DoNotUse = param;

    public string Name => char.IsNumber(Region[0]) ? $"House {Region}: {RawName}" : RawName;
    public string GenRule() => $"{string.Join(" and ", Requirements)}";
}

public enum SeasonalLoot { None, Halloween, Christmas }