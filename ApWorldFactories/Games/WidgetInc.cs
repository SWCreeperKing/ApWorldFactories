using CreepyUtil.Archipelago.WorldFactory;
using static CreepyUtil.Archipelago.WorldFactory.PremadePython;
using Range = CreepyUtil.Archipelago.WorldFactory.Range;

namespace ApWorldFactories.Games;

public class WidgetInc() : BuildData(
    FDrive, "Widget Inc", "SW_CreeperKing.Widgitpelago", "widget_inc", "1IdcSvjcpVu7AlMY5EUUnbr9mLd9X6LaLiIemBAgD_gA",
    "0.1.2", gameFolder: "WidgetInc"
)
{
    public override void RunShenanigans(WorldFactory factory)
    {
        GetSpreadsheet("Main")
           .ToFactory()
           .ReadTable(new TechTreeCreator(), 6, out var techTreeData).SkipColumn()
           .ReadTable(new ResourceCreator(), 2, out var resourceData);

        var frameIdMap = techTreeData.ToDictionary(data => data.Tech, data => data.Id);

        var resourceBuildingRequirement =
            techTreeData.Where(data => data.Unlock != "")
                        .ToDictionary(data => data.Unlock, data => data.Tech);

        var craftingRecipes =
            resourceData.ToDictionary(data => data.Resource, data => data.CraftingRequirements);

        var craftlessResources =
            resourceBuildingRequirement.Keys.Except(craftingRecipes.Keys).ToArray();

        var endingNodes = techTreeData.Where(tech => techTreeData.All(data => data.PreviousTech != tech.Tech))
                                      .Select(data => data.Tech).ToArray();

        var tieredProducers =
            techTreeData.Where(data => data.Unlock != "").GroupBy(data => data.TierRequirement)
                        .ToDictionary(g => g.Key, g => g.Select(data => data.Tech).ToArray());
        
        factory
           .GetOptionsFactory(GitLink)
           .AddOption("Production Multiplier", "Gives a production multiplier", new Range(4, 1, 10))
           .AddOption("Hand Crafting Multiplier", "Gives a multiplier to hand crafting", new Range(2, 1, 10))
           .AddOption("Starting Tier Producers", "Starts with upto X Tier producers", new Range(1, 0, 3))
           .AddCheckOptions()
           .GenerateOptionFile();

        factory
           .GetLocationFactory(GitLink)
           .AddLocations(
                "tech_tree",
                techTreeData
                   .Append(new TechTreeData(["Starting Check (1)", "", "", "", "", "0"]))
                   .Append(new TechTreeData(["Starting Check (2)", "", "", "", "", "0"]))
                   .Append(new TechTreeData(["Starting Check (3)", "", "", "", "", "0"]))
                   .Select(data => (string[])
                        [data.Tech, $"Tier {data.TierRequirement}".Replace("Tier 0", "Menu")]
                    )
            )
           .GenerateLocationFile();

        var ruleFactory =
            factory.GetRuleFactory(GitLink)
                        .AddLogicFunction(
                             "tier", "has_tier", StateHas("Progressive Tier", "tier"), "tier"
                         )
                        .AddLogicFunction("frame", "has_frame", StateHas("frame", stringify:false), "frame");

        craftlessResources.Aggregate(
            ruleFactory, (factory1, s) =>
                factory1.AddCompoundLogicFunction(
                    s.Replace(" ", ""),
                    s.ToLower().Replace(" ", "_"), $"frame['{resourceBuildingRequirement[s]}']"
                )
        );

        craftingRecipes.Aggregate(
            ruleFactory, (factory1, pair) =>
                factory1.AddCompoundLogicFunction(
                    pair.Key.Replace(" ", ""), pair.Key.ToLower().Replace(" ", "_"),
                    $"frame[\"{techTreeData.First(data => data.Unlock == pair.Key).Tech}\"] and {string.Join(" and ", pair.Value.Select(s => s.Replace(" ", "")))}"
                )
        );

        techTreeData.Aggregate(
            ruleFactory, (factory1, data) =>
            {
                List<string> rules = [];

                if (data.PreviousTech is not "") rules.Add($"frame['{data.PreviousTech}']");
                rules.AddRange(data.ResourceRequirements.Select(res => res.Replace(" ", "")));
                rules.Add($"tier[{data.TierRequirement}]");

                return factory1.AddLogicRule(data.Tech, string.Join(" and ", rules));
            }
        );

        ruleFactory.GenerateRulesFile();

        factory.GetItemFactory()
                    .AddItem("Motivational Poster", ItemFactory.ItemClassification.Filler)
                    .AddItemCountVariable(
                         "progressive_tier", new Dictionary<string, int> { ["Progressive Tier"] = 12 },
                         ItemFactory.ItemClassification.Progression
                     )
                    .AddItems(
                         ItemFactory.ItemClassification.Filler,
                         items: techTreeData.Where(tech => endingNodes.Contains(tech.Tech) && tech.Unlock is "")
                                            .Select(data => data.Tech)
                                            .Where(s => !s.StartsWith("Tier") || s.EndsWith("Mastery")).ToArray()
                     )
                    .AddItems(
                         ItemFactory.ItemClassification.Progression,
                         items: techTreeData.Where(tech => !endingNodes.Contains(tech.Tech) || tech.Unlock is not "")
                                            .Select(data => data.Tech)
                                            .Where(s => !s.StartsWith("Tier") || s.EndsWith("Mastery")).ToArray()
                     )
                    .AddCreateItems(method =>
                         method
                            .AddCode(CreateItemsFromMapCountGenCode("progressive_tier"))
                            .AddCode(CreateItemsFromClassificationList())
                            .AddCode(CreateItemsFillRemainingWithItem("Motivational Poster"))
                     )
                    .GenerateItemsFile();

        var regionFactory = factory
           .GetRegionFactory(GitLink);

        for (var i = 0; i < 12; i++)
        {
            regionFactory.AddRegion($"Tier {i + 1}");

            if (i == 0)
            {
                regionFactory.AddConnectionCompiledRule("Menu", "Tier 1", "tier[1]");
                continue;
            }
            regionFactory.AddConnectionCompiledRule($"Tier {i}", $"Tier {i + 1}", $"tier[{i + 1}]");
        }

        regionFactory
           .AddLocationsFromList("tech_tree")
           .GenerateRegionFile();

        factory
           .GetInitFactory(GitLink)
           .UseInitFunction()
           .UseGenerateEarly()
           .AddUseUniversalTrackerPassthrough(yamlNeeded: false)
           .UseGenerateEarly(method =>
                {
                    var firstProducers = new IfFactory("options.starting_tier_producers == 1");
                    tieredProducers[1].Aggregate(firstProducers, (ifFactory, s) => ifFactory.AddCode(CreatePushPrecollected(s)));
                    firstProducers.SetElse(new CodeBlockFactory().AddCode(CreatePushPrecollected("Widget Factory")));
                    
                    method.AddCode(firstProducers);
                }
            )
           .UseCreateRegions()
           .AddCreateItems()
           .UseSetRules(method => method
                                 .AddCode("player = self.player")
                                 .AddCode(CreateGoalCondition("RocketSegment", factory.GetRuleFactory()))
            )
           .UseFillSlotData(
                new Dictionary<string, string> { ["uuid"] = "str(shuffled)" },
                method => method.AddCode(CreateUniqueId())
            )
           .InjectCodeIntoWorld(world => world.AddVariable(new Variable("gen_puml", "False")))
           .UseGenerateOutput(method => method.AddCode(PumlGenCode()))
           .GenerateInitFile();

        var scoutHints = techTreeData.Where(data => data.Unlock is not "").GroupBy(data => data.TierRequirement)
                                     .OrderBy(g => g.Key)
                                     .Select(g => string.Join(", ", g.Select(data => data.Tech)));

        WriteData("scoutHints", scoutHints);
        WriteData("idMap", frameIdMap.Select(kv => $"{kv.Key}:{kv.Value}"));
    }
}

public readonly struct TechTreeData(string[] param)
{
    public readonly string Tech = param[0].Trim();
    public readonly string PreviousTech = param[1].Trim();
    public readonly string Id = param[2].Trim();

    public readonly string[] ResourceRequirements
        = param[3].Split(',').Select(s => s.Trim()).Where(s => s is not "").ToArray();

    public readonly string Unlock = param[4].Trim();
    public readonly int TierRequirement = int.Parse(param[5].Trim());
}

public readonly struct ResourceData(string[] param)
{
    public readonly string Resource = param[0].Trim();
    public readonly string[] CraftingRequirements = param[1].Split(',').Select(s => s.Trim()).ToArray();
}

public class TechTreeCreator : CsvTableRowCreator<TechTreeData>
{
    public override TechTreeData CreateRowData(string[] param) => new(param);
}

public class ResourceCreator : CsvTableRowCreator<ResourceData>
{
    public override ResourceData CreateRowData(string[] param) => new(param);
}