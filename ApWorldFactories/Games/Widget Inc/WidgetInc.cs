using ApWorldFactories.Graphviz;
using CreepyUtil.Archipelago.WorldFactory;
using static CreepyUtil.Archipelago.WorldFactory.PremadePython;
using Range = CreepyUtil.Archipelago.WorldFactory.Range;

namespace ApWorldFactories.Games.Widget_Inc;

public class WidgetInc : BuildData
{
    public override string SteamDirectory => FDrive;
    public override string ModFolderName => "SW_CreeperKing.Widgitpelago";
    public override string GameName => "Widget Inc";
    public override string ApWorldName => "widget_inc";
    public override string GoogleSheetId => "1IdcSvjcpVu7AlMY5EUUnbr9mLd9X6LaLiIemBAgD_gA";
    public override string WorldVersion => "0.1.2";
    public override string GameFolder => "WidgetInc";

    private TechTreeData[] TechTreeData = [];
    private ResourceData[] ResourceData = [];

    private Dictionary<string, string> FrameIdMap = [];
    private Dictionary<string, string> ResourceBuildingRequirement = [];
    private Dictionary<string, string[]> CraftingRecipes = [];
    private string[] CraftlessResources = [];
    private string[] EndingNodes = [];
    private Dictionary<int, string[]> TieredProducers = [];

    public override void RunShenanigans()
    {
        GetSpreadsheet("Main")
           .ReadTable(out TechTreeData).SkipColumn()
           .ReadTable(out ResourceData);

        FrameIdMap = TechTreeData.ToDictionary(data => data.Tech, data => data.Id);

        ResourceBuildingRequirement =
            TechTreeData.Where(data => data.Unlock != "")
                        .ToDictionary(data => data.Unlock, data => data.Tech);

        CraftingRecipes =
            ResourceData.ToDictionary(data => data.Resource, data => data.CraftingRequirements);

        CraftlessResources =
            ResourceBuildingRequirement.Keys.Except(CraftingRecipes.Keys).ToArray();

        EndingNodes = TechTreeData.Where(tech => TechTreeData.All(data => data.PreviousTech != tech.Tech))
                                  .Select(data => data.Tech).ToArray();

        TieredProducers =
            TechTreeData.Where(data => data.Unlock != "").GroupBy(data => data.TierRequirement)
                        .ToDictionary(g => g.Key, g => g.Select(data => data.Tech).ToArray());

        var scoutHints = TechTreeData.Where(data => data.Unlock is not "").GroupBy(data => data.TierRequirement)
                                     .OrderBy(g => g.Key)
                                     .Select(g => string.Join(", ", g.Select(data => data.Tech)));

        WriteData("scoutHints", scoutHints);
        WriteData("idMap", FrameIdMap.Select(kv => $"{kv.Key}:{kv.Value}"));
        
        WriteData("resources", ResourceBuildingRequirement.Select(kv => $"{kv.Key}:{kv.Value}"));
        WriteData("recipes", CraftingRecipes.Select(kv => $"{kv.Key}:{string.Join(',', kv.Value)}"));
        WriteData(
            "requireMap",
            TechTreeData.Where(data => data.ResourceRequirements.Length != 0)
                        .Select(data => $"{data.Id}:{string.Join(',', data.ResourceRequirements)}")
        );
    }

    public override void Options(WorldFactory _, OptionsFactory options_fact)
    {
        options_fact
           .AddOption("Production Multiplier", "Gives a production multiplier", new Range(4, 1, 10))
           .AddOption("Hand Crafting Multiplier", "Gives a multiplier to hand crafting", new Range(2, 1, 10))
           .AddOption("Starting Tier Producers", "Starts with upto X Tier producers", new Range(1, 0, 3))
           .AddCheckOptions();
    }

    public override void Locations(WorldFactory _, LocationFactory location_fact)
    {
        location_fact.AddLocations(
            "tech_tree",
            TechTreeData.Select(data => (string[])[data.Tech, $"Tier {data.TierRequirement}".Replace("Tier 0", "Menu")])
        );
    }

