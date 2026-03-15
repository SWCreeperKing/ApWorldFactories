using ApWorldFactories.Graphviz;
using CreepyUtil.Archipelago.WorldFactory;
using static CreepyUtil.Archipelago.WorldFactory.PremadePython;

namespace ApWorldFactories.Games.Poco;

public class Poco : BuildData
{
    public override string SteamDirectory => DDrive;
    public override string ModFolderName => "SW_CreeperKing.Pocopelago";
    public override string GameName => "Poco";
    public override string ApWorldName => "poco";
    public override string GoogleSheetId => "1Hq3zKyTXmiiiCPAdSduEHKs5kzdlWuW7MVS0LdjU6U0";
    public override string WorldVersion => "0.1.1";

    public RegionData[] RegionData = [];
    public LocationData[] LocationData = [];
    public ItemData[] ItemData = [];
    public NpcQuestData[] NpcQuestData = [];
    public ItemBlockerData[] ItemBlockerData = [];

    public override void RunShenanigans()
    {
        GetSpreadsheet().ReadTable(out RegionData).SkipColumn()
                        .ReadTable(out LocationData).SkipColumn()
                        .ReadTable(out ItemData).SkipColumn()
                        .ReadTable(out NpcQuestData).SkipColumn()
                        .ReadTable(out ItemBlockerData);

        WriteData("locations", LocationData.Select(data => $"{data.Location}:{data.Id}"));
        WriteData("items", ItemData.Select(data => data.Name));
        WriteData("blockers", ItemBlockerData.Select(data => $"{data.Item}:{string.Join(',', data.Blockers)}"));
    }

    public override void Locations(WorldFactory _, LocationFactory location_fact)
    {
        location_fact.AddLocations("locations", LocationData.Select(data => (string[])[data.Location, data.Area]));
    }

    public override void Items(WorldFactory _, ItemFactory item_fact)
    {
        item_fact.AddItemListVariable(
                      "items", ItemFactory.ItemClassification.Progression,
                      list: ItemData.Select(data => data.Name).ToArray()
                  ).AddItem("Clown Nose", ItemFactory.ItemClassification.Filler)
                 .AddCreateItems(method
                      => method.AddCode(CreateItemsFromList("items"))
                               .AddCode(CreateItemsFillRemainingWithItem("Clown Nose"))
                  );
    }

    public override void Rules(WorldFactory _, RuleFactory rule_fact)
    {
        rule_fact.AddLogicFunction("has", "has_item", StateHas("item", stringify: false), "item")
                 .AddLogicFunction(
                      "quest", "done_quest", StateHas("f\"{quest}'s Quest Completion\"", stringify: false), "quest"
                  )
                 .AddLogicRules(
                      LocationData.Where(data => data.Requirements.Length != 0 || data.QuestRequirements.Length != 0)
                                  .ToDictionary(
                                       data => data.Location, data => data.GenRule()
                                   )
                  )
                 .AddLogicRules(
                      NpcQuestData.ToDictionary(data => data.QuestName, data => data.GenRule())
                  );
    }

    public override void Regions(WorldFactory _, RegionFactory region_fact)
    {
        region_fact.AddRegions(RegionData.Select(data => data.Region).Distinct().ToArray())
                   .ForEachOf(
                        RegionData,
                        (b, data) => b.AddConnectionCompiledRule(data.ConnectsFrom, data.Region, data.GenRule())
                    ).AddLocationsFromList("locations")
                   .ForEachOf(
                        NpcQuestData,
                        (b, data) => b.AddEventLocation(
                            new EventLocationData(data.Area, data.QuestName, data.QuestItem, data.QuestName)
                        )
                    );
    }

    public override void Init(WorldFactory worldFactory, WorldInitFactory init_fact)
    {
        init_fact
           .UseInitFunction()
           .AddUseUniversalTrackerPassthrough(yamlNeeded: false)
           .UseCreateRegions()
           .AddCreateItems()
           .UseSetRules(method => method.AddCode(
                    CreateGoalCondition(
                        string.Join(" and ", NpcQuestData.Select(data => $"has[\"{data.QuestItem}\"]")),
                        worldFactory.GetRuleFactory()
                    )
                )
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
              .AddRegions(RegionData.Select(data => data.Region).Distinct().ToArray())
              .ForEachOf(
                   RegionData, (b, data) => b.AddConnection(data.ConnectsFrom, data.Region, data.GenRule())
               )
              .AddLocationsFromDoubleArray(locationDoubleArrays, getRule)
              .ForEachOf(
                   NpcQuestData,
                   (b, data) => b.AddEventLocation(
                       data.Area, getRule, data.QuestName, data.QuestName, data.QuestItem
                   )
               )
              .GenString();
    }
}