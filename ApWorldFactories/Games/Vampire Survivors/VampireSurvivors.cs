using System.Text;
using ApWorldFactories.Extended;
using ApWorldFactories.Games.Slime_Rancher;
using ApWorldFactories.Graphviz;
using CreepyUtil.Archipelago.WorldFactory;
using CreepyUtil.ClrCnsl;
using static ApWorldFactories.Games.Vampire_Survivors.CodeBank;
using static CreepyUtil.Archipelago.WorldFactory.PremadePython;
using Range = CreepyUtil.Archipelago.WorldFactory.Range;

namespace ApWorldFactories.Games.Vampire_Survivors;

public class VampireSurvivors : BuildData
{
    public override string SteamDirectory => FDrive;
    public override string ModFolderName => "SW_CreeperKing.ArchipelagoSurvivors";
    public override string GameName => "Vampire Survivors";
    public override string ApWorldName => "vampire_survivors";
    public override string GoogleSheetId => "1lXIr2a5fa7rdQ9fe5efA7BOAGaO1ko-pNSSO62mqlCM";
    public override string WorldVersion => "0.3.2";
    public override string MainSheetGid => "954348750";

    public override Dictionary<string, string> SheetGids => new()
    {
        ["stage"] = "422810729", ["stageEnemies"] = "890158057", ["stageBosses"] = "1899232716",
        ["enemies"] = "1716501525", ["characters"] = "236202290", ["charClass"] = "1894016824",
        ["stageClass"] = "1140982719",
    };

    public StageData[] StageData = [];
    public StageEnemyData[] StageEnemyData = [];
    public StageBossData[] StageBossData = [];
    public EnemyData[] EnemyData = [];
    public CharacterData[] CharacterData = [];
    public CharacterClassificationData[] CharacterClassificationData = [];
    public StageClassificationData[] StageClassificationData = [];
    // public EnemyBlacklistData[] EnemyBlacklistData = [];

    public Dictionary<string, string> StageNameMap = [];
    public Dictionary<string, string> EnemyVariantMap = [];
    public Dictionary<string, string> EnemyNameMap = [];
    public Dictionary<string, string[]> EnemyMap = [];
    public Dictionary<string, string[]> EnemyHurryMap = [];
    public List<string> EnemiesRequireArcana = [];

