using WorldBuilder;
using static ApWorldFactories.Games.Funko_Fusion.CollectableType;

namespace ApWorldFactories.Games.Funko_Fusion;

public readonly struct RegionRowData(DataArray param)
{
    [Mark] public readonly string Region = param;
    [Mark] public readonly string FromRegion = param;
    [Mark] public readonly bool IsSubArea = param;
}

public readonly struct ItemsRowData(DataArray param)
{
    [Mark] public readonly string Name = param;
    public string CleanName => Name.ToLower().Replace(" ", "");
}

public readonly struct CharacterRowData(DataArray param)
{
    [Mark] public readonly string Character = param;
    [Mark] public readonly string[] Abilities = param;
    [Mark] public readonly string DLC = param;
    [Mark] public readonly bool IsQoL = param;
}

public readonly struct LocationRowData(DataArray param) : ILogicSectorDataType<string, LocationRowData>
{
    [Mark] public readonly string BaseName = param;
    [Mark] public readonly string DisplayName = param;
    [Mark] public readonly CollectableType CollectableType = param.GetEnum(Unknown);
    [Mark] public readonly string Region = param;
    [Mark] public readonly string[] Abilities = param;
    [Mark] public readonly string[] Characters = param;

    public string GetName() => $"{BaseName}: {(DisplayName is "" ? CollectableType.GetName() : DisplayName)}";
    public string GetIdentifier() => string.Join(", ", [.. Abilities, .. Characters]);

    public bool IsMatch(LocationRowData matchAgainst)
        => BaseName == matchAgainst.BaseName && DisplayName == matchAgainst.DisplayName
                                             && CollectableType == matchAgainst.CollectableType
                                             && Region == matchAgainst.Region;

    public bool IsNoOption() => true;
    public string GenOption() => "";
    public string Print() => $"{GetName()}| {BaseName},{DisplayName},{CollectableType},{Region},[{GetIdentifier()}]";

    public string GenRule() => string.Join(" and ", [.. Abilities.Select(a => $"{a.ToLower().Replace(" ", "")}"), .. Characters.Select(c => $"has[\"{c}\"]"), $"level[\"{Region}\"]"]);
}

public enum CollectableType
{
    Unknown, SilverCrown, GoldCrown,
    KFCBucket, BrainAndBone, Amber,
    GreenProtopart_SodaPart, RedPrototype_WeaponPart
}

public static class CollectableTypeHelper
{
    extension(CollectableType collect)
    {
        public string GetName() => collect switch
        {
            SilverCrown => "Silver Crown", GoldCrown => "Gold Crown", KFCBucket => "KFC Bucket",
            BrainAndBone => "Brain & Bone", Amber => "Amber", GreenProtopart_SodaPart => "Green Protopart/Soda Part",
            RedPrototype_WeaponPart => "Red Prototype/Weapon Part", _ => "Unknown",
        };
    }
}