using WorldBuilder;

namespace ApWorldFactories.Games.Thief_Simulator_1;

public class RegionSector(RegionRowData thisData) : LogicSector<RegionSector, RegionRowData, string>(thisData)
{
    public string Name = thisData.Name;
    public string From = thisData.ConnectsFrom;
    public bool IsRegionUnlockable = thisData.IsRegionUnlockable;
}