namespace ApWorldFactories.Games.Plague_Inc;

public readonly struct DiseaseData(DataArray param)
{
    public readonly string Name = param;
    public readonly string Id = param;
    public readonly bool Include = param;
}

public readonly struct CountryData(DataArray param)
{
    public readonly string Name = param;
    public readonly bool IsWealthy = param;
    public readonly bool IsPoor = param;
    public readonly bool IsUrban = param;
    public readonly bool IsRural = param;
    public readonly bool IsHot = param;
    public readonly bool IsCold = param;
    public readonly bool IsHumid = param;
    public readonly bool IsArid = param;
}

public readonly struct DifficultyData(DataArray param)
{
    public readonly string DiseaseType = param;
    public readonly string Difficulty = param;
    public readonly int VictoryScore = param;
}

public readonly struct HexLayoutData(DataArray param)
{
    public readonly int Hex = param;
    public readonly int[] AdjacentHexes = param.Get().Trim('[', ']').Split(',').Select(s => int.Parse(s.Trim())).ToArray();
}

public readonly struct TechData(DataArray param)
{
    public readonly string Id = param.Get() is "" ? param[2] : param[0];
    public readonly int Hex = param;

    public readonly LogicRule RuleType = param.SetIndex(3).Get().ToLower() switch
    {
        "always" => LogicRule.Always,
        "adjacent" => LogicRule.Adjacent,
        "specific" => LogicRule.Specific,
    };

    public readonly string SpecificRuleTechs = string.Join(
        " and ", param.GetSplitAndTrim().Select(s => $"has[\"{s}\"]")
    );

    public readonly string[] Diseases = param;
    public readonly string TechTreeType = param;
    public readonly float Infectivity = param;
    public readonly float Severity = param;
    public readonly float Lethality = param;
    
    public readonly float MutationChance = param;
    public readonly float TraitCost = param;
    public readonly float LandTransmission = param;
    public readonly float SeaTransmission = param;
    public readonly float AirTransmission = param;
    public readonly float CorpseTransmission = param;
    
    public readonly float WealthyEffectiveness = param;
    public readonly float PoorEffectiveness = param;
    public readonly float UrbanEffectiveness = param;
    public readonly float RuralEffectiveness = param;
    public readonly float HotEffectiveness = param;
    public readonly float ColdEffectiveness = param;
    public readonly float HumidEffectiveness = param;
    public readonly float AridEffectiveness = param;
    public readonly float CureRequirement = param;
    public readonly float CureResearchEfficiency = param;
    
    public readonly bool CanDevolve = param;
    
    public readonly float DevolveCostModifier = param;
    public readonly string ImportantNotes = param;

    public float GetScore => Infectivity + Lethality;

    public SingledOutTechData[] GetIndevTechs()
    {
        var tech = this;
        return Diseases.Select(disease => new SingledOutTechData(
                disease, tech.Id, (int)tech.GetScore, tech.RuleType, tech.SpecificRuleTechs, tech.Hex, tech.TechTreeType
            )
        ).ToArray();
    }

    public readonly struct SingledOutTechData
        (string disease, string id, int score, LogicRule ruleType, string specificRule, int hex, string tab)
    {
        public readonly string Disease = disease;
        public readonly string Name = $"{disease} Evolve: {id}";
        public readonly string Id = id;
        public readonly int Score = score;
        public readonly LogicRule RuleType = ruleType;
        public readonly string SpecificRule = specificRule;
        public readonly int Hex = hex;
        public readonly string Tab = tab;

        public string GenRule(Dictionary<string, Dictionary<string, HexMap>> maps) => RuleType switch
        {
            LogicRule.Specific => $"has[\"{Disease}\"] and {SpecificRule}",
            LogicRule.Always => $"has[\"{Disease}\"]",
            LogicRule.Adjacent =>
                $"has[\"{Disease}\"] and has[\"{Id}\"] and ( {string.Join(" or ", maps[disease][tab][Hex].Adjacents.Where(node => node.Name is not "").Select(node => $"has[\"{node.Name}\"]"))} )",
        };
    }
}

public readonly struct HexMap
{
    private readonly HexNode[] Nodes;

    public HexMap(int size)
    {
        Nodes = new HexNode[size];
        for (var i = 0; i < size; i++) { Nodes[i] = new HexNode("", []); }
    }

    public void SetNode(int node, string name, int[] adjacents)
    {
        var map = this;
        Nodes[node].Name = name;
        Nodes[node].Adjacents = adjacents.Select(i => map.Nodes[i]).ToArray();
    }

    public HexNode this[int i] => Nodes[i];

    public class HexNode(string name, HexNode[] nodes)
    {
        public string Name = name;
        public HexNode[] Adjacents = nodes;
    }
}

public enum LogicRule
{
    Always,
    Adjacent,
    Specific
}