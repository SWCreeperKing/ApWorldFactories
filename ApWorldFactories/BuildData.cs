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
    public readonly string ModDataPath = $"{Directories[directory]}/{(gameFolder is "" ? gameName : gameFolder)}/Mods/{modFolder}/Data";
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
        RunShenanigans(builder);
        builder.GenerateArchipelagoJson(apVersion, version, "SW_CreeperKing");
    }

    public abstract void RunShenanigans(WorldFactory factory);
}