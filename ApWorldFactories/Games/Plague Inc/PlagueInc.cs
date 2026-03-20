using ApWorldFactories.Graphviz;
using CreepyUtil.Archipelago.WorldFactory;
using static CreepyUtil.Archipelago.WorldFactory.ItemFactory.ItemClassification;
using static CreepyUtil.Archipelago.WorldFactory.PremadePython;

namespace ApWorldFactories.Games.Plague_Inc;

public class PlagueInc : BuildData
{
    public override string SteamDirectory => FDrive;
    public override string ModFolderName => "SW_CreeperKing.Plaguepelago";
    public override string GameName => "Plague Inc";
    public override string ApWorldName => "plague_inc";
    public override string GoogleSheetId => "1VRvUwqMd4uWx3bVjrkY-fnQSKrj1e03jdUMcTmoNiPs";
    public override string WorldVersion => "0.1.0";
    public override string GameFolder => "PlagueInc";

    public override Dictionary<string, string> SheetGids { get; }
        = new() { ["techs"] = "894487465", ["data"] = "508764681", ["combos"] = "919603737" };

    private DiseaseData[] DiseaseData = [];
    private CountryData[] CountryData = [];
    private TechData[] TechData = [];
    private DifficultyData[] DifficultyData = [];
    private HexLayoutData[] HexLayoutData = [];

    private string[] TabNames = [];
    private Dictionary<string, string> Diseases = [];
    public Dictionary<int, int[]> hexAdjacency = [];
    private TechData.SingledOutTechData[] VictoryScores = [];
    private (string name, string disease, string diff, int score)[] DifficultyVictory = [];
    private readonly Dictionary<string, Dictionary<string, HexMap>> HexMap = [];

    public override void RunShenanigans()
    {
        GetSpreadsheet().ReadTable(out DiseaseData).SkipColumn()
                        .ReadTable(out CountryData);

        GetSpreadsheet("techs").ReadTable(out TechData);

        GetSpreadsheet("data")
           .ReadTable(out DifficultyData).SkipColumn()
           .ReadTable(out HexLayoutData);

        TabNames = TechData.Select(data => data.TechTreeType).Distinct().ToArray();
        Diseases = DiseaseData
                  .Where(data => data.Include && TechData.Any(data1 => data1.Diseases.Contains(data.Name)))
                  .ToDictionary(data => data.Name, data => data.Id);
        hexAdjacency = HexLayoutData.ToDictionary(data => data.Hex, data => data.AdjacentHexes);
        VictoryScores = Diseases.Keys
                                .SelectMany(disease => TechData
                                                      .Where(data => data.Diseases.Contains(disease))
                                                      .SelectMany(data => data.GetIndevTechs())
                                 ).DistinctBy(data => data.Name).ToArray();
        DifficultyVictory = Diseases.Keys.SelectMany(disease => DifficultyData.Select(diff
                => ($"Beat {disease} on {diff.Difficulty}", disease, diff.Difficulty, diff.VictoryScore)
            )
        ).ToArray();

        foreach (var singledOutTechData in VictoryScores)
        {
            var disease = singledOutTechData.Disease;
            var tab = singledOutTechData.Tab;
            var hex = singledOutTechData.Hex;

            if (!HexMap.TryGetValue(disease, out var map)) { HexMap[disease] = map = new Dictionary<string, HexMap>(); }

            if (!map.TryGetValue(tab, out var tabs)) { HexMap[disease][tab] = tabs = new HexMap(hexAdjacency.Count); }

            tabs.SetNode(hex, singledOutTechData.Id, hexAdjacency[hex]);
        }

        WriteData("diseases", DiseaseData.Select(data => $"{data.Name}:{data.Id}"));
        WriteData("techs", TechData.Select(data => data.Id));
    }

