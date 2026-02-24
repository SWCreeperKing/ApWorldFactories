namespace ApWorldFactories.Games.The_WereCleaner;

public readonly struct LevelData(string[] param)
{
    public readonly string LevelName = param[0];
    public readonly string[] Collectibles = param[1].SplitAndTrim(',');
    public readonly string[] Abilities = param[2].SplitAndTrim(',');
}

public readonly struct ItemData(string[] param)
{
    public readonly string Collectible = param[0];
    public readonly string CollectibleId = param[1];
    public readonly string Ability = param[2];
}

public readonly struct NpcData(string[] param)
{
    public readonly string LevelName = param[0];
    public readonly string LevelId = param[1];
    public readonly string[] Npcs = param[2].SplitAndTrim(',');
}