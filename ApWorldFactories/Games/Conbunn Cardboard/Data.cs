namespace ApWorldFactories.Games.ConbunnCardboard;

public readonly struct RegionData(DataArray param)
{
    [Mark] public readonly string RawRegion = param;
    [Mark] public readonly string BackRegion = param;
    [Mark] public readonly string[] Abilities = param;
    [Mark] public readonly string TransitionName = param;
    [Mark] public readonly string DoorFrame = param;
    [Mark] public readonly bool IsSubZone = param;

    public string Region => IsSubZone ? $"{RawRegion} ({BackRegion})" : RawRegion;
    public bool HasTransition => TransitionName is not "";
    public bool HasDoor => DoorFrame is not "";

    public string GenRule
    {
        get
        {
            List<string> req = [];
            if (Abilities.Contains("Bounce Pads")) req.Add("pads");
            if (Abilities.Contains("Dash")) req.Add("dash");
            if (HasTransition) req.Add($"unlock[\"{Region}\"]");
            return string.Join(" and ", req);
        }
    }
}

public readonly struct LocData(DataArray param)
{
    [Mark] public readonly string Id = param;
    [Mark] public readonly string Region = param;
    [Mark] public readonly string[] Abilities = param;
    public bool IsCoin => Id.StartsWith("Coin_");

    public string GenRule
    {
        get
        {
            List<string> req = [];
            if (Abilities.Contains("Bounce Pads")) req.Add("pads");
            if (Abilities.Contains("Dash")) req.Add("dash");
            return string.Join(" and ", req);
        }
    }
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

    public string GenRule(Dictionary<string, string> regionMap)
    {
        List<string> req = [$"unlock[\"{regionMap[Region]}\"]"];
        if (Id is "1") req.Add("coin[50]");
        if (Id is "2") req.Add("coin[150]");
        return string.Join(" and ", req);
    }
}

public class RegionDataCreator : DataCreator<RegionData>
{
    public override bool IsValidData(RegionData t) => t.Region is not "Menu";
}
