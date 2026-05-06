using System.Text;

namespace ApWorldFactories.Graphviz;

public class GraphBuilder(string gameName)
{
    private const string Font = "monocraft";
    private string GameName = gameName;
    private Dictionary<string, GraphRegion> Regions = new() { ["Menu"] = new GraphRegion("Menu") };

    public GraphBuilder AddRegion(string region)
    {
        Regions[region] = new GraphRegion(region);
        return this;
    }

    public GraphBuilder AddRegions(params string[] regions)
        => regions.Aggregate(this, (builder, s) => builder.AddRegion(s));

    public GraphBuilder AddConnection(string from, string to, string rules = "")
    {
        if (!Regions.ContainsKey(from)) AddRegion(from);
        if (!Regions.ContainsKey(to)) AddRegion(to);
        Regions[from].Connections.Add(new GraphConnection(from, to, rules));
        return this;
    }

    public GraphBuilder AddLocationsFromDoubleArray(string[][] doubleArray, Func<string, string> getRule)
    {
        return this.ForEachOf(doubleArray, (b, strings) => b.AddLocation(strings[1], getRule, strings[0]));
    }
    
    public GraphBuilder AddLocation(string region, GraphLocation location)
    {
        if (!Regions.ContainsKey(region)) AddRegion(region);
        Regions[region].Locations.Add(location);
        return this;
    }

    public GraphBuilder AddLocation(
        string region, Func<string, string> getRule, string location, bool isEvent = false, string placedItem = ""
    )
    {
        if (!Regions.ContainsKey(region)) AddRegion(region);
        Regions[region].Locations.Add(new GraphLocation(location, getRule(location), isEvent, placedItem));
        return this;
    }

    public GraphBuilder AddEventLocation(
        string region, Func<string, string> getRule, string eventLocation, string ruleLocation, string placedItem = ""
    )
    {
        if (!Regions.ContainsKey(region)) AddRegion(region);
        Regions[region].Locations.Add(new GraphLocation(eventLocation, getRule(ruleLocation), true, placedItem));
        return this;
    }

    public GraphBuilder AddLocations(string region, params GraphLocation[] locations)
    {
        if (!Regions.ContainsKey(region)) AddRegion(region);
        Regions[region].Locations.AddRange(locations);
        return this;
    }

    public GraphBuilder AddLocations(string region, Func<string, string> getRule, params string[] locations)
    {
        if (!Regions.ContainsKey(region)) AddRegion(region);
        Regions[region].Locations.AddRange(locations.Select(loc => new GraphLocation(loc, getRule(loc))));
        return this;
    }

    public string GenString()
    {
        StringBuilder sb = new();

        sb.Append($"digraph {GameName.Replace(" ", "_")} {{\nconcentrate=True\nrankdir=TB;\nnode [shape=none]\ngraph [fontname = \"{Font}\"];\nnode [fontname = \"{Font}\"];\nedge [fontname = \"{Font}\"];");

        var regions = Regions.Values.ToArray();
        foreach (var graphRegion in regions) sb.Append(graphRegion.GenNode()).Append('\n');
        foreach (var graphRegion in regions) sb.Append(graphRegion.GenConnections()).Append('\n');

        sb.Append("\n}");

        return sb.ToString().Replace('&', '+');
    }
}

public class GraphRegion(string name)
{
    public readonly string Name = name;
    public List<GraphConnection> Connections = [];
    public List<GraphLocation> Locations = [];

    public string GenNode()
    {
        StringBuilder sb = new();

        sb.Append(
               "<table border=\"2\" cellborder=\"1\" cellspacing=\"0\" cellpadding=\"4\"><tr> <td colspan = \"3\"><b>\""
           )
          .Append(Name)
          .Append("\"</b></td></tr>");

        if (Locations.Count != 0)
        {
            sb.Append("<tr><td><b>Location</b></td><td><b>Logic</b></td><td><b>Item</b></td></tr>");
            foreach (var graphLocation in Locations.OrderBy(loc => loc.IsEvent)) sb.Append(graphLocation.GenLocation());
        }


        sb.Append("</table>");
        return $"\"{Name}\" [label=<{sb}>]";
    }

    public string GenConnections() => string.Join("\n", Connections.Select(con => con.GenConnection()));
}

public readonly struct GraphConnection(string from, string to, string rules = "")
{
    public readonly string From = from;
    public readonly string To = to;
    public readonly string Rules = rules;

    public string GenConnection()
        => $"\"{From}\" -> \"{To}\"{(Rules is not "" ? $" [label=\"{Rules.Replace('"', '\'')}\" color=\"#00008844\"]" : "")}";
}

public readonly struct GraphLocation
    (string name, string logic = "", bool isEventLocation = false, string placedItem = "")
{
    public readonly string LocationName = name;
    public readonly string Logic = logic.Trim() is "" ? "N/A" : logic;
    public readonly bool IsEvent = isEventLocation;
    public readonly string PlacedItem = placedItem.Trim() is "" ? "Random" : placedItem;

    public string GenLocation()
    {
        return
            $"<tr><td align=\"left\">{(IsEvent ? "🔒" : "")}\"{LocationName}\"</td><td>\"{Logic.Replace('"', '\'')}\"</td><td>\"{PlacedItem}\"</td></tr>";
    }
}