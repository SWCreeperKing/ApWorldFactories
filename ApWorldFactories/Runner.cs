using System.Reflection;
using ApWorldFactories;
using RedefinedRpg;
using static RedefinedRpg.ClrCnsl;
using static RedefinedRpg.Prompts;

var types = Assembly
           .GetEntryAssembly()!
           .GetTypes()
           .Where(t => t is { IsClass: true, IsAbstract: false } &&
                       t.IsSubclassOf(typeof(BuildData))
            )
           .Select(Activator.CreateInstance)
           .Cast<BuildData>()
           .OrderBy(t => t.GameName)
           .ToArray();

List<string> list = ["Exit", "Rebuild All"];
list.AddRange(types.Select(t => t.GameName));
var arr = list.ToArray();

var i = 0;
while (true)
{
    try
    {
        switch (i = ListViewPrompt("Select an option", i, arr))
        {
            case 0:
                return;
            case 1:
                foreach (var buildData in types) { buildData.Run(); }
                break;
            default:
                Clr();
                var world = types[i - 2]!;
                switch (ListViewPrompt(
                            $"What to do with [{list[i]}]?", options: ["Go Back", "Build World", "Download Sheets", "Download And Build"]
                        ))
                {
                    case 0:
                        break;
                    case 1:
                        WriteLine("Building World");
                        world.Run();
                        WriteLine("World Built");
                        WaitForInput();
                        break;
                    case 2:
                        WriteLine("Downloading Sheet(s)");
                        world.DownloadSheets();
                        WriteLine("Sheet(s) Downloaded");
                        WaitForInput();
                        break;
                    case 3:
                        WriteLine("Downloading Sheet(s)");
                        world.DownloadSheets();
                        WriteLine("Sheet(s) Downloaded");
                        WriteLine("Building World");
                        world.Run();
                        WriteLine("World Built");
                        WaitForInput();
                        break;
                }
                break;
        }
    }
    catch (Exception e)
    {
        WriteLine($"[#red]Error: \n{e}");
        WaitForInput();
    }
    Clr();
}