namespace ApWorldFactories.Games.Widget_Inc;

public readonly struct TechTreeData(DataArray param)
{
    [Mark] public readonly string Tech = param;
    [Mark] public readonly string PreviousTech = param;
    [Mark] public readonly string Id = param;

    [Mark] public readonly string[] ResourceRequirements
        = param.GetSplitAndTrim().Where(s => s is not "").ToArray();

    [Mark] public readonly string Unlock = param;
    [Mark] public readonly int TierRequirement = param;
}

public readonly struct ResourceData(DataArray param)
{
    [Mark] public readonly string Resource = param;
    [Mark] public readonly string[] CraftingRequirements = param;
}