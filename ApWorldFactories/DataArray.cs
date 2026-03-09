namespace ApWorldFactories;

public class DataArray(string[] param)
{
    public string[] Params = param;
    public int Index;

    public string Get(bool move = true)
    {
        var data = Params[Index];
        if (move) Index++;
        return data.Trim();
    }

    public bool GetIsTrue(bool move = true) => Get(move).IsTrue();
    public string[] GetSplitAndTrim(char split = ',', bool move = true) => Get(move).SplitAndTrim(split);
    public int TryGetInt(int def = 0, bool move = true) => int.TryParse(Get(move), out var i) ? i : def;
    public float TryGetFloat(float def = 0, bool move = true) => float.TryParse(Get(move), out var f) ? f : def;

    public TEnum GetEnum<TEnum>(bool ignoreCase = true, bool move = true) where TEnum : struct
        => Enum.Parse<TEnum>(Get(move).Replace(" ", "").Replace("&", "And"), ignoreCase);

    public DataArray SetIndex(int i)
    {
        Index = i;
        return this;
    }

    public string this[int i] => Params[i];

    public static implicit operator DataArray(string[] array) => new(array);
    public static implicit operator string(DataArray array) => array.Get();
    public static implicit operator string[](DataArray array) => array.GetSplitAndTrim().Where(s => s is not "").ToArray();
    public static implicit operator bool(DataArray array) => array.GetIsTrue();
    public static implicit operator int(DataArray array) => array.TryGetInt();
    public static implicit operator float(DataArray array) => array.TryGetFloat();
}