using CreepyUtil.Archipelago.WorldFactory;

namespace ApWorldFactories.Extended;

public class OptionSet(string def, string collection) : IOptionType
{
    public string DataType() => "str";
    public string Parameter() => "OptionSet";

    public IPythonVariable[] GetData()
        => [new Variable("valid_keys", $"frozenset({collection})"), new Variable("default", def)];
}