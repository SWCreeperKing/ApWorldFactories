using System.Diagnostics;
using System.Reflection;
using CreepyUtil.Archipelago.WorldFactory;
using CreepyUtil.ClrCnsl;
using CreepyUtil.Pos;

namespace ApWorldFactories;

public abstract class BuildData
{
    public const string MainDirectory = "../../../";
    public const string RawOutputPath = $"{MainDirectory}Output/";
    public const string RawInputPath = $"{MainDirectory}Input/";
    public const string DotCommandPrefix = "E:/Graphviz-14.1.3-win64/bin/dot.exe";
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
    public virtual string MainSheetGid => "0";

    public virtual string GamePath => SteamDirectory is "" ? "" : $"{SteamDirectory}/{GameName}";

    public virtual string WriteOutputDirectory => SteamDirectory is "" || ModFolderName is ""
        ? $"{RawOutputPath}{GameFolder}"
        : $"{SteamDirectory}/{GameFolder}/Mods/{ModFolderName}/Data";

    public virtual string ReadInputDirectory => $"{RawInputPath}{GameName}/";
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
        PrintProgress(pos, 0, amount, "Sheet(s) Downloaded");
        DownloadSheet(client, $"{MainSheetLink}&gid={MainSheetGid}", "main");
        PrintProgress(pos, 1, amount, "Sheet(s) Downloaded");
        var i = 2;
        foreach (var (name, gid) in SheetGids)
        {
            DownloadSheet(client, $"{MainSheetLink}&gid={gid}", name);
            PrintProgress(pos, i++, amount, "Sheet(s) Downloaded");
        }
    }

    private void DownloadSheet(HttpClient client, string url, string output)
    {
        var response = client.GetAsync(url).GetAwaiter().GetResult();
        var csvData = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
        File.WriteAllBytes($"{CsvPath}/{output}.csv", csvData);
    }

    private static void PrintProgress(Pos pos, int curr, int amount, string text)
    {
        var real = ClrCnsl.GetCursor();
        ClrCnsl.SetCursor(pos);
        ClrCnsl.ProgressBar(curr, amount, 20, d => d switch { < 1 => ConsoleColor.Cyan, >= 1 => ConsoleColor.Green });
        ClrCnsl.WriteLine($"\n{curr}/{amount} {text}");
        ClrCnsl.SetCursor(real);
    }

    public CsvFactory GetSpreadsheet(string sheet = "main", int linesFromTop = 1, int linesFromLeft = 0)
        => new CsvParser($"{CsvPath}/{sheet}.csv", linesFromTop, linesFromLeft).ToFactory();

    public void WriteData(string file, IEnumerable<string> data, string ext = "txt")
    {
        if (WriteOutputDirectory is "") return;
        ClrCnsl.WriteLine($"[#darkgray]Writing: [{WriteOutputDirectory}/{file}.{ext}]");
        File.WriteAllLines($"{WriteOutputDirectory}/{file}.{ext}", data);
    }

    public T[] ReadData<T>(string file, Func<string, T> iterAction, string ext = "txt")
        => ReadData(file, ext).Select(iterAction).ToArray();

    public string[] ReadData(string file, string ext = "txt") => File.ReadAllLines($"{ReadInputDirectory}{file}.{ext}");

    public void Run()
    {
        ClrCnsl.Clr();
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

        var cur = ClrCnsl.GetCursor();
        const int stepCount = 18;
        var i = 0;
        ClrCnsl.SetCursor(cur.X, cur.Y + 2);
        PrintProgress(cur, i++, stepCount, "Manipulating Data");
        RunShenanigans();

        PrintProgress(cur, i++, stepCount, "Generating Options");
        Options(builder, options_fact);
        PrintProgress(cur, i++, stepCount, "Generating Host Settings");
        HostSettings(builder, host_fact);
        PrintProgress(cur, i++, stepCount, "Generating Locations    ");
        Locations(builder, location_fact);
        PrintProgress(cur, i++, stepCount, "Generating Items    ");
        Items(builder, item_fact);
        PrintProgress(cur, i++, stepCount, "Generating Rules");
        Rules(builder, rule_fact);
        PrintProgress(cur, i++, stepCount, "Generating Regions");
        Regions(builder, region_fact);
        PrintProgress(cur, i++, stepCount, "Generating Init   ");
        Init(builder, init_fact);

        PrintProgress(cur, i++, stepCount, "Outputting Options");
        GenerateOptions(options_fact);
        PrintProgress(cur, i++, stepCount, "Outputting Host Settings");
        GenerateHostSettings(host_fact);
        PrintProgress(cur, i++, stepCount, "Outputting Locations    ");
        GenerateLocations(out var locationList, location_fact);
        PrintProgress(cur, i++, stepCount, "Outputting Items    ");
        GenerateItems(out var itemList, item_fact);
        PrintProgress(cur, i++, stepCount, "Outputting Rules");
        GenerateRules(rule_fact);
        PrintProgress(cur, i++, stepCount, "Outputting Regions");
        GenerateRegions(region_fact);
        PrintProgress(cur, i++, stepCount, "Outputting Init   ");
        GenerateInit(init_fact);

        PrintProgress(cur, i++, stepCount, "Writing Ap Json");
        GenerateJson(builder);

        PrintProgress(cur, i++, stepCount, "Processing Locations");
        ProcessLocationList(locationList);
        PrintProgress(cur, i++, stepCount, "Processing Items    ");
        ProcessItemList(itemList);
        PrintProgress(cur, i++, stepCount, "World Built!    ");

        ClrCnsl.WriteLine("Checking for Graph Gen");

        var associations = rule_fact.GetRuleMapAssociations();
        Func<string, string> getRule = s => associations.GetValueOrDefault(s, "");
        var doubleArr = location_fact.ReadLocationsDoubleArray().SelectMany(kv => kv.Value.Select(arr => arr).ToArray())
                                     .ToArray();

        var graph = GenerateGraphViz(builder, associations, getRule, doubleArr);

        if (graph is "")
        {
            ClrCnsl.WriteLine("No Graph Detected :(");
            return;
        }

        ClrCnsl.WriteLine("Writing Graph Data");
        if (!Directory.Exists($"{RawOutputPath}/Raw Graph Data"))
            Directory.CreateDirectory($"{RawOutputPath}/Raw Graph Data");
        if (!Directory.Exists($"{RawOutputPath}/Graph Output"))
            Directory.CreateDirectory($"{RawOutputPath}/Graph Output");

        File.WriteAllText($"{RawOutputPath}/Raw Graph Data/{GameName}.dot", graph);

        ClrCnsl.WriteLine("Generating Graph");

        var curDir = Directory.GetCurrentDirectory();
        var cmd1 = $"cd {curDir}";
        var cmd2
            = $"\"{DotCommandPrefix}\" -Tpng \"{RawOutputPath}Raw Graph Data/{GameName}.dot\" > \"{RawOutputPath}Graph Output/{GameName}.png\"";

        var graphProcess = new ProcessStartInfo
        {
            FileName = "cmd.exe", RedirectStandardOutput = true, RedirectStandardError = true,
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true
        };

        using var process = new Process();
        process.StartInfo = graphProcess;
        process.Start();

        using var sw = process.StandardInput;
        if (sw.BaseStream.CanWrite)
        {
            ClrCnsl.WriteLine($"Running: [#darkgray]{cmd1}");
            sw.WriteLine(cmd1);
            ClrCnsl.WriteLine($"Running: [#darkgray]{cmd2}");
            sw.WriteLine(cmd2);
            sw.WriteLine("exit");
        }

        var output = process.StandardOutput.ReadToEnd();
        if (output.Trim() is not "") ClrCnsl.WriteLine($"Output: [#darkgray]{output}");
        var error = process.StandardError.ReadToEnd();

        ClrCnsl.WriteLine(!string.IsNullOrEmpty(error) ? $"[#red]Error: {error}" : "Graph Generated");
    }

    public abstract void RunShenanigans();

    public virtual void Options(WorldFactory _, OptionsFactory options_fact) => options_fact.AddCheckOptions();

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

    public virtual string GenerateGraphViz(
        WorldFactory worldFactory, Dictionary<string, string> associations, Func<string, string> getRule,
        string[][] locationDoubleArrays
    ) => "";
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

    public static string OptionVariableFormat(
        this string text, string prefix = "", string suffix = ""
    ) => $"{prefix.LowerReplace()}{text.LowerReplace()}{suffix.LowerReplace()}";

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

    public static T ForEachOf<T, TV>(this T t, IEnumerable<TV> arr, Action<TV> action)
    {
        foreach (var v in arr) action(v);
        return t;
    }

    public static T ForEachOf<T, TV>(this T t, IEnumerable<TV> arr, Action<T, TV> action)
    {
        foreach (var v in arr) action(t, v);
        return t;
    }

    public static string AsStringifiedArray(
        this IEnumerable<string> arr, string surround = "\"", string separator = ", ", string leftEnd = "[",
        string rightEnd = "]"
    )
    {
        return $"{leftEnd}{string.Join(separator, arr.Select(s => s.Surround(surround)))}{rightEnd}";
    }
}

[AttributeUsage(AttributeTargets.Field)]
public class MarkAttribute : Attribute;