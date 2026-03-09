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
    public override string WorldVersion => "0.1.0";

    public RegionData[] RegionData = [];
    public LocationData[] LocationData = [];
    public ItemData[] ItemData = [];
    public NpcQuestData[] NpcQuestData = [];

    public override void RunShenanigans()
    {
        GetSpreadsheet().ReadTable(out RegionData).SkipColumn()
                        .ReadTable(out LocationData).SkipColumn()
                        .ReadTable(out ItemData).SkipColumn()
                        .ReadTable(out NpcQuestData);

        WriteData("locations", LocationData.Select(data => $"{data.Area}:{data.Id}"));
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
                 .AddLogicRules(
                      LocationData.Where(data => data.Requirements.Any()).ToDictionary(
                          data => data.Location, data => data.GenRule()
                      )
                  )
                 .AddLogicRules(
                      NpcQuestData.ToDictionary(data => $"Complete {data.NpcName}'s Quest", data => data.GenRule())
                  );
    }

    public override void Regions(WorldFactory _, RegionFactory region_fact)
    {
        region_fact.AddRegions(RegionData.Select(data => data.Region).Distinct().ToArray());

        RegionData.Aggregate(
            region_fact,
            (factory, data) => factory.AddConnectionCompiledRule(data.ConnectsFrom, data.Region, data.GenRule())
        );

        region_fact.AddLocationsFromList("locations");

        NpcQuestData.Aggregate(
            region_fact,
            (factory, data) => factory.AddEventLocation(
                new EventLocationData(
                    data.Area, $"Complete {data.NpcName}'s Quest", $"{data.NpcName}'s Quest Completion",
                    $"Complete {data.NpcName}'s Quest"
                )
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
           .UseSetRules(method => method
               .AddCode(
                    CreateGoalCondition(
                        string.Join(" and ", NpcQuestData.Select(data => $"has[\"{data.NpcName}'s Quest Completion\"]")),
                        worldFactory.GetRuleFactory()
                    )
                )
            )
           .InjectCodeIntoWorld(world => world.AddVariable(new Variable("gen_puml", "False")))
           .UseGenerateOutput(method => method.AddCode(PumlGenCode()));
    }
}