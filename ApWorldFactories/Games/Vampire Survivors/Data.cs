namespace ApWorldFactories.Games.Vampire_Survivors;

public readonly struct StageData(DataArray param) : IGetDlc, IGetName
{
    [Mark] public readonly string StageName = param;
    [Mark] public readonly string StageId = param;
    [Mark] public readonly VsDlc StageDlc = param.Get().ToVampireSurvivorsDlc();

    public bool IsMatch(StageData data) => StageId == data.StageId;
    public bool IsAcceptable() => StageName is not "";
    public VsDlc GetDlc() => StageDlc;
    public string GetName() => StageName;
}

public readonly struct StageEnemyData(string enemyId, string stageId, int minute)
{
    [Mark] public readonly string EnemyId = enemyId;
    [Mark] public readonly string StageId = stageId;
    [Mark] public readonly int Minute = minute;

    public StageEnemyData(DataArray param) : this(param, param, param)
    {
    }
    
    public bool IsMatch(StageEnemyData data) => EnemyId == data.StageId && StageId == data.StageId;
}

public readonly struct StageBossData(string bossId, string stageId, int minute)
{
    [Mark] public readonly string BossId = bossId;
    [Mark] public readonly string StageId = stageId;
    [Mark] public readonly int Minute = minute;

    public StageBossData(DataArray param) : this(param, param, param)
    {
    }
    
    public bool IsMatch(StageBossData data) => BossId == data.BossId && StageId == data.StageId;
}

public readonly struct EnemyData(DataArray param) : IGetDlc, IGetName
{
    [Mark] public readonly string Name = param.Get().Trim('-');
    [Mark] public readonly string EnemyId = param;
    [Mark] public readonly bool BestiaryInclude = param;
    [Mark] public readonly bool NeedArcana = param;
    [Mark] public readonly VsDlc Dlc = param.Get().ToVampireSurvivorsDlc();
    [Mark] public readonly string[] Variants = param.GetSplitAndTrim().Distinct().ToArray();

    public bool IsMatch(EnemyData data) => EnemyId == data.EnemyId;
    public bool IsAcceptable() => Name is not "" || !BestiaryInclude;
    public VsDlc GetDlc() => Dlc;
    public string GetName() => Name;
}

public readonly struct CharacterData(DataArray param) : IGetDlc, IGetName
{
    [Mark] public readonly string Name = param;
    [Mark] public readonly string CharacterId = param;
    [Mark] public readonly VsDlc Dlc = param.Get().ToVampireSurvivorsDlc();

    public bool IsMatch(CharacterData data) => CharacterId == data.CharacterId;
    public bool IsAcceptable() => Name is not "";
    public VsDlc GetDlc() => Dlc;
    public string GetName() => Name;
}

public readonly struct CharacterClassificationData(DataArray param) : IClassifier, IGetName
{
    [Mark] public readonly string Name = param;
    [Mark] public readonly bool IsSecretCharacter = param;
    [Mark] public readonly bool IsMegaloCharacter = param;
    [Mark] public readonly bool IsUnfairCharacter = param;

    public bool IsNormal => !IsSecretCharacter && !IsMegaloCharacter && !IsUnfairCharacter;
    public bool IsSecret => IsSecretCharacter && !IsMegaloCharacter && !IsUnfairCharacter;
    public bool IsMegalo => IsMegaloCharacter && !IsUnfairCharacter;
    public bool IsUnfair => IsUnfairCharacter;
    public bool GetClassifier(int type) => ((bool[])[IsNormal, IsSecret, IsMegalo, IsUnfair])[type];
    public string GetName() => Name;
}

public readonly struct StageClassificationData(DataArray param) : IClassifier, IGetName
{
    [Mark] public readonly string Name = param;
    [Mark] public readonly bool IsBonusStage = param;
    [Mark] public readonly bool IsChallengeStage = param;

    public bool IsNormal => !IsBonusStage && !IsChallengeStage;
    public bool IsBonus => IsBonusStage && !IsChallengeStage;
    public bool IsChallenge => IsChallengeStage;
    public bool GetClassifier(int type) => ((bool[])[IsNormal, IsBonus, IsChallenge])[type];
    public string GetName() => Name;
}

public readonly struct EnemyBlacklistData(DataArray param)
{
    [Mark] public readonly string EnemyId = param;
}

public interface IGetDlc
{
    public VsDlc GetDlc();
}

public interface IGetName
{
    public string GetName();
}

public interface IClassifier
{
    public bool GetClassifier(int type);
}

public static class Converter
{
    public static VsDlc ToVampireSurvivorsDlc(this string text) => text.ToLower() switch
    {
        "" => VsDlc.None,
        "moonspell" => VsDlc.Moonspell,
        "foscari" => VsDlc.Foscari,
        "chalcedony" => VsDlc.Amongus,
        "firstblood" => VsDlc.Guns,
        "thosepeople" => VsDlc.Castlevania,
        "emeralds" => VsDlc.Emerald,
        "lemon" => VsDlc.Balatro,
    };

    public static string GetProperName(this VsDlc dlc) => dlc switch
    {
        VsDlc.None => "Vanilla",
        VsDlc.Moonspell => "Legacy of the Moonspell DLC",
        VsDlc.Foscari => "Tides of the Foscari DLC",
        VsDlc.Amongus => "Emergency Meeting DLC",
        VsDlc.Guns => "Operation Guns DLC",
        VsDlc.Castlevania => "Ode to Castlevania DLC",
        VsDlc.Emerald => "Emerald Diorama DLC",
        VsDlc.Balatro => "Ante Chamber DLC",
    };

    public static string[] GetNames<T>(this IEnumerable<T> arr) where T : IGetName
        => arr.Select(t => t.GetName()).ToArray();

    public static T[] OfClassifier<T>(this IEnumerable<T> arr, int type) where T : IClassifier
        => arr.Where(t => t.GetClassifier(type)).ToArray();

    public static T[] OfDlc<T>(this IEnumerable<T> arr, VsDlc dlc) where T : IGetDlc
        => arr.Where(t => t.GetDlc() == dlc).ToArray();

    public static bool Contains(this EnemyBlacklistData[] arr, string id) => arr.Any(data => data.EnemyId == id);
}

public enum VsDlc
{
    None,
    Moonspell,
    Foscari,
    Amongus,
    Guns,
    Castlevania,
    Emerald,
    Balatro,
}