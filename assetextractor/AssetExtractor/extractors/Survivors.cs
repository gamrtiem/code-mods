using System;
using System.IO;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine;
using Path = System.IO.Path;

namespace AssetExtractor;

public partial class WikiFormat
{
    public static void FormatSurvivor(ReadOnlyContentPack readOnlyContentPack)
    {
        var path = Path.Combine(WikiOutputPath, WIKI_OUTPUT_SURVIVORS);

        if (!Directory.Exists(WikiOutputPath)) Directory.CreateDirectory(WikiOutputPath);
        TextWriter tw = new StreamWriter(path, WikiAppend);

        foreach (SurvivorDef surv in readOnlyContentPack.survivorDefs)
            try
            {
                string f = "survivors[\"{0}\"] = {{\n";
                f += "\tName = \"{1}\",\n";
                f += "\tImage = \"{2}\",\n";
                f += "\tBaseHealth = {3},\n";
                f += "\tScalingHealth = {4},\n";
                f += "\tBaseDamage = {5},\n";
                f += "\tScalingDamage = {6},\n";
                f += "\tBaseHealthRegen = {7},\n";
                f += "\tScalingHealthRegen = {8},\n";
                f += "\tBaseSpeed = {9},\n";
                f += "\tBaseArmor = {10},\n";
                f += "\tDescription = \"{11}\",\n";
                f += "\tUmbra = \"{17}\",\n";
                f += "\tPhraseEscape = \"{12}\",\n";
                f += "\tPhraseVanish = \"{13}\",\n";
                f += "\tMass = {14},\n";
                f += "\tLocalizationInternalName = \"{15}\",\n";
                f += "\tColor = \"{16}\",\n";
                
                var survName = "";
                var desc = "";
                var unlock = "";
                var token = "";
                var color = "";
                float basehealth = 99999999999999;
                float scalinghealth = 99999999999999;
                float damage = 99999999999999;
                float scalingdamage = 99999999999999;
                float regen = 99999999999999;
                float scalingregen = 99999999999999;
                float speed = 99999999999999;
                float armor = 99999999999999;
                float mass = 99999999999999;
                var mainendingescape = "";
                var outroFlavor = "";
                var umbrasubtitle = "";
                var unlocktoken = "";

                // I HEART NULL CHECKS !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                if (surv.displayNameToken != null) survName = Language.GetString(surv.displayNameToken);

                if (surv.descriptionToken != null) desc = Language.GetString(surv.descriptionToken);

                if (surv.primaryColor != null) color = ColorUtility.ToHtmlStringRGB(surv.primaryColor);

                if (surv.unlockableDef != null)
                {
                    string nameToken = AchievementManager
                        .GetAchievementDefFromUnlockable(surv.unlockableDef.cachedName)?.nameToken;
                    if (nameToken != null)
                        unlock = Language.GetString(nameToken);
                }

                if (surv.displayNameToken != null && surv.displayNameToken.EndsWith("_NAME"))
                    token = surv.displayNameToken.Remove(surv.displayNameToken.Length - 5); // remove _NAME
                else if (surv.displayNameToken != null) token = surv.displayNameToken;

                if (Language.english.TokenIsRegistered(surv.displayNameToken.Replace("_NAME", "_LORE")))
                    loredefs.Add("Survivor " + surv.displayNameToken + " " +
                                 surv.displayNameToken.Replace("_NAME", "_LORE"));

                if (surv.bodyPrefab.TryGetComponent(out CharacterBody body))
                {
                    basehealth = body.baseMaxHealth;
                    scalinghealth = body.levelMaxHealth;
                    damage = body.baseDamage;
                    scalingdamage = body.levelDamage;
                    regen = body.baseRegen;
                    scalingregen = body.levelRegen;
                    speed = body.baseMoveSpeed;
                    armor = body.baseArmor;
                }

                if (surv.bodyPrefab.TryGetComponent(out CharacterMotor motor)) mass = motor.mass;

                if (surv.mainEndingEscapeFailureFlavorToken != null)
                    mainendingescape = Language.GetString(surv.mainEndingEscapeFailureFlavorToken);
                if (surv.outroFlavorToken != null)
                    outroFlavor = Language.GetString(surv.outroFlavorToken);
                if (body.subtitleNameToken != null)
                    umbrasubtitle = Language.GetString(body.subtitleNameToken);

                if (surv.unlockableDef)
                {
                    var achievement = AchievementManager.GetAchievementDefFromUnlockable(surv.unlockableDef.cachedName);
                    unlocktoken = Language.GetString(achievement.nameToken);
                    
                    f += "\tUnlock = \"" + unlocktoken + "\",\n";
                }

                if (surv.GetRequiredExpansion() != null)
                {
                    f += "\tExpansion = \"" + acronymHelper(Language.GetString(surv.GetRequiredExpansion().nameToken), false) + "\",\n";
                }
                
                f += "\t}}";
                var format = Language.GetStringFormatted(f, survName, survName,
                    survName.Replace(" ", "_") + WikiModname + ".png", basehealth, scalinghealth, damage, scalingdamage,
                    regen,
                    scalingregen, speed, armor, desc, outroFlavor, mainendingescape, mass, token, "#" + color, umbrasubtitle);

                foreach (var kvp in FormatR2ToWiki)
                    format = format.Replace(kvp.Key, kvp.Value);

                tw.WriteLine(format);

                if (!SurvivorCatalog.GetSurvivorPortrait(surv.survivorIndex)) continue;

                var temp = WikiOutputPath + @"\survivors\";
                Directory.CreateDirectory(temp);
                try
                {
                    if (survName == "")
                    {
                        Log.Debug("surv name is blank ! using toke n");
                        exportTexture(SurvivorCatalog.GetSurvivorPortrait(surv.survivorIndex),
                            Path.Combine(temp, token + WikiModname + ".png"));
                    }
                    else
                    {
                        exportTexture(SurvivorCatalog.GetSurvivorPortrait(surv.survivorIndex),
                            Path.Combine(temp, survName.Replace(" ", "_") + WikiModname + ".png"));
                    }
                }
                catch
                {
                    Log.Debug("erm ,,.,. failed to export surv icon with proper name ,,. trying with tokenm !! " +
                              survName);
                    exportTexture(SurvivorCatalog.GetSurvivorPortrait(surv.survivorIndex),
                        Path.Combine(temp, token + WikiModname + ".png"));
                }

                foreach (var skin in SkinCatalog.allSkinDefs)
                {
                    var temp2 = WikiOutputPath + @"\skins\";
                    Directory.CreateDirectory(temp2);
                    //Log.Debug(surv.bodyPrefab);
                    if (!surv.bodyPrefab.TryGetComponent(out ModelLocator modellocator)) continue;
                    if (modellocator.modelTransform.name == null || skin.rootObject == null) continue;
                    if (modellocator.modelTransform.name != skin.rootObject.name) continue;
                    
                    try
                    {
                        string filename;
                        string skinlang = Language.GetString(skin.nameToken);
                        if (skinlang == "Default")
                        {
                            filename = "Default " + survName;
                        }
                        else
                        {
                            filename = Language.GetString(skin.nameToken);
                        }

                        if (filename == "" || skin.nameToken == "")
                        {
                            filename = skin.name;
                        }
                        exportTexture(skin.icon, Path.Combine(temp2, filename.Replace(" ", "_") + WikiModname + ".png"));
                    }
                    catch
                    {
                        Log.Debug("erm ,,.,. failed to export skin icon with proper name ,,. trying with tokenm !! " + Language.GetString(skin.nameToken)); 
                        exportTexture(skin.icon, Path.Combine(temp2, skin.nameToken + WikiModname + ".png"));
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error while exporting skin: " + e);
            }

        tw.Close();

        var length = new FileInfo(path).Length;
        if (length <= 0) File.Delete(path);
    }
}