namespace ApWorldFactories.Games.Widget_Inc;

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