    public override void RunShenanigans()
    {
        GetSpreadsheet()
           .ReadTable(out StageData[] overrideStageData).SkipColumn()
           .ReadTable(out StageEnemyData[] overrideStageEnemyData).SkipColumn()
           .ReadTable(out StageBossData[] overrideStageBossData).SkipColumn()
           .ReadTable(out EnemyData[] overrideEnemyData).SkipColumn()
           .ReadTable(out CharacterData[] overrideCharacterData).SkipColumn()
            // .ReadTable(out EnemyBlacklistData)
            ;

        GetSpreadsheet("stage").ReadTable(out StageData);
        GetSpreadsheet("stageEnemies").ReadTable(out StageEnemyData);
        GetSpreadsheet("stageBosses").ReadTable(out StageBossData);
        GetSpreadsheet("enemies").ReadTable(out EnemyData);
        GetSpreadsheet("characters").ReadTable(out CharacterData);
        GetSpreadsheet("charClass").ReadTable(out CharacterClassificationData);
        GetSpreadsheet("stageClass").ReadTable(out StageClassificationData);

        StageData = StageData.Where(data => !overrideStageData.Any(data.IsMatch)).Concat(overrideStageData)
                             .Where(data => data.IsAcceptable()).ToArray();

        StageClassificationData = StageClassificationData
                                 .Where(data => StageData.Any(stage => stage.GetName() == data.Name)).ToArray();

        StageEnemyData = StageEnemyData.Where(data => !overrideStageEnemyData.Any(data.IsMatch))
                                       .Where(data => StageData.Any(stage => stage.StageId == data.StageId))
                                       .Concat(overrideStageEnemyData)
                                       .ToArray();

        StageEnemyData = StageEnemyData
                        .Concat(StageEnemyData
                               .Where(data => data.StageId is "FOREST" or "LIBRARY" or "WAREHOUSE" or "TOWER"
                                    or "CHAPEL")
                               .Select(data => new StageEnemyData(data.EnemyId, "GREENACRES", data.Minute))).ToArray();

        StageBossData = StageBossData.Where(data => !overrideStageBossData.Any(data.IsMatch))
                                     .Where(data => StageData.Any(stage => stage.StageId == data.StageId))
                                     .Concat(overrideStageBossData).ToArray();

        StageBossData = StageBossData
                       .Concat(StageBossData
                              .Where(data => data.StageId is "FOREST" or "LIBRARY" or "WAREHOUSE" or "TOWER"
                                   or "CHAPEL")
                              .Select(data => new StageBossData(data.BossId, "GREENACRES", data.Minute))).ToArray();

        EnemyData = EnemyData.Where(data => !overrideEnemyData.Any(data.IsMatch)).Concat(overrideEnemyData)
                             .Where(data => data.IsAcceptable()
                                  // && !EnemyBlacklistData.Contains(data.EnemyId)
                              )
                             .ToArray();

        CharacterData = CharacterData.Where(data => !overrideCharacterData.Any(data.IsMatch))
                                     .Concat(overrideCharacterData).Where(data => data.IsAcceptable()).ToArray();

        StageNameMap = StageData.ToDictionary(data => data.StageId, data => data.StageName);

        EnemyNameMap = EnemyData.Where(data => data.Name is not "" && data.BestiaryInclude)
                                .ToDictionary(data => data.EnemyId, data => data.Name);

        EnemyVariantMap = EnemyData
                         .Where(data => data.Variants.Length != 0 && data.Variants[0] is not "" && data.BestiaryInclude)
                         .SelectMany(data => data.Variants.Select(v => (v, data.EnemyId)))
                         .ToDictionary(t => t.v, t => t.EnemyId);

        var rawEnemyMap = StageEnemyData
                         .Select(data => (data.EnemyId, data.StageId, data.Minute))
                         .Concat(StageBossData.Select(data => (data.BossId, data.StageId, data.Minute)))
                         .Select(t => (EnemyVariantMap.TryGetValue(t.Item1, out var value) ? value : t.Item1, t.StageId,
                              t.Minute))
                         .Where(t => EnemyNameMap.ContainsKey(t.Item1) && StageNameMap.ContainsKey(t.StageId))
                         .Select(t => (EnemyNameMap[t.Item1], StageNameMap[t.StageId], t.Minute)).ToArray();

        var rawEnemyMapGrouping = rawEnemyMap.GroupBy(t => t.Item1).ToArray();

        EnemyMap = rawEnemyMapGrouping.ToDictionary(g => g.Key, g => g.Select(t => t.Item2).Distinct().ToArray());
        EnemyHurryMap = rawEnemyMap
                       .Where(t => t.Minute >= 15
                                   && !rawEnemyMapGrouping
                                      .First(g => g.Key == t.Item1)
                                      .Any(subT => subT.Item2 == t.Item2 && subT.Minute < 15)).GroupBy(t => t.Item1)
                       .ToDictionary(g => g.Key,
                            g => g.Select(t => t.Item2).Distinct().ToArray());

        EnemiesRequireArcana.Clear();
        foreach (var enemy in EnemyData.Where(data => data.NeedArcana))
        {
            EnemiesRequireArcana.Add(enemy.Name);
            EnemyMap[enemy.Name] = StageNameMap.Values.ToArray();
        }

        // MergeLegacyData.MergeData(this);

        WriteData("EnemyData",
            EnemyData.Where(data => data.Name is not "" && data.BestiaryInclude)
                     .Select(data => $"{data.Name}:{data.EnemyId}"));
        WriteData("StageData", StageData.Select(data => $"{data.StageName}:{data.StageId}"));
        WriteData("EnemyVariantMap",
            EnemyVariantMap.Where(kv => kv.Key != kv.Value).Select(kv => $"{kv.Key}:{kv.Value}"));
        WriteData("CharData",
            CharacterData.Where(data => data.Name is not "").Select(data => $"{data.Name}:{data.CharacterId}"));
        WriteData("EnemyVariants",
            EnemyData.Where(data => data.Variants.Length != 0 && data.Variants[0] is not "")
                     .Select(data => $"{data.EnemyId}:{string.Join('|', data.Variants)}"));
        WriteData("EnemyMap", EnemyMap.Select(kv => $"{kv.Key}:{string.Join('|', kv.Value)}"));
        WriteData("EnemyHurryMap", EnemyHurryMap.Select(kv => $"{kv.Key}:{string.Join('|', kv.Value)}"));
        WriteData("ArcanaEnemyList", EnemyData.Where(data => data.NeedArcana).Select(data => data.EnemyId));
    }

