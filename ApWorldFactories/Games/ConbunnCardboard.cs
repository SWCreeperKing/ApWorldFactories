using System.Text;
using CreepyUtil.Archipelago.WorldFactory;
using static CreepyUtil.Archipelago.WorldFactory.ItemFactory.ItemClassification;
using static CreepyUtil.Archipelago.WorldFactory.PremadePython;
using Range = CreepyUtil.Archipelago.WorldFactory.Range;

namespace ApWorldFactories.Games;

public class ConbunnCardboard() : BuildData(
    FDrive, "Conbunn Cardboard", "SW_CreeperKing.Conbunnipelago", "conbunn_cardboard",
    "1T4Gk3olQCz_J6dkZXPU1BtXysywf3wvbPeJHVYvnYic",
    "0.1.0"
)
{
    public override Dictionary<string, string> SheetGids { get; }

    public override void RunShenanigans(WorldFactory factory)
    {
        GetSpreadsheet("main")
           .ToFactory()
           .ReadTable(new RegionDataCreator(), 6, out var regionData)
           .ReadTable(new LocationDataCreator(), 3, out var locationData).SkipColumn()
           .ReadTable(new AbilityDataCreator(), 1, out var abilityData).SkipColumn()
           .ReadTable(new SkinDataCreator(), 5, out var skinData);

        var unlocks = regionData.Where(data => data.HasTransition).ToDictionary(
            data => data.Region, data => $"Transition Unlock: {data.Region}"
        );
        Dictionary<string, int[]> Counter = [];
        Dictionary<string, string> LocationIdMap = [];

        foreach (var location in locationData)
        {
            if (!Counter.ContainsKey(location.Region)) Counter[location.Region] = [1, 1];
            LocationIdMap[location.Id]
                = $"{location.Region} {(location.IsCoin ? "Coin" : "CD")} #{Counter[location.Region][location.IsCoin ? 0 : 1]++}";
        }

        factory.GetOptionsFactory(GitLink)
               .AddOption("CDs Required To Goal", "The amount of CDs required to goal", new Range(25, 5, 40))
               .AddCheckOptions(method => method.AddCode(CreateMinimalCatch(GameName)))
               .GenerateOptionFile();

        factory.GetLocationFactory(GitLink)
               .AddLocations(
                    "coins",
                    locationData.Where(data => data.IsCoin)
                                .Select(data => (string[])[LocationIdMap[data.Id], data.Region])
                )
               .AddLocations(
                    "cds",
                    locationData.Where(data => !data.IsCoin)
                                .Select(data => (string[])[LocationIdMap[data.Id], data.Region])
                )
               .AddLocations("skins", skinData.Select(data => (string[])[data.Name, data.Region]))
               .GenerateLocationFile();

        factory.GetItemFactory(GitLink)
               .AddItemListVariable("unlocks", Progression, list: unlocks.Values.ToArray())
               .AddItemListVariable("abilities", Progression, list: abilityData.Select(data => data.Name).ToArray())
               .AddItemCountVariable("CDs", new Dictionary<string, int> { ["CD"] = 40 }, Progression)
               .AddItem("Cardboard Coin", Filler)
               .AddCreateItems(method => method.AddCode(CreateItemsFromList("unlocks"))
                                               .AddCode(CreateItemsFromList("abilities"))
                                               .AddCode(CreateItemsFromMapCountGenCode("CDs"))
                                               .AddCode(CreateItemsFillRemainingWithItem("Cardboard Coin"))
                )
               .GenerateItemsFile();

        factory.GetRuleFactory(GitLink)
               .AddLogicFunction(
                    "unlock", "has_unlock", StateHas("f\"Transition Unlock: {transition}\"", stringify: false),
                    "transition"
                )
               .AddLogicFunction("dash", "has_dash", StateHas("Dash"))
               .AddLogicFunction("pads", "has_bounce_pads", StateHas("Bounce Pads"))
               .AddLogicFunction("coin", "has_coin_count", StateHas("Real Coin", "amt"), "amt")
               .AddLogicRules(locationData.ToDictionary(data => LocationIdMap[data.Id], data => data.GenRule))
               .AddLogicRules(skinData.ToDictionary(data => data.Name, data => data.GenRule))
               .GenerateRulesFile();

        var regionFactory = factory.GetRegionFactory(GitLink)
                                   .AddRegions(regionData.Select(data => data.Region).ToArray());

        regionData.Aggregate(
            regionFactory, (factory1, data)
                =>
            {
                var rule = data.GenRule;
                return rule is not "" ? factory1.AddConnectionCompiledRule(data.BackRegion, data.Region, rule)
                    : factory1.AddConnection(data.BackRegion, data.Region);
            }
        );

        regionFactory.AddLocationsFromList("coins")
                     .AddLocationsFromList("cds")
                     .AddLocationsFromList("skins")
                     .AddEventLocationsFromList("coins", item: "\"Real Coin\"")
                     .GenerateRegionFile();

        factory.GetHostSettingsFactory(GitLink).GenerateHostSettingsFile();

        factory.GetInitFactory()
               .UseInitFunction()
               .AddUseUniversalTrackerPassthrough(yamlNeeded: false)
               .UseCreateRegions()
               .AddCreateItems()
               .UseSetRules(method
                    => method.AddCode(
                        CreateGoalCondition(StateHas("CD", "self.options.cds_required_to_goal", returnValue: false))
                    )
                )
               .UseFillSlotData(
                    new Dictionary<string, string> { ["uuid"] = "str(shuffled)" },
                    method => method.AddCode(CreateUniqueId())
                )
               .InjectCodeIntoWorld(world => world.AddVariable(new Variable("gen_puml", "False")))
               .UseGenerateOutput(method => method.AddCode(PumlGenCode()))
               .GenerateInitFile();

        WriteData("LocationIds", LocationIdMap.Select(kv => $"{kv.Key}:{kv.Value}"));
        WriteData(
            "TransitionIds",
            regionData.Where(data => data.HasTransition).Select(data => $"{data.TransitionName}:{data.Region}")
        );
        WriteData(
            "LocationDoors", regionData.Where(data => data.HasDoor).Select(data => $"{data.Region},{data.DoorFrame}")
        );
        WriteData(
            "SkinData",
            skinData.OrderBy(data => int.Parse(data.Id)).Select(data => $"{data.Name},{data.LocalInt},{data.GloablInt}")
        );
    }
}

