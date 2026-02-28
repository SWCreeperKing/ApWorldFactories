namespace ApWorldFactories.Games.The_WereCleaner;

public readonly struct LevelData(DataArray param)
{
    public readonly string LevelName = param;
    public readonly string[] Collectibles = param;
    public readonly string[] Abilities = param;
}

public readonly struct ItemData(DataArray param)
{
    public readonly string Collectible = param;
    public readonly string CollectibleId = param;
    public readonly string Ability = param;
}

public readonly struct NpcData(DataArray param)
{
    public readonly string LevelName = param;
    public readonly string LevelId = param;
    public readonly string[] Npcs = param;
}