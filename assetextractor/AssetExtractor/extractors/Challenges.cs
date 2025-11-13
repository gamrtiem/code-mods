using System;
using System.IO;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Skills;
using Path = System.IO.Path;

namespace AssetExtractor;

public static partial class WikiFormat
{
    public static void FormatChallenges(ReadOnlyContentPack readOnlyContentPack)
    {
        var path = Path.Combine(WikiOutputPath, WIKI_OUTPUT_CHALLENGES);

        var f = "challenges[\"{0}\"] = {{\n";
        f += "\tType = \"{1}\",\n";
        f += "\tUnlock = {{ \"{2}\" }} ,\n";
        f += "\tDesc = \"{3}\",\n";
        f += "\t}}";

        if (!Directory.Exists(WikiOutputPath)) Directory.CreateDirectory(WikiOutputPath);
        TextWriter tw = new StreamWriter(path, WikiAppend);

        foreach (var unlockdef in readOnlyContentPack.unlockableDefs)
        {
            var name = "";
            var type = "";
            var unlock = "";
            var desc = "";
            var achievement = AchievementManager.GetAchievementDefFromUnlockable(unlockdef.cachedName);
            if (achievement == null) continue;
            if (achievement.nameToken != null) name = Language.GetString(achievement.nameToken);
            //Log.Debug(name);
            if (achievement.unlockableRewardIdentifier != null)
            {
                foreach (var item in ItemCatalog.allItemDefs)
                {
                    if (item.unlockableDef != unlockdef) continue;
                    unlock = Language.GetString(item.nameToken);
                    type = "Items";
                    break;
                }

                foreach (var surv in SurvivorCatalog.allSurvivorDefs)
                {
                    if (unlock != "") break;
                    if (surv.unlockableDef != unlockdef) continue;
                    unlock = Language.GetString(surv.displayNameToken);
                    type = "Survivors";
                    break;
                }

                foreach (var equip in EquipmentCatalog.equipmentDefs)
                {
                    if (unlock != "") break;
                    if (equip.unlockableDef != unlockdef) continue;
                    unlock = Language.GetString(equip.nameToken);
                    type = "Equipment";
                    break;
                }

                foreach (var skin in SkinCatalog.allSkinDefs)
                {
                    if (unlock != "") break;
                    if (skin.unlockableDef != unlockdef) continue;
                    unlock = Language.GetString(skin.nameToken);
                    type = "Skins";
                    break;
                }

                foreach (var artifact in ArtifactCatalog.artifactDefs)
                {
                    if (unlock != "") break;
                    if (artifact.unlockableDef != unlockdef) continue;
                    unlock = Language.GetString(artifact.nameToken);
                    type = "Artifacts";
                    break;
                }

                foreach (var skill in SkillCatalog._allSkillFamilies)
                {
                    if (unlock != "") break;
                    foreach (var variant in skill.variants)
                    {
                        if (variant.unlockableDef != unlockdef) continue;
                        unlock = Language.GetString(variant.skillDef.skillNameToken);
                        type = "Skills";
                        break;
                    }
                }
            }

            if (achievement.descriptionToken != null) desc = Language.GetString(achievement.descriptionToken);

            try
            {
                var format = string.Format(f, name, type, unlock, desc);

                foreach (var kvp in FormatR2ToWiki) format = format.Replace(kvp.Key, kvp.Value);
                tw.WriteLine(format);
            }
            catch (Exception e)
            {
                Log.Error("Error while exporting challenge: " + e);
            }
        }

        tw.Close();

        var length = new FileInfo(path).Length;
        if (length <= 0) File.Delete(path);
    }
}