    public override void Options(WorldFactory _, OptionsFactory options_fact)
    {
        options_fact.ForEachOf(
            Diseases.Keys,
            (b, disease) => b.AddOption(
                disease, "Allow the disease as an item in the multiworld",
                disease is "Bacteria" ? new DefaultOnToggle() : new Toggle()
            )
        ).ForEachOf(
            DifficultyData,
            (b, data) => b.AddOption(
                $"{data.Difficulty} Difficulty", "Allow the difficulty as an item in the multiworld",
                data.Difficulty.ToLower()[0] is 'c' ? new DefaultOnToggle() : new Toggle()
            )
        ).AddCheckOptions(method =>
            {
                method
                   .AddCode(
                        new Variable(
                            "difs",
                            $"[item for item in [{string.Join(", ", DifficultyData.Select(data => data.Difficulty.OptionFormat(suffix: "_difficulty")))}] if item]"
                        )
                    )
                   .AddCode(
                        new Variable(
                            "diseases",
                            $"[item for item in [{string.Join(", ", Diseases.Keys.Select(disease => disease.OptionFormat()))}] if item]"
                        )
                    )
                   .AddCode(new Variable("dif_count", "len(difs)"))
                   .AddCode(new Variable("disease_count", "len(diseases)"))
                   .AddCode(
                        new IfFactory("dif_count < 1").AddCode(
                                                           "raise_yaml_error(world.player, \"You must have at least 1 difficulty enabled\")"
                                                       )
                                                      .SetElse(
                                                           new CodeBlockFactory().AddCode(
                                                               "world.starting_diff = random.choice(difs).display_name.replace(\" Difficulty\", \"\")"
                                                           )
                                                       )
                    ).AddCode(
                        new IfFactory("disease_count < 1").AddCode(
                            "raise_yaml_error(world.player, \"You must have at least 1 disease enabled\")"
                        ).SetElse(
                            new CodeBlockFactory()
                               .AddCode("world.starting_disease = random.choice(diseases).display_name")
                        )
                    ).AddCode("world.victories_needed = dif_count * disease_count");
            }
        );
    }

    public override void Locations(WorldFactory _, LocationFactory location_fact)
    {
        location_fact.ForEachOf(
            Diseases.Keys,
            (b, disease) => b.AddLocations(
                $"{disease.LowerReplace()}_techs",
                VictoryScores.Where(t => t.Disease == disease).Select(t => (string[])[t.Name, t.Disease]).Concat(
                    DifficultyVictory.Where(t => t.disease == disease).Select(t => (string[])[t.name, t.disease])
                )
            )
        );
    }

    public override void GenerateLocations(out string[] locationList, LocationFactory locationFactory)
    {
        locationFactory.GenerateLocationFile(
            out locationList,
            injectCode: factory1 => factory1.AddObject(
                new MappedVariable<string, string>(
                    "victory_scores",
                    VictoryScores.GroupBy(t => t.Disease).ToDictionary(
                        g => $"\"{g.Key}\"",
                        g => new MappedVariable<string, int>(
                            "",
                            g.GroupBy(t => t.Id).ToDictionary(g => $"\"{g.Key}\"", g => g.First().Score)
                        ).GetText(1)[4..]
                    )
                )
            )
        );
    }

    public override void Items(WorldFactory _, ItemFactory item_fact)
    {
        item_fact
           .AddItemListVariable(
                "always_tech", Progression,
                list: TechData.Where(data => data.RuleType is LogicRule.Always && data.TechTreeType is "Transmission")
                              .Select(data => data.Id)
                              .ToArray()
            )
           .AddItemListVariable(
                "tech_items", Progression,
                list: TechData
                     .Where(data => !(data.RuleType is LogicRule.Always && data.TechTreeType is "Transmission"))
                     .Select(data => data.Id)
                     .ToArray()
            )
           .AddItemListVariable(
                "difficulties", Progression, list: DifficultyData.Select(data => data.Difficulty).ToArray()
            )
           .AddItemListVariable("diseases", Progression, list: Diseases.Keys.ToArray())
           .AddItem("A Sickly Sensation", Filler)
           .AddCreateItems(method =>
                {
                    method.AddCode(CreateItemsFromList("tech_items"))
                          .ForEachOf(
                               DifficultyData,
                               data => method.AddCode(
                                   new IfFactory(
                                       $"{data.Difficulty.OptionFormat(suffix: "_difficulty")} and world.starting_diff != \"{data.Difficulty}\""
                                   ).AddCode(CreateItem(data.Difficulty))
                               )
                           ).ForEachOf(
                               Diseases.Keys,
                               disease => method.AddCode(
                                   new IfFactory(
                                       $"{disease.OptionFormat()} and world.starting_disease != \"{disease}\""
                                   ).AddCode(CreateItem(disease))
                               )
                           ).AddCode(CreateItemsFillRemainingWithItem("A Sickly Sensation"));
                }
            );
    }

