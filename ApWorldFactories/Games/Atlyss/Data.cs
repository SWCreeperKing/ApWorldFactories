using System.Text;
using static CreepyUtil.Archipelago.WorldFactory.ItemFactory;

namespace ApWorldFactories.Games.Atlyss;

public readonly struct LocationLevelData(DataArray param) : IFarmingNode
{
    [Mark] public readonly string Area = param;
    [Mark] public readonly int LevelMin = param;
    [Mark] public readonly int LevelMax = param;
    [Mark] public readonly string Connection = param.Get() is "N/A" or "" ? "Menu" : param[3];
    [Mark] public readonly string[] Enemies = param.GetSplitAndTrim().Where(s => s is not "N/A").ToArray();
    [Mark] public readonly string QuestRequirement = param.Get() is "N/A" ? "" : param[5];
    [Mark] public readonly int ProgressivePortalCount = param;

    public string GenRule()
    {
        List<string> rules = [];
        if (Connection is not "Menu") rules.Add($"area[\"{Area}\"]");
        if (QuestRequirement is not "") rules.Add($"quest[\"{QuestRequirement}\"]");
        return string.Join(" and ", rules);
    }

    public string FarmAreaMinMaxLevel() => $"[\"{Area}\", {LevelMin}, {LevelMax}]";
}

public readonly struct EnemyListData(DataArray param)
{
    [Mark] public readonly string Name = param;
    [Mark] public readonly int Level = param;
    [Mark] public readonly int Tier = param;
    [Mark] public readonly string[] Areas = param.GetSplitAndTrim();
    [Mark] public readonly bool IsBoss = param;
}

public readonly struct QuestData(DataArray param)
{
    [Mark] public readonly string Quest = param;
    [Mark] public readonly string PrevQuest = param.Get(false) is "N/A" ? "" : param;
    [Mark] public readonly int Level = param;
    [Mark] public readonly string AreaAccepted = param;
    [Mark] public readonly string ClassRequired = param;

    [Mark] public readonly string[] AreasRequired
        = param.GetSplitAndTrim().Where(s => s is not ("N/A" or "")).ToArray();

    [Mark] public readonly string LogicNotes = param;

    public bool Enabled => LogicNotes is not "Class Quest";

    public string GenRule()
    {
        var areas = $"[{string.Join(", ", AreasRequired.Distinct().Select(s => $"\"{s}\""))}]";
        List<string> rules = [$"level[{Level}]"];

        if (PrevQuest is not "") rules.Add($"quest[\"{PrevQuest}\"]");

        if (LogicNotes.Trim() is not "") 
            rules.Add(
                $"{LogicNotes.Replace("+", "and").Replace("Needs Any", $"any_area[{areas}]").Replace("Requires All", $"all_areas[{areas}]").Trim()}"
            );

        return string.Join(" and ", rules);
    }
}

public readonly struct ProfessionsData(DataArray param)
{
    [Mark] public readonly string Profession = param;
    [Mark] public readonly string NodeType = param;
    [Mark] public readonly int MinLevel = param;
    [Mark] public readonly int MaxInLogic = param;
    [Mark] public readonly string[] Areas = param;

    public IFarmingNode[] GetNodes()
    {
        var self = this;
        return Areas.Select(area => new SingleNodes(area, self.MinLevel, self.MaxInLogic)).Cast<IFarmingNode>()
                    .ToArray();
    }

    public readonly struct SingleNodes(string area, int minLevel, int maxLevel) : IFarmingNode
    {
        public readonly string Area = area;
        public readonly int MinLevel = minLevel;
        public readonly int MaxLevel = maxLevel;
        public string FarmAreaMinMaxLevel() => $"[\"{Area}\", {MinLevel}, {MaxLevel}]";
    }
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

public readonly struct AchievementData(DataArray param)
{
    [Mark] public readonly string Name = param;
    [Mark] public readonly string Area = param.Get(false) is "" ? "Menu" : param;
    [Mark] public readonly int Level = param;
    [Mark] public readonly ClassType Class = param.GetEnum<ClassType>();
    [Mark] public readonly string Subclass = param;

    [Mark] public readonly string[] RequiredItems =
        param.GetSplitAndTrim()
             .Where(s => s.Trim() is not "")
             .Select(s =>
                  {
                      var split = s.Split('x');
                      return split.Length < 2 ? $"has[\"{s}\"]"
                          : $"hasN[\"{string.Join('x', split.Skip(1))}\", {split[0]}]";
                  }
              ).ToArray();

    [Mark] public readonly bool Enabled = param;

    public string GenRule()
    {
        List<string> rules = [$"level[{Level}]"];

        if (Area is not ("Menu" or "Sanctum")) rules.Add($"area[\"{Area}\"]");
        if (RequiredItems.Any()) rules.AddRange(RequiredItems);

        return string.Join(" and ", rules);
    }
}

public interface IFarmingNode
{
    public string FarmAreaMinMaxLevel();
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