namespace ApWorldFactories.Games.ConbunnCardboard;

public readonly struct RegionData(string[] param)
{
    public readonly string RawRegion = param[0];
    public readonly string BackRegion = param[1];
    public readonly string[] Abilities = param[2].SplitAndTrim(',');
    public readonly string TransitionName = param[3];
    public readonly string DoorFrame = param[4];
    public readonly bool IsSubZone = param[5] is not "" && param[5].ToLower()[0] == 'y';

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

public readonly struct LocData(string[] param)
{
    public readonly string Id = param[0];
    public readonly string Region = param[1];
    public readonly string[] Abilities = param[2].Split(',').Select(s => s.Trim()).ToArray();
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

public readonly struct AbilityData(string[] parm)
{
    public readonly string Name = parm[0];
}

public record SkinData(string[] param)
{
    public readonly string Name = param[0];
    public readonly string Id = param[1];
    public readonly string Region = param[2];
    public readonly string LocalInt = param[3];
    public readonly string GloablInt = param[4];

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
