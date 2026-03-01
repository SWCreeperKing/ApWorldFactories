using System.Reflection;
using CreepyUtil;
using CreepyUtil.Archipelago.WorldFactory;
using CreepyUtil.ClrCnsl;
using CreepyUtil.Pos;

namespace ApWorldFactories;

public abstract class BuildData
{
    public const string DDrive = "D:/Programs/steam/steamapps/common";
    public const string FDrive = "F:/SteamLibrary/steamapps/common";

    public const string GitLink
        = "https://github.com/SWCreeperKing/ApWorldFactories/tree/master/ApWorldFactories/Games";

    public abstract string SteamDirectory { get; }
    public abstract string ModFolderName { get; }
    public abstract string GameName { get; }
    public abstract string ApWorldName { get; }
    public abstract string GoogleSheetId { get; }
    public abstract string WorldVersion { get; }
    public virtual string ArchipelagoVersion => "0.6.5";
    public virtual string GameFolder => GameName;

    public virtual string GamePath => SteamDirectory is "" ? "" : $"{SteamDirectory}/{GameName}";

    public virtual string WriteOutputDirectory => SteamDirectory is "" || ModFolderName is ""
        ? $"../../../Output/{GameName}"
        : $"{SteamDirectory}/{GameName}/Mods/{ModFolderName}/Data";

    public virtual string ApWorldPath => $"E:/coding projects/python/Deathipelago/worlds/{ApWorldName}";
    public virtual string CsvPath => $"E:/coding projects/C#/ApWorldFactories/ApWorldFactories/Spreadsheets/{GameName}";
    public virtual string MainSheetLink => $"https://docs.google.com/spreadsheets/d/{GoogleSheetId}/export?format=csv";

    public virtual Dictionary<string, string> SheetGids { get; } = [];

    public void DownloadSheets()
    {
        if (!Directory.Exists(CsvPath)) Directory.CreateDirectory(CsvPath);
        using var client = new HttpClient();
        var pos = ClrCnsl.GetCursor();
        var amount = SheetGids.Count + 1;
        PrintProgress(pos, 0, amount);
        DownloadSheet(client, MainSheetLink, "main");
        PrintProgress(pos, 1, amount);
        if (SheetGids.Count == 0) return;
        var i = 2;
        foreach (var (name, gid) in SheetGids)
        {
            DownloadSheet(client, $"{MainSheetLink}&gid={gid}", name);
            PrintProgress(pos, i++, amount);
        }
    }

    private void DownloadSheet(HttpClient client, string url, string output)
    {
        var response = client.GetAsync(url).GetAwaiter().GetResult();
        var csvData = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
        File.WriteAllBytes($"{CsvPath}/{output}.csv", csvData);
    }

    private void PrintProgress(Pos pos, int curr, int amount)
    {
        ClrCnsl.SetCursor(pos);
        ClrCnsl.ProgressBar(curr, amount, 20, d => d switch { < 1 => ConsoleColor.Cyan, >= 1 => ConsoleColor.Green });
        ClrCnsl.Write($"\n{curr}/{amount} ");
    }

    public CsvParser GetSpreadsheet(string sheet, int linesFromTop = 1, int linesFromLeft = 0)
        => new($"{CsvPath}/{sheet}.csv", linesFromTop, linesFromLeft);

    public void WriteData(string file, IEnumerable<string> data, string ext = "txt")
    {
        if (WriteOutputDirectory is "") return;
        File.WriteAllLines($"{WriteOutputDirectory}/{file}.{ext}", data);
    }

