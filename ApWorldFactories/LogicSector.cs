namespace ApWorldFactories;

public abstract class LogicSector<TSector, TData, TEnum>(TData data)
    where TSector : LogicSector<TSector, TData, TEnum>
    where TData : IGetLogicEnum<TEnum>, IGenRule, IPrintable, IGenOption
    where TEnum : struct, Enum
{
    public readonly Dictionary<TEnum, TData> Variants = new() { [data.GetEnum()] = data };

    public abstract bool HasNoRule();
    public abstract bool Match(TData data);

    public static LogicSector<TSector, TData, TEnum> operator +(LogicSector<TSector, TData, TEnum> sector, TData data)
    {
        if (sector.Variants.ContainsKey(data.GetEnum()))
            throw new ArgumentException($"Duplicate logic found: [{data.Print()}]");
        sector.Variants[data.GetEnum()] = data;
        return sector;
    }

    public static T[] CreateSectorFromData<T>(TData[] dataArr, Func<TData, T> maker)
        where T : LogicSector<TSector, TData, TEnum>
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

public interface IGetLogicEnum<out TEnum> where TEnum : struct, Enum
{
    public TEnum GetEnum();
}

public interface IGenRule
{
    public string GenRule();
}

public interface IGenOption
{
    public string GenOption();
}

public interface IPrintable
{
    public string Print();
}