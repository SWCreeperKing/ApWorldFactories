using CreepyUtil.Archipelago.WorldFactory;
using static CreepyUtil.Archipelago.WorldFactory.ItemFactory.ItemClassification;
using static CreepyUtil.Archipelago.WorldFactory.PremadePython;
using Range = CreepyUtil.Archipelago.WorldFactory.Range;

namespace ApWorldFactories;

public class ConbunnCardboard() : BuildData(
    FDrive, "Conbunn Cardboard", "SW_CreeperKing.Conbunnipelago", "conbunn_cardboard", "Conbunn Cardboard - Sheet1.csv",
    "0.1.0"
)
{
    // Spreadsheet used for logic:
    // https://docs.google.com/spreadsheets/d/1T4Gk3olQCz_J6dkZXPU1BtXysywf3wvbPeJHVYvnYic/edit?usp=sharing

    public override void RunShenanigans(WorldFactory factory)
    {
        GetSpreadsheet()
           .ToFactory()
           .ReadTable(new RegionDataCreator(), 4, out var regionData).SkipColumn()
           .ReadTable(new LocationDataCreator(), 3, out var locationData).SkipColumn()
           .ReadTable(new AbilityDataCreator(), 1, out var abilityData);

        var Unlocks = regionData.Where(data => data.HasTransition).ToDictionary(
            data => data.Region, data => $"Transition Unlock: {data.Region}"
        );
        Dictionary<string, int[]> Counter = [];
        Dictionary<string, string> LocationIdMap = [];

        foreach (var location in locationData)
        {
            var isCoin = location.Id.StartsWith("Coin_");

            if (!Counter.ContainsKey(location.Region)) Counter[location.Region] = [1, 1];
            LocationIdMap[location.Id]
                = $"{location.Region} {(isCoin ? "Coin" : "CD")} #{Counter[location.Region][isCoin ? 0 : 1]++}";
        }

        factory.GetOptionsFactory(GitLink)
               .AddOption("CDs Required To Goal", "The amount of CDs required to goal", new Range(25, 5, 40))
               .AddCheckOptions()
               .GenerateOptionFile();

        factory.GetLocationFactory(GitLink)
               .AddLocations(
                    "collectables", locationData.Select(data => (string[])[LocationIdMap[data.Id], data.Region])
                )
               .GenerateLocationFile();

        factory.GetItemFactory(GitLink)
               .AddItems(Progression, items: Unlocks.Values.ToArray())
               .AddItems(Progression, items: abilityData.Select(data => data.Name).ToArray())
               .AddItemCountVariable("CDs", new Dictionary<string, int> { ["CDs"] = 40 }, Progression)
               .AddItem("Cardboard Coin", Filler)
               .AddCreateItems(_ => { })
               .GenerateItemsFile();

        factory.GetRuleFactory(GitLink)
               .AddLogicFunction(
                    "unlock", "has_unlock", StateHasR("f\"Transition Unlock: {transition}\""), "transition"
                )
               .AddLogicFunction("dash", "has_dash", StateHasSR("Dash"))
               .AddLogicFunction("pads", "has_bounce_pads", StateHasSR("Bounce Pads"))
               .AddLogicRules(locationData.ToDictionary(data => LocationIdMap[data.Id], data => data.GenRule))
               .GenerateRulesFile();

        var regionFactory = factory.GetRegionFactory(GitLink)
                                   .AddRegions(regionData.Select(data => data.Region).ToArray());

        regionData.Aggregate(
            regionFactory, (factory1, data)
                => data.BackRegions.Aggregate(
                    factory1,
                    (factory2, s) =>
                    {
                        if (data.HasTransition)
                            return factory2.AddConnectionCompiledRule(s, data.Region, $"unlock[\"{data.Region}\"]");
                        return factory2.AddConnection(s, data.Region);
                    }
                )
        );

        factory.GetHostSettingsFactory(GitLink).GenerateHostSettingsFile();

        regionFactory.AddLocationsFromList("collectables")
                     .GenerateRegionFile();

        factory.GetInitFactory()
               .AddUseUniversalTrackerPassthrough(yamlNeeded: false)
               .UseCreateRegions()
               .AddCreateItems()
               .UseSetRules(method
                    => method.AddCode(CreateGoalCondition(StateHasS("CDs", "self.options.cds_required_to_goal")))
                )
               .UseFillSlotData()
               .InjectCodeIntoWorld(world => world.AddVariable(new Variable("gen_puml", "False")))
               .UseGenerateOutput(method => method.AddCode(PumlGenCode()))
               .GenerateInitFile();

        WriteData("LocationIds", LocationIdMap.Select(kv => $"{kv.Key}:{kv.Value}"));
        WriteData(
            "TransitionIds",
            regionData.Where(data => data.HasTransition).Select(data => $"{data.TransitionName}:{data.Region}")
        );
    }
}

public readonly struct RegionData(string[] param)
{
    public readonly string Region = param[0];
    public readonly string[] BackRegions = param[1].Split(',').Select(s => s.Trim()).ToArray();
    public readonly string[] Abilities = param[2].Split(',').Select(s => s.Trim()).ToArray();
    public readonly string TransitionName = param[3];
    public bool HasTransition => TransitionName is not "";
}

public readonly struct LocData(string[] param)
{
    public readonly string Id = param[0];
    public readonly string Region = param[1];
    public readonly string[] Abilities = param[2].Split(',').Select(s => s.Trim()).ToArray();
    public string GenRule => $"unlock[\"{Region}\"]";
}

public readonly struct AbilityData(string[] parm)
{
    public readonly string Name = parm[0];
}

public class RegionDataCreator : CsvTableRowCreator<RegionData>
{
    public override RegionData CreateRowData(string[] param) => new(param);
    public override bool IsValidData(RegionData t) => t.Region is not "Menu";
}

public class LocationDataCreator : CsvTableRowCreator<LocData>
{
    public override LocData CreateRowData(string[] param) => new(param);
}

public class AbilityDataCreator : CsvTableRowCreator<AbilityData>
{
    public override AbilityData CreateRowData(string[] param) => new(param);
}