    public override void Rules(WorldFactory _, RuleFactory rule_fact)
    {
        rule_fact
           .AddLogicFunction(
                "score", "has_score",
                "return sum([victory_scores[disease][item] for item, count in state.prog_items[player].items() if count > 0 and item in victory_scores[disease]]) >= target_score",
                "disease", "target_score"
            )
           .AddLogicRules(VictoryScores.ToDictionary(t => t.Name, t => t.GenRule(HexMap)))
           .AddLogicRules(
                DifficultyVictory.ToDictionary(
                    t => t.name,
                    t => $"has[\"{t.disease}\"] and has[\"{t.diff}\"] and score[\"{t.disease}\", {t.score}]"
                )
            );
    }

    public override void Regions(WorldFactory _, RegionFactory region_fact)
    {
        region_fact.AddRegions(Diseases.Keys.ToArray())
                   .ForEachOf(
                        Diseases.Keys,
                        (b, disease)
                            => b.AddConnectionCompiledRule(
                                     "Menu", disease, $"has[\"{disease}\"]", condition: disease.OptionFormat()
                                 ).AddLocationsFromList(
                                     $"{disease.LowerReplace()}_techs", condition: disease.OptionFormat()
                                 )
                                .AddEventLocations(
                                     disease.OptionFormat(),
                                     DifficultyVictory.Where(t => t.disease == disease).Select(t
                                         => new EventLocationData(t.disease, $"Event: {t.name}", "Victory", t.name)
                                     ).ToArray()
                                 )
                    );
    }

    public override void Init(WorldFactory _, WorldInitFactory init_fact)
    {
        init_fact
           .UseInitFunction(method => method.AddCode(new Variable("self.starting_diff", "\"\""))
                                            .AddCode(new Variable("self.starting_disease", "\"\""))
                                            .AddCode(new Variable("self.victories_needed", "0"))
            )
           .AddUseUniversalTrackerPassthrough(
                yamlNeeded: false,
                utBlock: factory1
                    => factory1.AddCode(CreateUtPassthrough("\"victories_needed\"", "victories_needed"))
            )
           .UseGenerateEarly(method
                => method.AddCode(CreatePushPrecollected("self.starting_diff", stringify: false)).AddCode(
                    CreatePushPrecollected("self.starting_disease", stringify: false)
                ).AddCode(
                    new ForLoopFactory("always_avail_tech", "always_tech").AddCode(
                        CreatePushPrecollected("always_avail_tech", stringify: false)
                    )
                )
            )
           .UseCreateRegions()
           .AddCreateItems()
           .UseSetRules(method => method
               .AddCode(CreateGoalCondition(StateHas("Victory", "self.victories_needed", returnValue: false)))
            )
           .UseFillSlotData(new Dictionary<string, string> { ["victories_needed"] = "int(self.victories_needed)" })
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
                   Diseases.Keys,
                   (b, disease) => b.AddConnection("Menu", disease, $"has[\"{disease}\"]").ForEachOf(
                       DifficultyVictory.Where(t => t.disease == disease),
                       t => b.AddLocation(
                           t.disease, new GraphLocation($"Event: {t.name}", getRule(t.name), true, "Victory")
                       )
                   )
               )
              .AddLocationsFromDoubleArray(locationDoubleArrays, getRule)
              .GenString();
    }
}