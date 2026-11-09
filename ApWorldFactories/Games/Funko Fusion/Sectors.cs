using WorldBuilder;

namespace ApWorldFactories.Games.Funko_Fusion;

public class LocationSector(LocationRowData data) : LogicSector<LocationSector, LocationRowData, string>(data)
{
   public readonly string Name = data.GetName();
   public readonly CollectableType CollectableType = data.CollectableType;
   public readonly string Region = data.Region;
}