    public void Run()
    {
        var builder = new WorldFactory(GameName)
                     .SetOnCompilerError((e, s) => ClrCnsl.WriteLine($"[#red]Error: [{s}]\n{e}"))
                     .SetOutputDirectory(ApWorldPath);
        if (WriteOutputDirectory is not "" && !Directory.Exists(WriteOutputDirectory))
            Directory.CreateDirectory(WriteOutputDirectory);
        if (!Directory.Exists(ApWorldPath)) Directory.CreateDirectory(ApWorldPath);

        var options_fact = builder.GetOptionsFactory(GitLink);
        var host_fact = builder.GetHostSettingsFactory(GitLink);
        var location_fact = builder.GetLocationFactory(GitLink);
        var item_fact = builder.GetItemFactory(GitLink);
        var rule_fact = builder.GetRuleFactory(GitLink);
        var region_fact = builder.GetRegionFactory(GitLink);
        var init_fact = builder.GetInitFactory(GitLink);

        RunShenanigans();

        Options(builder, options_fact);
        HostSettings(builder, host_fact);
        Locations(builder, location_fact);
        Items(builder, item_fact);
        Rules(builder, rule_fact);
        Regions(builder, region_fact);
        Init(builder, init_fact);

        GenerateOptions(options_fact);
        GenerateHostSettings(host_fact);
        GenerateLocations(out var locationList, location_fact);
        GenerateItems(out var itemList, item_fact);
        GenerateRules(rule_fact);
        GenerateRegions(region_fact);
        GenerateInit(init_fact);

        GenerateJson(builder);

        ProcessLocationList(locationList);
        ProcessItemList(itemList);
    }

    public abstract void RunShenanigans();

    public abstract void Options(WorldFactory _, OptionsFactory options_fact);

    public virtual void HostSettings(WorldFactory _, HostSettingsFactory host_fact)
    {
    }

    public abstract void Locations(WorldFactory _, LocationFactory location_fact);
    public abstract void Items(WorldFactory _, ItemFactory item_fact);
    public abstract void Rules(WorldFactory _, RuleFactory rule_fact);
    public abstract void Regions(WorldFactory _, RegionFactory region_fact);
    public abstract void Init(WorldFactory _, WorldInitFactory init_fact);

    public virtual void GenerateOptions(OptionsFactory optionsFactory) => optionsFactory.GenerateOptionFile();

    public virtual void GenerateHostSettings(HostSettingsFactory hostSettingsFactory)
        => hostSettingsFactory.GenerateHostSettingsFile();

    public virtual void GenerateLocations(out string[] locationList, LocationFactory locationFactory)
        => locationFactory.GenerateLocationFile(out locationList);

    public virtual void GenerateItems(out string[] itemList, ItemFactory itemFactory)
        => itemFactory.GenerateItemsFile(out itemList);

    public virtual void GenerateRules(RuleFactory ruleFactory) => ruleFactory.GenerateRulesFile();
    public virtual void GenerateRegions(RegionFactory regionFactory) => regionFactory.GenerateRegionFile();
    public virtual void GenerateInit(WorldInitFactory initFactory) => initFactory.GenerateInitFile();

    public virtual void GenerateJson(WorldFactory worldFactory)
        => worldFactory.GenerateArchipelagoJson(ArchipelagoVersion, WorldVersion, "SW_CreeperKing");

    public virtual void ProcessLocationList(string[] locationList)
    {
    }

    public virtual void ProcessItemList(string[] itemList)
    {
    }
}

public class DataCreator<T> : CsvTableRowCreator<T>
{
    public override T CreateRowData(string[] param) => (T)Activator.CreateInstance(typeof(T), [(DataArray)param])!;
}

public static class Extensions
{
    public static string[] SplitAndTrim(this string txt, char splitter)
        => txt.Split(splitter).Select(s => s.Trim()).ToArray();

    public static string[] SplitAndTrim(this string txt, string splitter)
        => txt.Split(splitter).Select(s => s.Trim()).ToArray();

    public static bool IsTrue(this string text) => text is not "" && text[0] is 't' or 'T' or 'y' or 'Y';

    public static string OptionFormat(
        this string text, string options = "options", string prefix = "", string suffix = ""
    ) => $"{options}.{prefix.LowerReplace()}{text.LowerReplace()}{suffix.LowerReplace()}";

    public static string LowerReplace(this string text) => text.ToLower().Replace(' ', '_');

    public static CsvFactory ReadTable<T>(this CsvFactory factory, CsvTableRowCreator<T> creator, out T[] table)
    {
        var fieldsCount = typeof(T).GetFields().Count(f => f.GetCustomAttributes<MarkAttribute>().Any());
        return factory.ReadTable(creator, fieldsCount, out table);
    }

    public static CsvFactory ReadTable<T>(this CsvFactory factory, out T[] table)
        => factory.ReadTable(new DataCreator<T>(), out table);
}

[AttributeUsage(AttributeTargets.Field)]
public class MarkAttribute : Attribute;