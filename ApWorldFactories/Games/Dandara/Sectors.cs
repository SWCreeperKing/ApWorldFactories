namespace ApWorldFactories.Games.Dandara;

public class ConnectionsSector(ConnectionRowData data) : LogicSector<ConnectionsSector, ConnectionRowData, string>(data)
{
    public string From = data.From;
    public string To = data.To;
}

public class ChestsSector(ChestsRowData data, Dictionary<string, string> roomIdToName)
    : LogicSector<ChestsSector, ChestsRowData, string>(data)
{
    public string CheckType = data.CheckType;
    public string RoomId = data.RoomId;
    public string ChestName = data.ChestName;
    public string ChestContents = data.ChestContents;
    public string Position = data.Position;
    public string Name = data.GetName(roomIdToName);
}

public class EventItemSector(EventItemsRowData data) : LogicSector<EventItemSector, EventItemsRowData, string>(data)
{
    public string EventName = data.EventName;
    public string RoomId = data.RoomId;
    public string EventItem = data.EventItem;
}