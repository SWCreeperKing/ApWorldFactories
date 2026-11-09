using WorldBuilder;

namespace ApWorldFactories.Games.DinoBones;

public readonly struct BoneLabel(DataArray param)
{
    public readonly string Species = param;
    public readonly string Region = param;
    public readonly string Bone = param;

    public string Name => $"Place {Species} {Region}: {Bone}";
}

public readonly struct BonePath(DataArray param)
{
    public readonly string Object = param;
    public readonly BoneLabel Label = new(param);

    public int CrateNumber => int.Parse(Object.Split('/')[^1].Replace("PF_Crate_", ""));
    public string Crate => $"Crate {CrateNumber}";
}