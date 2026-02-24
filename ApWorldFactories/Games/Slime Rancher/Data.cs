using CreepyUtil.Archipelago.WorldFactory;

namespace ApWorldFactories.Games.Slime_Rancher;

public readonly struct InteractableRowData(string[] line)
{
    public readonly string Id = line[0];
    public readonly string Name = line[1].Trim();
    public readonly string Area = line[2];
    public readonly string CrackerLevel = line[3].Trim();
    public readonly bool NeedsJetpack = line[4] == "Yes";
    public readonly int MinJetpackEnergy = int.TryParse(line[5].Split(' ')[0], out var energy) ? energy : 100;
    public readonly string Summary = line[6];
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

public readonly struct GateRowData(string[] line)
{
    public readonly string Id = line[0];
    public readonly string Name = line[1];
    public readonly string FromArea = line[2];
    public readonly string ToArea = line[3];
    public readonly string SkippableWithJetpack = line[4];
    public string GetText => $"{Id},{Name}";
}

public readonly struct GordoRowData(string[] line)
{
    public readonly string Id = line[0];
    public readonly string Name = line[1];
    public readonly string Area = line[2];
    public readonly string Contents = line[3];
    public readonly string TeleporterLocation = line[4];
    public readonly string JetpackRequirement = line[5];
    public readonly string NormalFoodRequirement = line[6];
    public readonly string FavoriteFood = line[7];
    public string GetText => $"{Id},{Name},Favorite: {FavoriteFood}";
}

public readonly struct UpgradeRowData(string[] line)
{
    public readonly string Name = line[0];
    public readonly string Id = line[1];
    public readonly string Rule = line[2];
}

public readonly struct CorporateRowData(string[] line)
{
    public readonly string Location = line[0].Trim();
    public readonly int Level = line[0] != "" ? int.Parse(line[0].Split(':')[0].Split('.')[1]) : -1;
    public readonly string Price = line[1];
    public readonly string Area = line[2];

    public static implicit operator LocationData(CorporateRowData data) => new(data.Area, data.Location);
}

public readonly struct RegionData(string[] param)
{
    public readonly string Region = param[0];
    public readonly string[] BackConnections = param[1].SplitAndTrim(',');
}

public readonly struct ItemAmountData(string[] param)
{
    public readonly string Item = param[0];
    public readonly string Count = param[1];
    public readonly string ProgType = param[2];
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