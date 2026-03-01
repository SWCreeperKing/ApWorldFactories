namespace ApWorldFactories.Games.The_WereCleaner;

public readonly struct LevelData(DataArray param)
{
    [Mark] public readonly string LevelName = param;
    [Mark] public readonly string[] Collectibles = param;
    [Mark] public readonly string[] Abilities = param;
}

public readonly struct ItemData(DataArray param)
{
    [Mark] public readonly string Collectible = param;
    [Mark] public readonly string CollectibleId = param;
    [Mark] public readonly string Ability = param;
}

public readonly struct NpcData(DataArray param)
{
    [Mark] public readonly string LevelName = param;
    [Mark] public readonly string LevelId = param;
    [Mark] public readonly string[] Npcs = param;
}