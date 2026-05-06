namespace ApWorldFactories;

public abstract class LogicSector<TSector, TData, TIdentifierType>(TData thisData)
    where TSector : LogicSector<TSector, TData, TIdentifierType>
    where TData : ILogicSectorDataType<TIdentifierType, TData> where TIdentifierType : notnull
{
    public readonly TData ThisData = thisData;

    public readonly Dictionary<TIdentifierType, TData> Variants = new() { [thisData.GetIdentifier()] = thisData };
    public bool HasNoOption { get; private set; } = thisData.IsNoOption();

    public bool Match(TData data) => ThisData.IsMatch(data);

    public static LogicSector<TSector, TData, TIdentifierType> operator +(
        LogicSector<TSector, TData, TIdentifierType> sector, TData data)
    {
        if (sector.Variants.ContainsKey(data.GetIdentifier()))
            throw new ArgumentException($"Duplicate logic found: [{data.Print()}]");
        sector.Variants[data.GetIdentifier()] = data;
        sector.HasNoOption = sector.HasNoOption || data.IsNoOption();
        return sector;
    }

    public static T[] CreateSectorFromData<T>(TData[] dataArr, Func<TData, T> maker)
        where T : LogicSector<TSector, TData, TIdentifierType>
    {
        List<T> rawSector = [];
        foreach (var dataObj in dataArr)
        {
            var data = rawSector.FirstOrDefault(tData => tData is not null && tData.Match(dataObj), null);
            if (data is not null)
            {
                _ = data + dataObj;
                continue;
            }

            rawSector.Add(maker(dataObj));
        }

        return rawSector.ToArray();
    }

    public string GenRule() => string.Join(
        " or ",
        Variants.Values.Select(sector => sector.GenRule()).Where(rule => rule.Trim() is not "")
                .Select(rule => $"( {rule} )")
    );

    public string GenOption() => string.Join(
        " or ",
        Variants.Values.Select(sector => sector.GenOption()).Where(rule => rule.Trim() is not "")
                .Select(rule => $"( {rule} )")
    );
}

public interface ILogicSectorDataType<out TIdentifierType, TDataType> where TIdentifierType : notnull
{
    public TIdentifierType GetIdentifier();
    public bool IsMatch(TDataType matchAgainst);
    public bool IsNoOption();
    public string GenRule();
    public string GenOption();
    public string Print();
}