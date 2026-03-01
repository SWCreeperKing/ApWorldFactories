using CreepyUtil.Archipelago.WorldFactory;

namespace ApWorldFactories.Games.Slime_Rancher;

public readonly struct InteractableRowData(DataArray param)
{
    [Mark] public readonly string Id = param;
    [Mark] public readonly string Name = param;
    [Mark] public readonly string Area = param;
    [Mark] public readonly string CrackerLevel = param;
    [Mark] public readonly bool NeedsJetpack = param;
    [Mark] public readonly int MinJetpackEnergy = int.TryParse(param.Get().Split(' ')[0], out var energy) ? energy : 100;
    [Mark] public readonly string Summary = param;
    public bool IsSecretStyle => CrackerLevel == "Secret Style";
    public bool IsNote => CrackerLevel == "Note";
    public string GetText => $"{Id},{Name},{Summary}";

    public string GenRule(bool forCompiler)
    {
        List<string> rules = [];

        if (CrackerLevel.Contains("Treasure Cracker"))
        {
            var level = Math.Max(1, CrackerLevel.Count(c => c == 'I'));
            rules.Add(forCompiler ? $"cracker[{level}]" : string.Join("", Enumerable.Repeat('c', level)));
        }

        if (NeedsJetpack) { rules.Add(forCompiler ? "jetpack" : "j"); }

        if (MinJetpackEnergy > 100)
        {
            var energyLevel = (int)Math.Ceiling(MinJetpackEnergy / 50f - 2f);
            rules.Add(
                forCompiler ? $"energy[{energyLevel}]" : string.Join("", Enumerable.Repeat('e', energyLevel))
            );
        }

        return forCompiler ? string.Join(" and ", rules) : string.Join("", rules);
    }

    public static implicit operator LocationData(InteractableRowData data) => new(data.Area, data.Name);
}

public readonly struct GateRowData(DataArray param)
{
    [Mark] public readonly string Id = param;
    [Mark] public readonly string Name = param;
    [Mark] public readonly string FromArea = param;
    [Mark] public readonly string ToArea = param;
    [Mark] public readonly string SkippableWithJetpack = param;
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
    [Mark] public readonly string Rule = param;
}

public readonly struct CorporateRowData(DataArray param)
{
    [Mark] public readonly string Location = param.Get().Trim();
    [Mark] public readonly int Level = param[0] != "" ? int.Parse(param[0].Split(':')[0].Split('.')[1]) : -1;
    [Mark] public readonly string Price = param;
    [Mark] public readonly string Area = param;

    public static implicit operator LocationData(CorporateRowData data) => new(data.Area, data.Location);
}

public readonly struct RegionData(DataArray param)
{
    [Mark] public readonly string Region = param;
    [Mark] public readonly string[] BackConnections = param;
}

public readonly struct ItemAmountData(DataArray param)
{
    [Mark] public readonly string Item = param;
    [Mark] public readonly string Count = param;
    [Mark] public readonly string ProgType = param;
}

public class InteractableCreator(string[] zones) : DataCreator<InteractableRowData>
{
    public override bool IsValidData(InteractableRowData t) => zones.Contains(t.Area);
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

public class RegionDataCreator : DataCreator<RegionData>
{
    public override bool IsValidData(RegionData t) => t.BackConnections.Length != 0 && t.Region is not "Menu";
}