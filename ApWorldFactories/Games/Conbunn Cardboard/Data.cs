namespace ApWorldFactories.Games.ConbunnCardboard;

public readonly struct RegionRowData(DataArray param)
{
    [Mark] public readonly string Region = param;
    [Mark] public readonly bool IsSubZone = param;
    [Mark] public readonly string DoorFrame = param;
    [Mark] public readonly string BackingRegion = param;

    public string RegionName => IsSubZone ? $"{Region} ({BackingRegion})" : Region;
    public bool HasDoor => DoorFrame is not "";
}

public readonly struct ConnectionRowData(DataArray param)
{
    [Mark] public readonly string From = param;
    [Mark] public readonly string To = param;
    [Mark] public readonly string[] Abilities = param;
    [Mark] public readonly string TransitionName = param;
    public bool HasTransition => TransitionName is not "";

    public string GenRule(Dictionary<string, string> regionMap)
        => ConbunnGenRule.GenRule([], Abilities, regionMap[To], HasTransition, 0);
}

public readonly struct LocData(DataArray param)
{
    [Mark] public readonly string Id = param;
    [Mark] public readonly string Region = param;
    [Mark] public readonly string[] Abilities = param;
    public bool IsCoin => Id.StartsWith("Coin_");
    public string GenRule() => ConbunnGenRule.GenRule([], Abilities, Region, false, 0);
}

public readonly struct AbilityData(DataArray parm)
{
    [Mark] public readonly string Name = parm;
}

public record SkinData(DataArray param)
{
    [Mark] public readonly string Name = param;
    [Mark] public readonly string Id = param;
    [Mark] public readonly string Region = param;
    [Mark] public readonly string LocalInt = param;
    [Mark] public readonly string GloablInt = param;

    public string GenRule(Dictionary<string, string> regionMap) => ConbunnGenRule.GenRule(
        [regionMap[Region]], [], Region, false, Id switch { "1" => 50, "2" => 150, _ => 0 }
    );
}

public static class ConbunnGenRule
{
    public static string GenRule(string[] regionsReq, string[] abilities, string region, bool hasTransition,
        int coinReq)
    {
        List<string> req = [];
        if (regionsReq.Length != 0) req.AddRange(regionsReq.Select(r => $"unlock[\"{r}\"]"));
        if (abilities.Contains("Bounce Pads")) req.Add("pads");
        if (abilities.Contains("Dash")) req.Add("dash");
        if (abilities.Contains("Cable Car")) req.Add("cablecar");
        if (hasTransition) req.Add($"unlock[\"{region}\"]");
        if (coinReq > 0) req.Add($"coin[{coinReq}]");
        return string.Join(" and ", req);
    }
}