    public override void HostSettings(WorldFactory _, HostSettingsFactory host_fact)
    {
        host_fact.AddSetting(new Bool("Allow Unfair Characters", "Allow the use of unfair characters", false));
    }

    public override void Options(WorldFactory _, OptionsFactory options_fact)
    {
        options_fact.AddOption("Goal Requirement",
                         """
                         0 = Stage hunt (beat/loop all stages)
                         1 = Kill Director (required ~75% of stages beaten)
                         """, new Choice(0, "stage_hunt", "director")).AddOption("Chest Checks Per Stage",
                         """
                         how many chest checks per stage
                         from 5 to 10
                         default: 7
                         """, new Range(7, 5, 10)).AddOption("Egg Inclusion",
                         """
                         how to include eggs:
                         0 = fully disabled
                         1 = locked behind an item
                         2 = fully unlocked
                         """, new Choice(1, "disabled", "locked_behind_item", "unlocked")).AddOption(
                         "Lock Hyper Behind Item", "Lock the `Hyper` gamemode behind an item", new Toggle())
                    .AddOption("Lock Hurry Behind Item",
                         """
                         Lock the `Hurry` gamemode behind an item
                         This will lock `Beat with [character]` and `[stage] beaten` checks until hurry is found
                         """, new Toggle())
                    .AddOption("Lock Arcanas Behind Item", "Lock the `Arcanas` gamemode behind an item", new Toggle())
                    .AddOption("Enemysanity", "The first kill of an enemy is a check", new Toggle())
                    .AddOption("Enemysanity Arcana Enemies",
                         "Allows Arcana specific enemies to be checks for enemysanity", new Toggle())
                    .AddOption("Character Pool Size",
                         "Limit the amount of characters (select a random assortment of characters if higher)",
                         new Range(0, 0, 999))
                    .AddOption("Stage Pool Size",
                         "Limit the amount of stages (select a random assortment of stages if higher)",
                         new Range(0, 0, 999));

        List<string> includedChars = [];
        List<string> includedStages = [];
        AllowClassifiers("Secret Characters", CharacterClassificationData, 1, true);
        AllowClassifiers("Megalo Characters", CharacterClassificationData, 2);
        AllowClassifiers("Unfair Characters", CharacterClassificationData, 3);
        AddDlcOptionsSet("Base Characters", VsDlc.None, CharacterData, true);
        AddDlcOptionsSet("Moonspell Characters", VsDlc.Moonspell, CharacterData, false);
        AddDlcOptionsSet("Foscari Characters", VsDlc.Foscari, CharacterData, false);
        AddDlcOptionsSet("Amongus Characters", VsDlc.Amongus, CharacterData, false);
        AddDlcOptionsSet("Operation Guns Characters", VsDlc.Guns, CharacterData, false);
        AddDlcOptionsSet("Castlevania Characters", VsDlc.Castlevania, CharacterData, false);
        AddDlcOptionsSet("Emerald Characters", VsDlc.Emerald, CharacterData, false);
        AddDlcOptionsSet("Balatro Characters", VsDlc.Balatro, CharacterData, false);

        AddClassifierOptionsSet("Normal Stages", 0, StageClassificationData, true);
        AddClassifierOptionsSet("Bonus Stages", 1, StageClassificationData, false);
        AddClassifierOptionsSet("Challenge Stages", 2, StageClassificationData, false);
        AddDlcOptionsSet("Moonspell Stages", VsDlc.Moonspell, StageData, false);
        AddDlcOptionsSet("Foscari Stages", VsDlc.Foscari, StageData, false);
        AddDlcOptionsSet("Amongus Stages", VsDlc.Amongus, StageData, false);
        AddDlcOptionsSet("Operation Guns Stages", VsDlc.Guns, StageData, false);
        AddDlcOptionsSet("Castlevania Stages", VsDlc.Castlevania, StageData, false);
        AddDlcOptionsSet("Emerald Stages", VsDlc.Emerald, StageData, false);
        AddDlcOptionsSet("Balatro Stages", VsDlc.Balatro, StageData, false);

        options_fact
           .InjectCodeIntoOptionsClass(py =>
                py
                   .AddMethod(new MethodFactory("flatten_locations")
                             .AddParams("self", "world", "list", "self_list")
                             .AddCode(new IfFactory("\"Random\" in self_list")
                                     .AddCode(
                                          "return world.random.sample(list, world.random.randint(int(len(list) / 2), len(list)))")
                                     .SetElse(new CodeBlockFactory().AddCode(
                                          "return list if \"All\" in self_list else [loc for loc in self_list if loc != \"Random\"]"))))
                   .AddMethod(new MethodFactory("get_included_characters")
                             .AddParams("self", "world")
                             .AddCode(
                                  $"return ({string.Join(" + ", includedChars.Select(charName => $"self.flatten_locations(world, {charName}, self.included_{charName})"))})"))
                   .AddMethod(new MethodFactory("get_included_stages")
                             .AddParams("self", "world")
                             .AddCode(
                                  $"return ({string.Join(" + ", includedStages.Select(stage => $"self.flatten_locations(world, {stage}, self.included_{stage})"))})")))
           .AddCheckOptions(method => method.AddCode(CheckOptions));

        return;

        void AllowClassifiers<T>(string name, T[] array, int type, bool defToggle = false)
            where T : IClassifier, IGetName
        {
            options_fact.AddOption($"Allow {name}",
                $"""
                 Allow the use of {name.ToLower()}:
                 [{string.Join(", ", array.OfClassifier(type).GetNames())}]
                 """, defToggle ? new DefaultOnToggle() : new Toggle());
        }

        void AddClassifierOptionsSet<T>(string included, int selector, T[] array, bool useAll) where T : IClassifier, IGetName
            => AddVariableOptionsSet(included, array.OfClassifier(selector), useAll);

        void AddDlcOptionsSet<T>(string included, VsDlc dlc, T[] array, bool useAll) where T : IGetDlc, IGetName
            => AddVariableOptionsSet(included, array.OfDlc(dlc), useAll);

        void AddVariableOptionsSet<T>(string included, T[] array, bool useAll) where T : IGetName
        {
            options_fact
               .AddOption($"Included {included}",
                    $"""
                     {included} to be randomized
                     possible options:
                     [{string.Join(", ", array.GetNames())}]
                     "All" - adds all locations above
                     "Random" - picks a random # of characters b/t list's max size / 2 and list's max size
                     """, new OptionSet(useAll ? "\"All\"" : "[]", $"{included.OptionVariableFormat()} + [\"All\", \"Random\"]"));
            (included.EndsWith("Stages") ? includedStages : includedChars).Add(included.OptionVariableFormat());
        }
    }

