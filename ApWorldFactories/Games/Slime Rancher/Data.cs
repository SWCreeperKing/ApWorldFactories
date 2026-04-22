using CreepyUtil.Archipelago.WorldFactory;
using static ApWorldFactories.Games.Slime_Rancher.InteractableRowData;

namespace ApWorldFactories.Games.Slime_Rancher;

public readonly struct InteractableRowData(DataArray param) : IGetLogicEnum<SkipLogic>, IGenRule, IGenOption, IPrintable
{
    public static bool ForCompiler = false;
    [Mark] public readonly string Id = param;
    [Mark] public readonly string Name = param;
    [Mark] public readonly string Region = param;
    [Mark] public readonly string CrackerLevel = param;
    [Mark] public readonly bool NeedsJetpack = param;

    [Mark]
    public readonly int MinJetpackEnergy = int.TryParse(param.Get().Split(' ')[0], out var energy) ? energy : 100;

    [Mark] public readonly SkipLogic SkipLogic = ((string[])param).ParseSkipLogic();

    public bool IsSecretStyle => CrackerLevel == "Secret Style";
    public bool IsNote => CrackerLevel == "H Note";
    public string GetText => $"{Id},{Name}";

    public string GenRule()
    {
        List<string> rules = [];

        if (CrackerLevel.Contains("Treasure Cracker"))
        {
            var level = Math.Max(1, CrackerLevel.Count(c => c == 'I'));
            rules.Add(ForCompiler ? $"cracker[{level}]" : string.Join("", Enumerable.Repeat('c', level)));
        }

        if (NeedsJetpack) rules.Add(ForCompiler ? "jetpack" : "j");

        if (MinJetpackEnergy > 100)
        {
            var energyLevel = (int)Math.Ceiling(MinJetpackEnergy / 50f - 2f);
            rules.Add(ForCompiler ? $"energy[{energyLevel}]" : string.Join("", Enumerable.Repeat('e', energyLevel)));
        }

        if (ForCompiler && SkipLogic is not SkipLogic.None) rules.Add(SkipLogic.GenRule());

        return ForCompiler ? string.Join(" and ", rules) : string.Join("", rules);
    }

    public static implicit operator LocationData(InteractableRowData data) => new(data.Region, data.Name);
    public SkipLogic GetEnum() => SkipLogic;
    public string Print() => $"Location |{Name}|";
    public string GenOption() => SkipLogic.GenOption();
}

public readonly struct RegionRowData(DataArray param) : IGetLogicEnum<SkipLogic>, IGenRule, IGenOption, IPrintable
{
    [Mark] public readonly string From = param;
    [Mark] public readonly string To = param;
    [Mark] public readonly bool SlimeGated = param;

    [Mark] public readonly string[] RegionUnlocks = ((string[])param).Where(s => s.ToLower() is not ("" or "none"))
                                                                     .ToArray();

    [Mark] public readonly bool NeedsJetpack = param;

    [Mark]
    public readonly int MinJetpackEnergy = int.TryParse(param.Get().Split(' ')[0], out var energy) ? energy : 100;

    [Mark] public readonly string[] PlortsRequired = param;
    [Mark] public readonly string GateEvent = param;
    [Mark] public readonly SkipLogic SkipLogic = ((string[])param).ParseSkipLogic();

    public SkipLogic GetEnum() => SkipLogic;

    public string GenRule()
    {
        List<string> rules = [];
        if (ForCompiler) rules.AddRange(RegionUnlocks.Select(region => $"region[\"{region}\"]"));
        if (NeedsJetpack) rules.Add(ForCompiler ? "jetpack" : "j");

        if (MinJetpackEnergy > 100)
        {
            var energyLevel = (int)Math.Ceiling(MinJetpackEnergy / 50f - 2f);
            rules.Add(ForCompiler ? $"energy[{energyLevel}]" : string.Join("", Enumerable.Repeat('e', energyLevel)));
        }

        if (ForCompiler && SkipLogic is not SkipLogic.None) rules.Add(SkipLogic.GenRule());
        if (ForCompiler && PlortsRequired.Length != 0)
            rules.AddRange(PlortsRequired.Select(plort => $"has['{plort}']"));
        if (ForCompiler && GateEvent is not "") rules.Add($"gate[\"{GateEvent}\"]");

        return string.Join(" and ", rules);
    }

    public string Print()
        => $"Region: |{From},{To},{SlimeGated},{string.Join(';', RegionUnlocks)}|{NeedsJetpack},{SkipLogic}|";

    public string GenOption() => SkipLogic.GenOption();
}

public readonly struct SlimeRowData(DataArray param)
{
    [Mark] public readonly string Slime = param;
    [Mark] public readonly string FavoriteFood = param;
    [Mark] public readonly string[] SpawnLocations = param;
    [Mark] public readonly string PlortDrop = param;
    [Mark] public readonly int PlortId = param;
}

