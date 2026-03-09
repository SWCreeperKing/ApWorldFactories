namespace ApWorldFactories.Games.Poco;

public readonly struct RegionData(DataArray param)
{
    [Mark] public readonly string Region = param;
    [Mark] public readonly string ConnectsFrom = param.Get(false) is "" ? "Menu" : param;
    [Mark] public readonly string[] Requirements = param;

    public string GenRule() => string.Join(" and ", Requirements.Select(item => $"has[\"{item}\"]"));
}

public readonly struct LocationData(DataArray param)
{
    [Mark] public readonly string Location = param;
    [Mark] public readonly string Area = param;
    [Mark] public readonly string[] Requirements = param;
    [Mark] public readonly string Id = param;

    public string GenRule() => string.Join(" and ", Requirements.Select(item => $"has[\"{item}\"]"));
}

public readonly struct ItemData(DataArray param)
{
    [Mark] public readonly string Name = param;
}

public readonly struct NpcQuestData(DataArray param)
{
    [Mark] public readonly string NpcName = param;
    [Mark] public readonly string[] Requirements = param;
    [Mark] public readonly string Area = param;
    
    public string GenRule() => string.Join(" and ", Requirements.Select(item => $"has[\"{item}\"]"));
}