    public override void Locations(WorldFactory _, LocationFactory location_fact)
    {
        List<string> includedChars = [];
        List<string> includedStages = [];
        AllowClassifiers("Secret Characters", CharacterClassificationData, 1);
        AllowClassifiers("Megalo Characters", CharacterClassificationData, 2);
        AllowClassifiers("Unfair Characters", CharacterClassificationData, 3);
        AddDlcOptionsSet("Base Characters", VsDlc.None, CharacterData);
        AddDlcOptionsSet("Moonspell Characters", VsDlc.Moonspell, CharacterData);
        AddDlcOptionsSet("Foscari Characters", VsDlc.Foscari, CharacterData);
        AddDlcOptionsSet("Amongus Characters", VsDlc.Amongus, CharacterData);
        AddDlcOptionsSet("Operation Guns Characters", VsDlc.Guns, CharacterData);
        AddDlcOptionsSet("Castlevania Characters", VsDlc.Castlevania, CharacterData);
        AddDlcOptionsSet("Emerald Characters", VsDlc.Emerald, CharacterData);
        AddDlcOptionsSet("Balatro Characters", VsDlc.Balatro, CharacterData);

        AddClassifierOptionsSet("Normal Stages", 0, StageClassificationData);
        AddClassifierOptionsSet("Bonus Stages", 1, StageClassificationData);
        AddClassifierOptionsSet("Challenge Stages", 2, StageClassificationData);
        AddDlcOptionsSet("Moonspell Stages", VsDlc.Moonspell, StageData);
        AddDlcOptionsSet("Foscari Stages", VsDlc.Foscari, StageData);
        AddDlcOptionsSet("Amongus Stages", VsDlc.Amongus, StageData);
        AddDlcOptionsSet("Operation Guns Stages", VsDlc.Guns, StageData);
        AddDlcOptionsSet("Castlevania Stages", VsDlc.Castlevania, StageData);
        AddDlcOptionsSet("Emerald Stages", VsDlc.Emerald, StageData);
        AddDlcOptionsSet("Balatro Stages", VsDlc.Balatro, StageData);

        location_fact
           .AddIndependentVariable("EUDAI", "\"Eudaimonia M.\"")
           .AddIndependentVariable(new Variable("all_stages", string.Join(" + ", includedStages)))
           .AddIndependentVariable(new Variable("all_characters", string.Join(" + ", includedChars)))
           .AddIndependentVariable(new StringArrayMap("enemy_map", EnemyMap))
           .AddIndependentVariable(new StringArray("enemy_arcana_map", EnemiesRequireArcana))
           .AddIndependentVariable(new Variable("non_special_characters",
                "[character for character in all_characters if character not in secret_characters and character not in megalo_characters and character not in unfair_characters]"))
           .AddLocations("stages_beaten", StageNameMap.Values.Select(s => (string[])[$"{s} Beaten", s]))
           .AddLocations("beat_with_character",
                CharacterData.GetNames().Select(s => (string[])[$"Beat with {s}", "Characters"]))
           .AddLocations("open_chests",
                StageNameMap.Values.SelectMany(s => Enumerable.Range(1, 10)
                                                              .Select(i => (string[])[$"Open Chest #{i} on {s}", s])))
           .AddLocations("kill_enemies",
                EnemyMap.Keys.Where(s => !EnemiesRequireArcana.Contains(s))
                        .Select(s => (string[])[$"Kill {s}", "Enemies"]))
           .AddLocations("kill_enemies_arcana",
                EnemyMap.Keys.Where(s => EnemiesRequireArcana.Contains(s))
                        .Select(s => (string[])[$"Kill {s}", "Enemies"]));

        return;

        void AllowClassifiers<T>(string name, T[] array, int type)
            where T : IClassifier, IGetName
        {
            location_fact.AddIndependentVariable(new StringArray(name.OptionVariableFormat(),
                array.Where(data => data.GetClassifier(type)).GetNames()));
        }

        void AddClassifierOptionsSet<T>(string included, int selector, T[] array) where T : IClassifier, IGetName
            => AddVariableOptionsSet(included, array.OfClassifier(selector));

        void AddDlcOptionsSet<T>(string included, VsDlc dlc, T[] array) where T : IGetDlc, IGetName
            => AddVariableOptionsSet(included, array.OfDlc(dlc));

        void AddVariableOptionsSet<T>(string included, T[] array) where T : IGetName
        {
            location_fact.AddIndependentVariable(new StringArray(included.OptionVariableFormat(), array.GetNames()));
            (included.EndsWith("Stages") ? includedStages : includedChars).Add(included.OptionVariableFormat());
        }
    }