public readonly struct LocationNameGroupData(DataArray param)
{
    [Mark] public readonly string Group = param;
    [Mark] public readonly string[] Locations = param;
}

public readonly struct GateRowData(DataArray param)
{
    [Mark] public readonly string Id = param;
    [Mark] public readonly string Name = param;
    [Mark] public readonly string FromArea = param;
    [Mark] public readonly string ToArea = param;
    [Mark] public readonly string RegionUnlock = param;
    public string GetText => $"{Id},{Name}";
}

public readonly struct GordoRowData(DataArray param)
{
    [Mark] public readonly string Id = param;
    [Mark] public readonly string Name = param;
    [Mark] public readonly string Area = param;
    [Mark] public readonly string Contents = param;
    [Mark] public readonly string TeleporterLocation = param;
    [Mark] public readonly string JetpackRequirement = param;
    [Mark] public readonly string NormalFoodRequirement = param;
    [Mark] public readonly string FavoriteFood = param;
    public string GetText => $"{Id},{Name},Favorite: {FavoriteFood}";
}

public readonly struct UpgradeRowData(DataArray param)
{
    [Mark] public readonly string Name = param;
    [Mark] public readonly string Id = param;
    [Mark] public readonly string Area = param;
    [Mark] public readonly bool Is7ZeeUpgrade = param;
    [Mark] public readonly string UnlockNeed = param;
}

public readonly struct CorporateRowData(DataArray param)
{
    [Mark] public readonly string Location = param.Get().Trim();
    [Mark] public readonly int Level = param[0] != "" ? int.Parse(param[0].Split(':')[0].Split('.')[1]) : -1;
    [Mark] public readonly string Price = param;
    [Mark] public readonly string Area = param;

    public static implicit operator LocationData(CorporateRowData data) => new(data.Area, data.Location);
}

public readonly struct ItemAmountData(DataArray param)
{
    [Mark] public readonly string Item = param;
    [Mark] public readonly string Count = param;
    [Mark] public readonly string ProgType = param;
}

public readonly struct RegionUnlockRowData(DataArray param)
{
    [Mark] public readonly string ZoneId = param;
    [Mark] public readonly string RegionName = param;
    [Mark] public readonly bool Include = param;
    [Mark] public readonly bool ForCreditsGoal = param;
}

public class GateCreator : DataCreator<GateRowData>
{
    public override bool IsValidData(GateRowData t) => t.Id != "";
}

public class GordoCreator : DataCreator<GordoRowData>
{
    public override bool IsValidData(GordoRowData t) => t.Id != "";
}

public class UpgradeCreator : DataCreator<UpgradeRowData>
{
    public override bool IsValidData(UpgradeRowData t) => t.Name != "";
}

public class CorporateCreator : DataCreator<CorporateRowData>
{
    public override bool IsValidData(CorporateRowData t) => t.Location != "";
}

[Flags]
public enum SkipLogic
{
    None = 0, EasySkips = 1, PreciseMovement = 1 << 1,
    ObscureLocations = 1 << 2, JetpackBoosts = 1 << 3, LargoJumps = 1 << 4,
    DangerousSkips = 1 << 5, PostGame = 1 << 6,
}

public static class SkipLogicHelper
{
    public static string[] GenYamlNames(this SkipLogic logic)
    {
        List<string> rules = [];

        if (logic.HasFlag(SkipLogic.EasySkips)) rules.Add("easy_skips");
        if (logic.HasFlag(SkipLogic.PreciseMovement)) rules.Add("precise_movement");
        if (logic.HasFlag(SkipLogic.ObscureLocations)) rules.Add("obscure_locations");
        if (logic.HasFlag(SkipLogic.JetpackBoosts)) rules.Add("jetpack_boosts");
        if (logic.HasFlag(SkipLogic.LargoJumps)) rules.Add("largo_jumps");
        if (logic.HasFlag(SkipLogic.DangerousSkips)) rules.Add("dangerous_skips");
        if (logic.HasFlag(SkipLogic.PostGame)) rules.Add("postgame");

        return rules.ToArray();
    }

    public static string GenRule(this SkipLogic logic) => string.Join(
        " and ", logic.GenYamlNames().Select(s => $"yaml['{s}']")
    );

    public static string GenOption(this SkipLogic logic) => string.Join(
        " and ", logic.GenYamlNames().Select(s => $"options.{s}")
    );

    public static SkipLogic ParseSkipLogic(this string[] skip)
    {
        if (skip.Length == 0) return SkipLogic.None;
        var logics = skip.Select(s => Enum.Parse<SkipLogic>(s.Replace(" ", "").Replace("-", ""), true)).ToArray();
        return logics.Length == 1 ? logics[0] : logics.Aggregate((fA, fB) => fA | fB);
    }
}