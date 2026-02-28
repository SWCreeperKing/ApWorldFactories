namespace ApWorldFactories.Games.Widget_Inc;

public readonly struct TechTreeData(DataArray param)
{
    public readonly string Tech = param;
    public readonly string PreviousTech = param;
    public readonly string Id = param;

    public readonly string[] ResourceRequirements
        = param.GetSplitAndTrim().Where(s => s is not "").ToArray();

    public readonly string Unlock = param;
    public readonly int TierRequirement = param;
}

public readonly struct ResourceData(DataArray param)
{
    public readonly string Resource = param;
    public readonly string[] CraftingRequirements = param;
}