    public override void Items(WorldFactory _, ItemFactory item_fact)
    {
        item_fact.AddIndependentVariable(new Variable("unlock_character_items",
                      "[f\"Character Unlock: {character}\" for character in all_characters]"))
                 .AddIndependentVariable(new Variable("unlock_stage_items",
                      "[f\"Stage Unlock: {stage}\" for stage in (all_stages + [EUDAI])]"))
                 .AddIndependentVariable(new Variable("unlock_gamemodes",
                      "[f\"Gamemode Unlock: {gamemode}\" for gamemode in [\"Hyper\", \"Hurry\", \"Arcanas\", \"Eggs\"]]"))
                 .AddIndependentVariable(new Variable("filler_items",
                      "['Empty Coffins', 'Floor Chickens', 'Suspiciously Clean Skull', 'Easter Eggs', 'Progressive Nothing']"))
                 .AddToFinalLocationList("**{item: ItemClassification.progression for item in unlock_character_items}")
                 .AddToFinalLocationList("**{item: ItemClassification.progression for item in unlock_stage_items}")
                 .AddToFinalLocationList("**{item: ItemClassification.progression for item in unlock_gamemodes}")
                 .AddToFinalLocationList("**{item: ItemClassification.filler for item in filler_items}")
                 .AddCreateItems(method => method
                                          .AddCode(new Variable("stages", "world.final_included_stages_list"))
                                          .AddCode(new Variable("characters", "world.final_included_characters_list"))
                                          .AddCode(new ForLoopFactory("stage", "stages")
                                                  .AddCode(new IfFactory("stage == world.starting_stage").AddCode(
                                                       "continue"))
                                                  .AddCode(
                                                       "pool.append(world.create_item(f\"Stage Unlock: {stage}\"))"))
                                          .AddCode(new ForLoopFactory("character", "characters")
                                                  .AddCode(new IfFactory("character == world.starting_character")
                                                      .AddCode("continue"))
                                                  .AddCode(
                                                       "pool.append(world.create_item(f\"Character Unlock: {character}\"))"))
                                          .AddCode(new IfFactory("options.lock_hurry_behind_item").AddCode(
                                               CreateItem("Gamemode Unlock: Hurry")))
                                          .AddCode(new IfFactory("options.lock_hyper_behind_item").AddCode(
                                               CreateItem("Gamemode Unlock: Hyper")))
                                          .AddCode(new IfFactory("options.lock_arcanas_behind_item").AddCode(
                                               CreateItem("Gamemode Unlock: Arcanas")))
                                          .AddCode(new IfFactory("options.egg_inclusion").AddCode(
                                               CreateItem("Gamemode Unlock: Eggs")))
                                          .AddCode("world.location_count -= (len(stages) - 1) + (len(characters) - 1)")
                                          .AddCode(CreateItemsFillRemainingWith("filler_items")));
    }

