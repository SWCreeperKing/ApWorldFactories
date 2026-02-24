using CreepyUtil.Archipelago.WorldFactory;
using RedefinedRpg;

namespace ApWorldFactories;

public abstract class BuildData
(
    int directory, string gameName, string modFolder, string apWorld, string sheetId, string version,
    string apVersion = "0.6.5", string gameFolder = ""
)
{
    public const int DDrive = 0;
    public const int FDrive = 1;

    public const string GitLink
        = "https://github.com/SWCreeperKing/ApWorldFactories/tree/master/ApWorldFactories/Games";

    public static readonly string[] Directories =
    [
        "D:/Programs/steam/steamapps/common",
        "F:/SteamLibrary/steamapps/common",
    ];

    public readonly string GameName = gameName;
    public readonly string GamePath = $"{Directories[directory]}/{(gameFolder is "" ? gameName : gameFolder)}";

    public readonly string ModDataPath
        = $"{Directories[directory]}/{(gameFolder is "" ? gameName : gameFolder)}/Mods/{modFolder}/Data";

    public readonly string ApWorldPath = $"E:/coding projects/python/Deathipelago/worlds/{apWorld}";
    public readonly string CsvPath = $"E:/coding projects/C#/ApWorldFactories/ApWorldFactories/Spreadsheets/{gameName}";
    public readonly string MainSheetLink = $"https://docs.google.com/spreadsheets/d/{sheetId}/export?format=csv";

    public virtual Dictionary<string, string> SheetGids { get; } = [];

    public void DownloadSheets()
    {
        if (!Directory.Exists(CsvPath)) Directory.CreateDirectory(CsvPath);
        using var client = new HttpClient();
        DownloadSheet(client, MainSheetLink, "main");
        if (SheetGids is null || SheetGids.Count == 0) return;
        foreach (var (name, gid) in SheetGids) { DownloadSheet(client, $"{MainSheetLink}&gid={gid}", name); }
    }

    private void DownloadSheet(HttpClient client, string url, string output)
    {
        var response = client.GetAsync(url).GetAwaiter().GetResult();
        var csvData = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
        File.WriteAllBytes($"{CsvPath}/{output}.csv", csvData);
    }

    public CsvParser GetSpreadsheet(string sheet, int linesFromTop = 1, int linesFromLeft = 0)
        => new($"{CsvPath}/{sheet}.csv", linesFromTop, linesFromLeft);

    public void WriteData(string file, IEnumerable<string> data)
        => File.WriteAllLines($"{ModDataPath}/{file}.txt", data);

    public void Run()
    {
        var builder = new WorldFactory(GameName)
                     .SetOnCompilerError((e, s) => ClrCnsl.WriteLine($"[#red]Error: [{s}]\n{e}"))
                     .SetOutputDirectory(ApWorldPath);
        if (!Directory.Exists(ModDataPath)) Directory.CreateDirectory(ModDataPath);
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
        GenerateLocations(location_fact);
        GenerateItems(item_fact);
        GenerateRules(rule_fact);
        GenerateRegions(region_fact);
        GenerateInit(init_fact);
        
        GenerateJson(builder);
    }

    public abstract void RunShenanigans();

    public abstract void Options(WorldFactory _, OptionsFactory options_fact);
    public virtual void HostSettings(WorldFactory _, HostSettingsFactory host_fact) {}
    public abstract void Locations(WorldFactory _, LocationFactory location_fact);
    public abstract void Items(WorldFactory _, ItemFactory item_fact);
    public abstract void Rules(WorldFactory _, RuleFactory rule_fact);
    public abstract void Regions(WorldFactory _, RegionFactory region_fact);
    public abstract void Init(WorldFactory _, WorldInitFactory init_fact);
    
    
    public virtual void GenerateOptions(OptionsFactory optionsFactory) => optionsFactory.GenerateOptionFile();
    public virtual void GenerateHostSettings(HostSettingsFactory hostSettingsFactory) => hostSettingsFactory.GenerateHostSettingsFile();
    public virtual void GenerateLocations(LocationFactory locationFactory) => locationFactory.GenerateLocationFile();
    public virtual void GenerateItems(ItemFactory itemFactory) => itemFactory.GenerateItemsFile();
    public virtual void GenerateRules(RuleFactory ruleFactory) => ruleFactory.GenerateRulesFile();
    public virtual void GenerateRegions(RegionFactory regionFactory) => regionFactory.GenerateRegionFile();
    public virtual void GenerateInit(WorldInitFactory initFactory) => initFactory.GenerateInitFile();
    public virtual void GenerateJson(WorldFactory worldFactory) => worldFactory.GenerateArchipelagoJson(apVersion, version, "SW_CreeperKing");
}

public class DataCreator<T> : CsvTableRowCreator<T>
{
    public override T CreateRowData(string[] param) => (T)Activator.CreateInstance(typeof(T), [param])!;
}

public static class Extensions
{
    public static string[] SplitAndTrim(this string txt, char splitter)
        => txt.Split(splitter).Select(s => s.Trim()).ToArray();

    public static string[] SplitAndTrim(this string txt, string splitter)
        => txt.Split(splitter).Select(s => s.Trim()).ToArray();

    public static bool IsTrue(this string text) => text[0] is 't' or 'T' or 'y' or 'Y';

    public static string OptionFormat(
        this string text, string options = "options", string prefix = "", string suffix = ""
    ) => $"{options}.{prefix.LowerReplace()}{text.LowerReplace()}{suffix.LowerReplace()}";

    public static string LowerReplace(this string text) => text.ToLower().Replace(' ', '_');
}