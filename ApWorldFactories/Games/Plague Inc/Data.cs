namespace ApWorldFactories.Games.Plague_Inc;

public readonly struct DiseaseData(string[] param)
{
    public readonly string Name = param[0];
    public readonly string Id = param[1];
    public readonly bool Include = param[2].IsTrue();
}

public readonly struct CountryData(string[] param)
{
    public readonly string Name = param[0];
    public readonly bool IsWealthy = param[1].IsTrue();
    public readonly bool IsPoor = param[2].IsTrue();
    public readonly bool IsUrban = param[3].IsTrue();
    public readonly bool IsRural = param[4].IsTrue();
    public readonly bool IsHot = param[5].IsTrue();
    public readonly bool IsCold = param[6].IsTrue();
    public readonly bool IsHumid = param[7].IsTrue();
    public readonly bool IsArid = param[8].IsTrue();
}

public readonly struct DifficultyData(string[] param)
{
    public readonly string DiseaseType = param[0];
    public readonly string Difficulty = param[1];
    public readonly int VictoryScore = int.Parse(param[2]);
}

public readonly struct HexLayoutData(string[] param)
{
    public readonly int Hex = int.Parse(param[0]);
    public readonly int[] AdjacentHexes = param[1].Trim('[', ']').Split(',').Select(s => int.Parse(s.Trim())).ToArray();
}

public readonly struct TechData(string[] param)
{
    public readonly string Id = param[0] is "" ? param[2] : param[0];
    public readonly int Hex = int.Parse(param[1]);

    public readonly LogicRule RuleType = param[3].ToLower() switch
    {
        "always" => LogicRule.Always,
        "adjacent" => LogicRule.Adjacent,
        "specific" => LogicRule.Specific,
    };

    public readonly string SpecificRuleTechs = string.Join(
        " and ", param[4].SplitAndTrim(',').Select(s => $"has[\"{s}\"]")
    );

    public readonly string[] Diseases = param[5].SplitAndTrim(',');

    public readonly string TechTreeType = param[6];

    public readonly float Infectivity = param[7] is "" ? 0 : float.Parse(param[7]);

    // public readonly float Severity = float.Parse(param[8]);
    public readonly float Lethality = param[9] is "" ? 0 : float.Parse(param[9]);
    // public readonly float MutationChance = float.Parse(param[10]);
    // public readonly float LandTransmission = float.Parse(param[11]);
    // public readonly float SeaTransmission = float.Parse(param[12]);
    // public readonly float AirTransmission = float.Parse(param[13]);
    // public readonly float CorpseTransmission = float.Parse(param[14]);
    // public readonly float WealthyEffectiveness = float.Parse(param[15]);
    // public readonly float PoorEffectiveness = float.Parse(param[16]);
    // public readonly float UrbanEffectiveness = float.Parse(param[17]);
    // public readonly float RuralEffectiveness = float.Parse(param[18]);
    // public readonly float HotEffectiveness = float.Parse(param[19]);
    // public readonly float ColdEffectiveness = float.Parse(param[20]);
    // public readonly float HumidEffectiveness = float.Parse(param[21]);
    // public readonly float AridEffectiveness = float.Parse(param[22]);
    // public readonly float CureRequirement = float.Parse(param[23]);
    // public readonly float CureResearchEfficiency = float.Parse(param[24]);
    // public readonly bool CanDevolve = param[25].IsTrue();
    // public readonly float DevolveCostModifier = float.Parse(param[26]);
    // public readonly string ImportantNotes = param[27];
    // public readonly float TraitCost = float.Parse(param[28]);

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
                $"has[\"{Disease}\"] and ( {string.Join(" or ", maps[disease][tab][Hex].Adjacents.Where(node => node.Name is not "").Select(node => $"has[\"{node.Name}\"]"))} )",
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