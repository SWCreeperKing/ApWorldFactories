using CreepyUtil.Archipelago.WorldFactory;
using RedefinedRpg;

namespace ApWorldFactories;

public abstract class BuildData
(
    int directory, string gameName, string modFolder, string apWorld, string csvName, string version,
    string apVersion = "0.6.5"
)
{
    public const int DDrive = 0;
    public const int FDrive = 1;
    public const string GitLink = "";

    public static readonly string[] Directories =
    [
        "D:/Programs/steam/steamapps/common",
        "F:/SteamLibrary/steamapps/common",
    ];

    public readonly string GameName = gameName;
    public readonly string GamePath = $"{Directories[directory]}/{gameName}";
    public readonly string ModDataPath = $"{Directories[directory]}/{gameName}/Mods/{modFolder}/Data";
    public readonly string ApWorldPath = $"E:/coding projects/python/Deathipelago/worlds/{apWorld}";
    public readonly string CsvPath = $"E:/coding projects/C#/ApWorldFactories/ApWorldFactories/Spreadsheets/{csvName}";

    public CsvParser GetSpreadsheet(int linesFromTop = 1, int linesFromLeft = 0)
        => new(CsvPath, linesFromTop, linesFromLeft);

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