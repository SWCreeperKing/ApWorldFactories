namespace ApWorldFactories.Games.Vampire_Survivors;

public static class MergeLegacyData
{
    public static void MergeData(VampireSurvivors vs)
    {
        var enemyTypeMap = vs.EnemyNameMap.ToDictionary(kv => kv.Value, kv => kv.Key);
        enemyTypeMap["Mikoshi-nyudo"] = "MS_MIKOS";
        enemyTypeMap["Thunderous Oni"] = "MS_ONITHUNDER";
        enemyTypeMap["Windy Oni"] = "MS_ONIWIND";
        enemyTypeMap["Sam the Sandown Clown"] = "FS_CLOWN";
        enemyTypeMap["Hippogryph"] = "TP_HIPPOGRYPH";
        enemyTypeMap["Fleaman"] = "TP_FLEAMAN";
        enemyTypeMap["Miragellos"] = "EX_SPIRIT1";
        enemyTypeMap["Spiritello"] = "MS_SPIRIT1";
        enemyTypeMap["Followa"] = "FS_MEATY";
        enemyTypeMap["OG Merman"] = "TP_FISHMAN_01";
        enemyTypeMap["Divine Wood Spirit"] = "EME_GATEBOSS_SPIRITBEAST";
        var stageTypeMap = vs.StageNameMap.ToDictionary(kv => kv.Value, kv => kv.Key);
        stageTypeMap["Ode to Castlevania"] = "TP_CASTLE";
        stageTypeMap["Polus Replica"] = "POLUS";
        stageTypeMap["Neo Galuga"] = "FB_GALUGA";
        stageTypeMap["Emerald Diorama"] = "EMERALD";

        var legacyEnemyMap = vs.ReadData("Legacy Data/Enemy List", StripData)
                               .ToDictionary(t => t.Item1, t => t.Item2);
        var legacyEnemyHurryMap = vs.ReadData("Legacy Data/Enemy Hurry List", StripData)
                                    .ToDictionary(t => t.Item1, t => t.Item2);

        var missingHurry = legacyEnemyHurryMap
                          .Select(kv => (kv.Key,
                               kv.Value
                                 .Where(s => !vs.EnemyHurryMap.GetValueOrDefault(kv.Key, []).Contains(s))
                                 .Where(s => !vs.EnemyMap.GetValueOrDefault(kv.Key, []).Contains(s)).ToArray())
                           )
                          .Where(t => t.Item2.Length != 0)
                          .ToDictionary();

        var missingNormal = legacyEnemyMap
                           .Select(kv => (kv.Key,
                                kv.Value
                                  .Where(s => !vs.EnemyHurryMap.GetValueOrDefault(kv.Key, []).Contains(s))
                                  .Where(s => !vs.EnemyMap.GetValueOrDefault(kv.Key, []).Contains(s))
                                  .Where(s => !missingHurry.GetValueOrDefault(kv.Key, []).Contains(s)).ToArray())
                            )
                           .Where(t => t.Item2.Length != 0)
                           .ToDictionary();

        var convertedMissing
            = missingNormal.Select(kv => (enemyTypeMap[kv.Key], kv.Value.Select(s => stageTypeMap[s]).ToArray()));
        var convertedMissingHurry
            = missingHurry.Select(kv => (enemyTypeMap[kv.Key], kv.Value.Select(s => stageTypeMap[s]).ToArray()));

        vs.WriteData("(LEGACY) Bestiary", vs.EnemyMap.Keys);
        
        vs.WriteData("T(LEGACY) ransformed Data", convertedMissing.SelectMany(t => t.Item2.Select(s => $"{t.Item1}\t{s}\t5")));
        vs.WriteData(
            "(LEGACY) Transformed Data Hurry", convertedMissingHurry.SelectMany(t => t.Item2.Select(s => $"{t.Item1}\t{s}\t30"))
        );

        vs.WriteData("(LEGACY) missing names", vs.EnemyNameMap.Values.Except(vs.EnemyMap.Keys).ToArray());
        CheckForUnnamedEnemies();
        return;

        void CheckForUnnamedEnemies()
        {
            vs.WriteData(
                "(LEGACY) new_unnamed_enemy_data",
                vs.EnemyData.Where(data => data.BestiaryInclude && data.Name.Trim() is "").Select(data => data.EnemyId)
            );
        }
    }


    public static (string, string[]) StripData(string s)
    {
        var split = s.Replace("\"", "")
                     .Replace("[", "")
                     .Replace("]", "")
                     .Split(": ");

        return (split[0].Trim(), split[1].Split(',', StringSplitOptions.RemoveEmptyEntries));
    }
}