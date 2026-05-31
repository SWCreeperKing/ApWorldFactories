namespace ApWorldFactories.Games.Placid_Plastic_Duck_Simulator;

public readonly struct DuckRowData(DataArray param)
{
    [Mark] public readonly string DuckName = param;
    [Mark] public readonly string DuckId = param;
    [Mark] public readonly int Column = param;
    [Mark] public readonly DlcType DlcType = param.GetEnum<DlcType>();
    [Mark] public readonly bool HasUniqueQuack = param;
    [Mark] public readonly bool SpecialSpawn = param;
    [Mark] public readonly bool Include = param;
}

public readonly struct DuckLogicRowData(DataArray param) : ILogicSectorDataType<string, DuckLogicRowData>
{
    [Mark] public readonly string DuckName = param;
    [Mark] public readonly bool IsEventDuck = param;
    [Mark] public readonly bool IsSeasonalDuck = param;
    [Mark] public readonly string[] DucksRequired = param;
    [Mark] public readonly string[] SpecialDuckProperties = param;
    [Mark] public readonly string[] Map = param;

    public string GetIdentifier() => throw new NotImplementedException();
    public bool IsMatch(DuckLogicRowData matchAgainst) => throw new NotImplementedException();
    public bool IsNoOption() => throw new NotImplementedException();
    public string GenRule() => throw new NotImplementedException();
    public string GenOption() => "";
    public string Print() => throw new NotImplementedException();
}

public readonly struct MapRowData(DataArray param)
{
    [Mark] public readonly string MapName = param;
    [Mark] public readonly string MapId = param;
}

public readonly struct DlcNameRowData(DataArray param)
{
    [Mark] public readonly string DlcName = param.Get(false);
    public readonly DlcType DlcType = param.GetEnum<DlcType>();
}

public enum DlcType
{
    BaseGame, DucksPlease, QuackingTheIce,
    DuckAddiction, HippospaceDownload, SoManyDucks,
    RooftopOnePercent, DucksGalore, Ducklings,
    VirtualThermae,
}

public static class DlcTypeHelper
{
    public static string OptionName(this DlcType type) => type switch
    {
        DlcType.BaseGame => "base_game", DlcType.DucksPlease => "ducks_please",
        DlcType.QuackingTheIce => "quacking_the_ice", DlcType.DuckAddiction => "duck_addiction",
        DlcType.HippospaceDownload => "hippospace_download", DlcType.SoManyDucks => "so_many_ducks",
        DlcType.RooftopOnePercent => "rooftop_one_percent", DlcType.DucksGalore => "ducks_galore",
        DlcType.Ducklings => "ducklings", DlcType.VirtualThermae => "virtual_thermae", _ => "unknown_dlc"
    };

    public static string ItemName(this DlcType type) => type switch
    {
        DlcType.DucksPlease => "Ducks Please Ducks", DlcType.DuckAddiction => "Duck Addiction Ducks",
        DlcType.SoManyDucks => "So Many Ducks Ducks", DlcType.DucksGalore => "Ducks Galore Ducks",
        DlcType.Ducklings => "Ducklings Ducks", _ => ""
    };
}