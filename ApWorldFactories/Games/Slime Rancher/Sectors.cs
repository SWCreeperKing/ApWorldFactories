using CreepyUtil.ClrCnsl;

namespace ApWorldFactories.Games.Slime_Rancher;

public class RegionSector(RegionRowData data) : LogicSector<RegionSector, RegionRowData, SkipLogic>(data)
{
    public readonly string From = data.From;
    public readonly string To = data.To;

    public override bool HasNoRule() => Variants.ContainsKey(SkipLogic.None);
    public override bool Match(RegionRowData data) => From == data.From && To == data.To;
}

public class InteractableSector(InteractableRowData data) : LogicSector<InteractableSector, InteractableRowData, SkipLogic>(data)
{
    public readonly string Id = data.Id;
    public readonly string LegacyName = data.LegacyName;
    public readonly string VagueName = data.VagueName;
    public readonly string PreciseName = data.PreciseName;
    public readonly string Region = data.Region;
    public readonly bool IsSecretStyle = data.IsSecretStyle;
    public readonly bool IsNote = data.IsNote;
    public readonly string GetText = data.GetText;

    public override bool HasNoRule() => Variants.ContainsKey(SkipLogic.None);
    public override bool Match(InteractableRowData data) => Id == data.Id && VagueName == data.VagueName;
}