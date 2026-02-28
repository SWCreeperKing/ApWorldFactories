namespace ApWorldFactories.Games.ConbunnCardboard;

public readonly struct RegionData(DataArray param)
{
    public readonly string RawRegion = param;
    public readonly string BackRegion = param;
    public readonly string[] Abilities = param;
    public readonly string TransitionName = param;
    public readonly string DoorFrame = param;
    public readonly bool IsSubZone = param;

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
    public readonly string Id = param;
    public readonly string Region = param;
    public readonly string[] Abilities = param;
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
    public readonly string Name = parm;
}

public record SkinData(DataArray param)
{
    public readonly string Name = param;
    public readonly string Id = param;
    public readonly string Region = param;
    public readonly string LocalInt = param;
    public readonly string GloablInt = param;

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
