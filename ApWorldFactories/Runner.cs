using System.Reflection;
using ApWorldFactories;
using RedefinedRpg;

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

while (true)
{
    types[Prompts.ListViewPrompt("Which ApWorld to build?", options: types.Select(t => t.GameName).ToArray())].Run();
}