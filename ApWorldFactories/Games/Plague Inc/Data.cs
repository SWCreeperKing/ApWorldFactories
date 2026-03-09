namespace ApWorldFactories.Games.Plague_Inc;

public readonly struct DiseaseData(DataArray param)
{
    [Mark] public readonly string Name = param;
    [Mark] public readonly string Id = param;
    [Mark] public readonly bool Include = param;
}

public readonly struct CountryData(DataArray param)
{
    [Mark] public readonly string Name = param;
    [Mark] public readonly bool IsWealthy = param;
    [Mark] public readonly bool IsPoor = param;
    [Mark] public readonly bool IsUrban = param;
    [Mark] public readonly bool IsRural = param;
    [Mark] public readonly bool IsHot = param;
    [Mark] public readonly bool IsCold = param;
    [Mark] public readonly bool IsHumid = param;
    [Mark] public readonly bool IsArid = param;
}

public readonly struct DifficultyData(DataArray param)
{
    [Mark] public readonly string DiseaseType = param;
    [Mark] public readonly string Difficulty = param;
    [Mark] public readonly int VictoryScore = param;
}

public readonly struct HexLayoutData(DataArray param)
{
    [Mark] public readonly int Hex = param;

    [Mark] public readonly int[] AdjacentHexes
        = param.Get().Trim('[', ']').Split(',').Select(s => int.Parse(s.Trim())).ToArray();
}

public readonly struct TechData(DataArray param)
{
    [Mark] public readonly string OverrideId = param;
    [Mark] public readonly int Hex = param;
    [Mark] public readonly string CommonId = param;
    [Mark] public readonly LogicRule RuleType = param.GetEnum<LogicRule>();

    [Mark] public readonly string SpecificRuleTechs = string.Join(
        " and ", ((string[])param).Select(s => $"has[\"{s}\"]")
    );

    [Mark] public readonly string[] Diseases = param;
    [Mark] public readonly string TechTreeType = param;
    [Mark] public readonly float Infectivity = param;
    [Mark] public readonly float Severity = param;
    [Mark] public readonly float Lethality = param;

    [Mark] public readonly float MutationChance = param;
    [Mark] public readonly float TraitCost = param;
    [Mark] public readonly float LandTransmission = param;
    [Mark] public readonly float SeaTransmission = param;
    [Mark] public readonly float AirTransmission = param;
    [Mark] public readonly float CorpseTransmission = param;

    [Mark] public readonly float WealthyEffectiveness = param;
    [Mark] public readonly float PoorEffectiveness = param;
    [Mark] public readonly float UrbanEffectiveness = param;
    [Mark] public readonly float RuralEffectiveness = param;
    [Mark] public readonly float HotEffectiveness = param;
    [Mark] public readonly float ColdEffectiveness = param;
    [Mark] public readonly float HumidEffectiveness = param;
    [Mark] public readonly float AridEffectiveness = param;
    [Mark] public readonly float CureRequirement = param;
    [Mark] public readonly float CureResearchEfficiency = param;

    [Mark] public readonly bool CanDevolve = param;
    [Mark] public readonly float DevolveCostModifier = param;
    [Mark] public readonly string ImportantNotes = param;
    
    public string Id => OverrideId is "" ? CommonId : OverrideId;
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

        public string GenRule(Dictionary<string, Dictionary<string, HexMap>> maps)
            => $"has[\"{Disease}\"] and has[\"{Id}\"]{RuleType switch
            {
                LogicRule.Specific => $" and {SpecificRule}",
                LogicRule.Adjacent => $" and ( {string.Join(" or ", maps[disease][tab][Hex].Adjacents.Where(node => node.Name is not "").Select(node => $"has[\"{node.Name}\"]"))} )",
                _ => ""
            }}";
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