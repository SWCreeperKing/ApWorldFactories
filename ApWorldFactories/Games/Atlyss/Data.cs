using System.Text;
using static CreepyUtil.Archipelago.WorldFactory.ItemFactory;

namespace ApWorldFactories.Games.Atlyss;

public readonly struct LocationLevelData(DataArray param)
{
    [Mark] public readonly string Area = param;
    [Mark] public readonly int LevelMin = param;
    [Mark] public readonly int LevelMax = param;
    [Mark] public readonly string Connection = param.Get() is "N/A" ? "Menu" : param[3];
    [Mark] public readonly string[] Enemies = param.GetSplitAndTrim().Where(s => s is not "N/A").ToArray();
    [Mark] public readonly string QuestRequirement = param.Get() is "N/A" ? "" : param[5];

    public string GenRule()
    {
        List<string> rules = [];
        if (Connection is not "Menu") rules.Add($"area[\"{Connection}\"]");
        if (QuestRequirement is not "") rules.Add($"quest[\"{QuestRequirement}\"]");
        return string.Join(" and ", rules);
    }
}

public readonly struct EnemyListData(DataArray param)
{
    [Mark] public readonly string Name = param;
    [Mark] public readonly int Level = param;
    [Mark] public readonly string[] Areas = param.GetSplitAndTrim();
}

public readonly struct QuestData(DataArray param)
{
    [Mark] public readonly string Quest = param;
    [Mark] public readonly string PrevQuest = param.Get() is "N/A" ? "" : param[1];
    [Mark] public readonly int Level = param;
    [Mark] public readonly string AreaAccepted = param;

    [Mark] public readonly string[] AreasRequired
        = param.GetSplitAndTrim().Where(s => s is not "N/A").ToArray();

    [Mark] public readonly string LogicNotes = param;

    public bool Enabled => LogicNotes is not "Class Quest";

    public string GenRule()
    {
        var areas = AreasRequired.Append(AreaAccepted).Distinct().Select(s => $"area[\"{s}\"").ToArray();
        StringBuilder sb = new();

        if (PrevQuest is not "") sb.Append($"quest[\"{PrevQuest}\"] and ");

        return $"{(PrevQuest is not "" ? $"quest[\"{PrevQuest}\"] and " : "")}{LogicNotes.Replace("+", "and")
           .Replace("Needs Any", $"( {string.Join(" or ", areas)} )")
           .Replace("Requires All", $"( {string.Join(" and ", areas)} )")
        }";
    }
}

public readonly struct ProfessionsData(DataArray param)
{
    [Mark] public readonly string Profession = param;
    [Mark] public readonly string NodeType = param;
    [Mark] public readonly int MinLevel = param;
    [Mark] public readonly int MaxInLogic = param;
    [Mark] public readonly string[] Areas = param;
}

public readonly struct MerchantData(DataArray param)
{
    [Mark] public readonly string Name = param;
    [Mark] public readonly string Area = param;
}

public readonly struct ItemData(DataArray param)
{
    [Mark] public readonly string Name = param;
    [Mark] public readonly ItemType InGameClassification = param.GetEnum<ItemType>();
    [Mark] public readonly int LevelReq = param;
    [Mark] public readonly int Tier = param;
    [Mark] public readonly ClassType ClassRequirement = param.GetEnum<ClassType>();
    [Mark] public readonly ItemRarity ItemRarity = param.GetEnum<ItemRarity>();
    [Mark] public readonly ItemClassification Classification = param.GetEnum<ItemClassification>();
    [Mark] public readonly string WeaponClass = param;
    [Mark] public readonly string Notes = param;
    [Mark] public readonly int FillerWeight = param;
    [Mark] public readonly int ItemPoolCount = param;
}

public enum ClassType
{
    Any,
    Fighter,
    Mystic,
    Bandit
}

public enum ItemType
{
    None = 0,
    Consumable = 1,
    Weapon = 2,
    Helmet = 3,
    Cape = 4,
    ChestPiece = 5,
    Leggings = 6,
    Shield = 7,
    Trinket = 8,
    Currency = 9,
    PortalUnlock = 10,
    MonsterDrop = 11,
    OresAndIngots = 12,
    Fish = 13,
    TradeCurrency = 14,
    Cosmetic = 15,
    Dye = 16,
    QuestItem = 17,
}

public enum ItemRarity
{
    Common,
    TradeItem,
    Cosmetic,
    Currency,
    Rare,
    Consumable,
    Exotic,
    QuestItems
}

public static class Helper
{
    public static string Str(this ItemType type) =>
        type switch
        {
            ItemType.ChestPiece => "Chest Piece",
            ItemType.PortalUnlock => "Portal Unlock",
            ItemType.MonsterDrop => "Monster Drop",
            ItemType.OresAndIngots => "Ores & Ingots",
            ItemType.TradeCurrency => "Trade Currency",
            ItemType.QuestItem => "Quest Item", 
            _ => $"{type}",
        };
}