    public override void Items(WorldFactory _, ItemFactory item_fact)
    {
        item_fact
           .AddItem("Motivational Poster", ItemFactory.ItemClassification.Filler)
           .AddItemCountVariable(
                "progressive_tier", new Dictionary<string, int> { ["Progressive Tier"] = 12 },
                ItemFactory.ItemClassification.Progression
            )
           .AddItems(
                ItemFactory.ItemClassification.Filler,
                items: TechTreeData.Where(tech => EndingNodes.Contains(tech.Tech) && tech.Unlock is "")
                                   .Select(data => data.Tech)
                                   .Where(s => !s.StartsWith("Tier") || s.EndsWith("Mastery")).ToArray()
            )
           .AddItems(
                ItemFactory.ItemClassification.Progression,
                items: TechTreeData.Where(tech => !EndingNodes.Contains(tech.Tech) || tech.Unlock is not "")
                                   .Select(data => data.Tech)
                                   .Where(s => !s.StartsWith("Tier") || s.EndsWith("Mastery")).ToArray()
            )
           .AddCreateItems(method =>
                method
                   .AddCode(CreateItemsFromMapCountGenCode("progressive_tier"))
                   .AddCode(CreateItemsFromClassificationList())
                   .AddCode(CreateItemsFillRemainingWithItem("Motivational Poster"))
            );
    }

    public override void Rules(WorldFactory _, RuleFactory rule_fact)
    {
        rule_fact
           .AddCompoundLogicFunction("tier", "has_tier", "hasN['Progressive Tier', tier]", "tier")
           .AddCompoundLogicFunction("frame", "has_frame", "has[frame]", "frame")
           .ForEachOf(
                CraftlessResources, (b, s) => b.AddCompoundLogicFunction(
                    s.Replace(" ", ""),
                    s.ToLower().Replace(" ", "_"), $"frame['{ResourceBuildingRequirement[s]}']"
                )
            ).ForEachOf(
                CraftingRecipes, (b, pair) => b.AddCompoundLogicFunction(
                    pair.Key.Replace(" ", ""), pair.Key.ToLower().Replace(" ", "_"),
                    $"frame[\"{TechTreeData.First(data => data.Unlock == pair.Key).Tech}\"] and {string.Join(" and ", pair.Value.Select(s => s.Replace(" ", "")))}"
                )
            ).ForEachOf(
                TechTreeData, (b, data) =>
                {
                    List<string> rules = [];

                    if (data.PreviousTech is not "") rules.Add($"frame['{data.PreviousTech}']");
                    rules.AddRange(data.ResourceRequirements.Select(res => res.Replace(" ", "")));

                    b.AddLogicRule(data.Tech, string.Join(" and ", rules));
                }
            );
    }

    public override void Regions(WorldFactory _, RegionFactory region_fact)
    {
        region_fact.ForEachOf(
            Enumerable.Range(0, 12), (b, i) =>
            {
                b.AddRegion($"Tier {i + 1}");

                if (i == 0)
                {
                    b.AddConnectionCompiledRule("Menu", "Tier 1", "tier[1]");
                    return;
                }
                b.AddConnectionCompiledRule($"Tier {i}", $"Tier {i + 1}", $"tier[{i + 1}]");
            }
        ).AddLocationsFromList("tech_tree");
    }

    public override void Init(WorldFactory world_fact, WorldInitFactory init_fact)
    {
        init_fact
           .UseInitFunction()
           .UseGenerateEarly()
           .AddUseUniversalTrackerPassthrough(yamlNeeded: false)
           .UseGenerateEarly(method => method.AddCode(
                    new IfFactory("options.starting_tier_producers == 1")
                       .ForEachOf(TieredProducers[1], (ifFactory, s) => ifFactory.AddCode(CreatePushPrecollected(s)))
                       .SetElse(new CodeBlockFactory().AddCode(CreatePushPrecollected("Widget Factory")))
                )
            )
           .UseCreateRegions()
           .AddCreateItems()
           .UseSetRules(method => method
                                 .AddCode("player = self.player")
                                 .AddCode(CreateGoalCondition("RocketSegment", world_fact.GetRuleFactory()))
            )
           .UseFillSlotData(
                new Dictionary<string, string> { ["uuid"] = "str(shuffled)" },
                method => method.AddCode(CreateUniqueId())
            )
           .InjectCodeIntoWorld(world => world.AddVariable(new Variable("gen_puml", "False")))
           .UseGenerateOutput(method => method.AddCode(PumlGenCode()));
    }

    public override string GenerateGraphViz(
        WorldFactory worldFactory, Dictionary<string, string> associations, Func<string, string> getRule,
        string[][] locationDoubleArrays
    )
    {
        return new GraphBuilder(GameName)
              .ForEachOf(
                   Enumerable.Range(0, 12), (b, i) =>
                   {
                       if (i == 0)
                       {
                           b.AddConnection("Menu", "Tier 1", "tier[1]");
                           return;
                       }
                       b.AddConnection($"Tier {i}", $"Tier {i + 1}", $"tier[{i + 1}]");
                   }
               )
              .AddLocationsFromDoubleArray(locationDoubleArrays, getRule)
              .GenString();
    }
}