public readonly struct RegionData(string[] param)
{
    public readonly string RawRegion = param[0];
    public readonly string BackRegion = param[1];
    public readonly string[] Abilities = param[2].SplitAndTrim(',');
    public readonly string TransitionName = param[3];
    public readonly string DoorFrame = param[4];
    public readonly bool IsSubZone = param[5] is not ""&& param[5].ToLower()[0] == 'y';

    public string Region => IsSubZone ? $"{RawRegion} ({BackRegion})" : RawRegion;
    public bool HasTransition => TransitionName is not "";
    public bool HasDoor => DoorFrame is not "";

    public string GenRule
    {
        get
        {
            List<string> req = [];
            if (Abilities.Contains("Bounce Pads")) req.Add("pads");
            if (Abilities.Contains("Dash")) req.Add("dash");
            if (HasTransition) req.Add($"unlock[\"{Region}\"]");
            return string.Join(" and ", req);
        }
    }
}

public readonly struct LocData(string[] param)
{
    public readonly string Id = param[0];
    public readonly string Region = param[1];
    public readonly string[] Abilities = param[2].Split(',').Select(s => s.Trim()).ToArray();
    public bool IsCoin => Id.StartsWith("Coin_");

    public string GenRule
    {
        get
        {
            List<string> req = [];
            if (Abilities.Contains("Bounce Pads")) req.Add("pads");
            if (Abilities.Contains("Dash")) req.Add("dash");
            return string.Join(" and ", req);
        }
    }
}

public readonly struct AbilityData(string[] parm)
{
    public readonly string Name = parm[0];
}

public readonly struct SkinData(string[] param)
{
    public readonly string Name = param[0];
    public readonly string Id = param[1];
    public readonly string Region = param[2];
    public readonly string LocalInt = param[3];
    public readonly string GloablInt = param[4];

    public string GenRule
    {
        get
        {
            List<string> req = [$"unlock[\"{Region}\"]"];
            if (Id is "1") req.Add("coin[50]");
            if (Id is "2") req.Add("coin[150]");
            return string.Join(" and ", req);
        }
    }
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

public class SkinDataCreator : CsvTableRowCreator<SkinData>
{
    public override SkinData CreateRowData(string[] param) => new(param);
}