    public override void Rules(WorldFactory _, RuleFactory rule_fact)
    {
        rule_fact.AddCompoundLogicFunction("char", "has_character", "has[f\"Character Unlock: {character}\"]",
                      "character")
                 .AddCompoundLogicFunction("stage", "has_stage", "has[f\"Stage Unlock: {stage}\"]", "stage")
                 .AddCompoundLogicFunction("anyStage", "has_any_stage",
                      "any[[f\"Stage Unlock: {stage}\" for stage in stages]]", "stages")
                 .AddCompoundLogicFunction("gamemode", "has_gamemode", "has[f\"Gamemode Unlock: {gamemode}\"]",
                      "gamemode")
                 .AddCompoundLogicFunction("hurry", "has_hurry", "gamemode[\"Hurry\"]")
                 .AddCompoundLogicFunction("arcana", "has_arcana", "gamemode[\"Arcanas\"]")
                 .AddLogicRules(StageNameMap.ToDictionary(kv => $"{kv.Value} Beaten", _ => "hurry"))
                 .AddLogicRules(CharacterData.ToDictionary(data => $"Beat with {data.Name}",
                      data => $"char[\"{data.Name}\"] and hurry"))
                 .AddLogicRules(EnemyMap
                               .Where(kv => kv.Key is not "Death")
                               .ToDictionary(kv => $"Kill {kv.Key}", kv =>
                                {
                                    List<string> rules = [];
                                    var rawHurryStages = EnemyHurryMap.GetValueOrDefault(kv.Key, []);
                                    var rawStages = kv.Value.Where(s => !rawHurryStages.Contains(s)).ToArray();
                                    if (rawStages.Length != 0) rules.Add($"anyStage[{rawStages.AsStringifiedArray()}]");
                                    if (rawHurryStages.Length != 0)
                                        rules.Add($"( anyStage[{rawHurryStages.AsStringifiedArray()}] and hurry )");

                                    return EnemyData.First(data => data.GetName() == kv.Key).NeedArcana
                                        ? $"arcana and {string.Join(" or ", rules)}" : string.Join(" or ", rules);
                                }))
                 .AddLogicRule("Kill Death", "stage[\"Ode to Castlevania\"] and char[\"Richter Belmont\"] and hurry");
    }

