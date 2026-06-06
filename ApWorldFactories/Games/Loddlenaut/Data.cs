using static CreepyUtil.Archipelago.WorldFactory.ItemFactory;

namespace ApWorldFactories.Games.Loddlenaut;

public readonly struct RegionRowData(DataArray param)
{
    [Mark] public readonly string Region = param;
    [Mark] public readonly int Id = param;
    [Mark] public readonly bool RequiresDepth = param;
    [Mark] public readonly bool IsBiome = param;
    [Mark] public readonly string[] Plants = param;
    [Mark] public readonly string[] AvailableUpgrades = param;
    [Mark] public readonly string[] CleanRequirements = param;
}

public readonly struct RegionConnectionsRowData(DataArray param)
{
    [Mark] public readonly int A = param;
    [Mark] public readonly int B = param;
}

public readonly struct ItemsRowData(DataArray param)
{
    [Mark] public readonly string ItemName = param;
    [Mark] public readonly bool IsUpgrade = param;
    [Mark] public readonly int Count = param;
    [Mark] public readonly ItemClassification Classification = param.GetEnum<ItemClassification>();
}

public readonly struct PlantRowData(DataArray param)
{
    [Mark] public readonly string PlantName = param;
}

public readonly struct BadgeRowData(DataArray param)
{
    [Mark] public readonly string Name = param;
    [Mark] public readonly int RegionId = param;
}

public readonly struct CookingRowData(DataArray param)
{
    [Mark] public readonly string Food = param;
    [Mark] public readonly string[] Plants = param;
    [Mark] public readonly string[] Foods = param;

    public string GenRule() => string.Join(" and ",  Plants.Concat(Foods).Select(ingredient => $"has[\"{ingredient}\"]"));
}

public readonly struct EvolutionRowData(DataArray param)
{
    [Mark] public readonly string Name = param;
    [Mark] public readonly string[] Foods = param;
    [Mark] public readonly string[] Plants = param;
    
    public string GenRule() => string.Join(" and ",  Plants.Concat(Foods).Select(ingredient => $"has[\"{ingredient}\"]"));
}