    public override void Regions(WorldFactory _, RegionFactory region_fact)
    {
        region_fact.AddRegions("Characters", "Enemies")
                   .AddRegions(StageNameMap.Values.ToArray())
                   .AddConnection("Menu", "Characters")
                   .AddConnection("Menu", "Enemies")
                   .InjectCodeIntoCreateRegions(method =>
                    {
                        method.AddCode(new Variable("stages", "world.final_included_stages_list"))
                              .AddCode(new Variable("characters", "world.final_included_characters_list"))
                              .AddCode(new Variable("chest_checks", "options.chest_checks_per_stage"))
                              .AddCode(new ForLoopFactory("stage", "stages")
                                      .AddCode(new CodeBlockFactory()
                                          .AddCode(
                                               "make_location(world, f\"{stage} Beaten\", region_map[stage], rule_map)"))
                                      .AddCode(
                                           "make_event_location(world, f\"Event: [{stage} Beaten]\", f\"{stage} Beaten\", \"Beat a Stage\", None, region_map[stage], rule_map)")
                                      .AddCode(new IfFactory("stage != EUDAI").AddCode(
                                           new ForLoopFactory("i", "range(chest_checks)")
                                              .AddCode(
                                                   "make_location(world, f\"Open Chest #{i + 1} on {stage}\", region_map[stage], rule_map)")))
                                      .AddCode(
                                           $"region_map[\"Menu\"].connect(region_map[stage], rule = lambda state, stage_name=stage: {StateHas("f\"Stage Unlock: {stage_name}\"", stringify: false, returnValue: false)})"))
                              .AddCode(new ForLoopFactory("character", "characters")
                                  .AddCode(
                                       "make_location(world, f'Beat with {character}', region_map['Characters'], rule_map)"))
                              .AddCode(new IfFactory("options.enemysanity")
                                  .AddCode(new ForLoopFactory("enemy, raw_find_locs", "enemy_map.items()")
                                          .AddCode(new IfFactory(
                                                   "enemy in enemy_arcana_map and not options.enemysanity_arcana_enemies")
                                              .AddCode("continue"))
                                          .AddCode(new IfFactory(
                                                   "enemy == 'Death' and ('Ode to Castlevania' not in stages or 'Richter Belmont' not in characters)")
                                              .AddCode("continue"))
                                          .AddCode(new IfFactory("not any(loc in stages for loc in raw_find_locs)")
                                              .AddCode("continue"))
                                          .AddCode(
                                               "make_location(world, f\"Kill {enemy}\", region_map['Enemies'], rule_map)")));
                    });
    }

    public override void Init(WorldFactory worldFactory, WorldInitFactory init_fact)
    {
        init_fact
           .UseInitFunction(method => method
                                     .AddCode(new Variable("self.starting_character", "\"\""))
                                     .AddCode(new Variable("self.starting_stage", "\"\""))
                                     .AddCode(new Variable("self.stage_goal_amount", "0"))
                                     .AddCode(new Variable("self.final_included_characters_list", "[]"))
                                     .AddCode(new Variable("self.final_included_stages_list", "[]"))
                                     .AddCode(new Variable("self.ending_character_count", "0"))
                                     .AddCode(new Variable("self.ending_stage_count", "0")))
           .AddUseUniversalTrackerPassthrough(method => method
                                                       .AddCode(CreateUtPassthrough("self.starting_character",
                                                            "starting_character"))
                                                       .AddCode(CreateUtPassthrough("self.starting_stage",
                                                            "starting_stage"))
                                                       .AddCode(CreateUtPassthrough("self.final_included_stages_list",
                                                            "final_stages"))
                                                       .AddCode(CreateUtPassthrough(
                                                            "self.final_included_characters_list", "final_chars"))
                                                       .AddCode(CreateUtPassthrough("self.ending_stage_count",
                                                            "ending_stage_count"))
                                                       .AddCode(CreatePushPrecollected(
                                                            "f\"Stage Unlock: {self.starting_stage}\"",
                                                            stringify: false))
                                                       .AddCode(CreatePushPrecollected(
                                                            "f\"Character Unlock: {self.starting_character}\"",
                                                            stringify: false))
                                                       .AddCode(CreatePushPrecollected("Gamemode Unlock: Arcanas",
                                                            "self.options.lock_arcanas_behind_item.value"))
                                                       .AddCode(CreatePushPrecollected("Gamemode Unlock: Hurry",
                                                            "self.options.lock_hurry_behind_item.value")), false, false)
           .UseGenerateEarly(method => method.AddCode(GenEarly))
           .UseCreateRegions()
           .AddCreateItems()
           .UseSetRules(method =>
                method.AddCode(new IfFactory("self.options.goal_requirement == 0")
                              .AddCode(CreateGoalCondition("hasN[\"Beat a Stage\", self.stage_goal_amount]",
                                   worldFactory.GetRuleFactory())).AddElseIf(
                                   new ElifFactory("self.options.goal_requirement == 1")
                                      .AddCode(new IfFactory("self.ending_stage_count == 0").AddCode(
                                           "self.ending_stage_count = int(len(self.final_included_stages_list) * .75)"))
                                      .AddCode(CreateGoalCondition(
                                           "stage[EUDAI] and hasN[\"Beat a Stage\", self.ending_stage_count]",
                                           worldFactory.GetRuleFactory())))))
           .UseFillSlotData(new Dictionary<string, string>
            {
                ["goal_requirement"] = "int(self.options.goal_requirement)",
                ["egg_inclusion"] = "int(self.options.egg_inclusion.value)",
                ["starting_character"] = "str(self.starting_character)",
                ["starting_stage"] = "str(self.starting_stage)",
                ["stages_to_beat"] = "str(self.final_included_stages_list)",
                ["is_hyper_locked"] = "bool(self.options.lock_hyper_behind_item)",
                ["is_hurry_locked"] = "bool(self.options.lock_hurry_behind_item)",
                ["is_arcanas_locked"] = "bool(self.options.lock_arcanas_behind_item)",
                ["chest_checks_per_stage"] = "int(self.options.chest_checks_per_stage)",
                ["enemysanity"] = "bool(self.options.enemysanity)",
                ["final_stages"] = "self.final_included_stages_list",
                ["final_chars"] = "self.final_included_characters_list",
                ["ending_stage_count"] = "int(self.ending_stage_count)",
            })
           .InjectCodeIntoWorld(world =>
                world.AddVariable(new Variable("gen_puml", "False"))
                     .AddVariable(new MappedVariable<string, string>("item_name_groups",
                          new Dictionary<string, string>
                          {
                              ["\"unlocks\""] = "unlock_character_items + unlock_stage_items + unlock_gamemodes"
                          })))
           .UseGenerateOutput(method => method.AddCode(PumlGenCode()));
    }

    public override string GenerateGraphViz(
        WorldFactory worldFactory, Dictionary<string, string> associations, Func<string, string> getRule,
        string[][] locationDoubleArrays
    )
    {
        return new GraphBuilder(GameName)
              .ForEachOf(StageNameMap, (b, kv) => b.AddConnection("Menu", kv.Value, $"stage[\"{kv.Value}\"]"))
              .AddConnection("Menu", "Characters")
              .AddConnection("Menu", "Enemies")
              .AddLocationsFromDoubleArray(locationDoubleArrays, getRule)
              .ForEachOf(StageNameMap,
                   (b, kv) => b.AddEventLocation(kv.Value, getRule, $"Event: [{kv.Value} Beaten]", $"{kv.Value} Beaten",
                       "Beat a Stage"))
              .GenString();
    }
}