using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using R2API;
using RoR2;
using RoR2.Artifacts;
using RoR2.Skills;
using UnityEngine;
using Path = System.IO.Path;
namespace AssetExtractor
{
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    
    public class AssetExtractor : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "icebro";
        public const string PluginName = "assetextractor";
        public const string PluginVersion = "1.0.0";
        internal static AssetExtractor Instance { get; private set; }

        public void Awake()
        {
            Instance = this;
            Log.Init(Logger);
        }
        

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.F2)) return;
            Log.Info("F2 pressed ,,. extracting !!!!");

            WikiFormat.FormatItem();
            WikiFormat.FormatEquipment();
            WikiFormat.FormatSurvivor();
            WikiFormat.FormatSkill();
            WikiFormat.FormatChallenges();
            
            Log.Info("complete !!!!");
        }
    }


    public static class WikiFormat
    {
        const string WIKI_OUTPUT_FOLDER = "wiki";
        const string WIKI_OUTPUT_ITEM = "Items.txt";
        const string WIKI_OUTPUT_EQUIPMENT = "Equipments.txt";
        const string WIKI_OUTPUT_SURVIVORS = "Survivors.txt";
        const string WIKI_OUTPUT_SKILLS = "Skills.txt";
        const string WIKI_OUTPUT_CHALLENGES = "Challenges.txt";
        
        static string WikiOutputPath = Path.Combine(Path.GetDirectoryName(AssetExtractor.Instance.Info.Location) ?? throw new InvalidOperationException(), WIKI_OUTPUT_FOLDER);
        static Dictionary<string, string> FormatR2ToWiki = new Dictionary<string, string>()
        {
            { "</style>", "}}"},
            { "<style=cStack>", "{{Stack|" },
            { "<style=cIsDamage>", "{{Color|d|" },
            { "<style=cIsHealing>", "{{Color|h|" },
            { "<style=cIsUtility>", "{{{Color|u|" },
            { "<style=cIsHealth>", "{{Color|hp|" },
            { "<style=cDeath>", "{{Color|hp|" },
            { "<style=cIsVoid>", "{{Color|v|" },
            { "<style=cIsLunar>", "{{Color|lunar|" },
            { "<style=cShrine>", "{{Color|boss|" }, // idk about this one
        };
        
        public static void FormatItem()
        {
            string path = Path.Combine(WikiOutputPath, WIKI_OUTPUT_ITEM);
            string f =
                "items[\u0022{0}\u0022] = {{\n\tRarity = \u0022{1}\u0022,\n\tQuote = \u0022{2}\u0022,\n\tDesc = \u0022{3}\u0022,\n\tCategory = {{ {4} }},\n\tUnlock = \u0022{5}\u0022,\n\tCorrupt = \u0022{6}\u0022, \n\tUncorrupt = \u0022{7}\u0022,\n\tID = ,\n\tStats = {{\n\t\t {{\n\t\t\tStat = \u0022\u0022,\n\t\t\tValue = \u0022\u0022,\n\t\t\tStack = \u0022\u0022,\n\t\t\tAdd = \u0022\u0022\n\t\t}}\n\t}},\n\tLocalizationInternalName = \u0022{8}\u0022,\n\t}}";
            if (!Directory.Exists(WikiOutputPath))
            {
                Directory.CreateDirectory(WikiOutputPath);
            }

            TextWriter tw = new StreamWriter(path, false);
            foreach (ItemIndex index in ItemCatalog.allItems)
            {
                try{
                    var item = ItemCatalog.GetItemDef(index);
                    string itemName = "";
                    string pickup = "";
                    string desc = "";
                    string tags = "";
                    string token = "";
                    
                    if (item == null) continue;

                    if (item.nameToken != null)
                    {
                        itemName = Language.GetString(item.nameToken);
                    }

                    ItemTier itemTier = item.tier; // will supposedly never = null
                    string rarity = itemTier switch
                    {
                        ItemTier.Tier1 => "Common",
                        ItemTier.Tier2 => "Uncommon",
                        ItemTier.Tier3 => "Legendary",
                        ItemTier.Lunar => "Lunar",
                        ItemTier.Boss => "Boss",
                        ItemTier.VoidTier1 => "Void",
                        ItemTier.VoidTier2 => "Void",
                        ItemTier.VoidTier3 => "Void",
                        ItemTier.VoidBoss => "Void",
                        ItemTier.NoTier => "Untiered",
                        _ => "Untiered",
                    };
                    //if (itemTier == ItemTier.NoTier) continue;

                    if (item.pickupToken != null)
                    {
                        pickup = Language.GetString(item.pickupToken);
                    }

                    if (item.descriptionToken != null)
                    {
                        desc = Language.GetString(item.descriptionToken);
                    }
                    
                    
                    for (int i = 0; i < item.tags.Length; i++)
                    {
                        tags += "\u0022" + Enum.GetName(typeof(ItemTag), item.tags[i]) + "\u0022";
                        if (i < item.tags.Length - 1) tags += ",";
                    }

                    string unlock = "";
                    if (item.unlockableDef)
                    {
                        AchievementDef achievement =
                            AchievementManager.GetAchievementDefFromUnlockable(item.unlockableDef.cachedName);
                        if (achievement != null && !string.IsNullOrEmpty(achievement.nameToken))
                            unlock = Language.GetString(achievement.nameToken);

                    }

                    if (item.nameToken != null && item.nameToken.EndsWith("_NAME"))
                    {
                        token = item.nameToken.Remove(item.nameToken.Length - 5); // remove _NAME
                    }
                    else if (item.nameToken != null)
                    {
                        token = item.nameToken; 
                    }

                    string format = Language.GetStringFormatted(f, itemName, rarity, pickup, desc, tags, unlock, String.Empty, String.Empty, token);
                    foreach (KeyValuePair<string, string> kvp in FormatR2ToWiki)
                    {
                        format = format.Replace(kvp.Key, kvp.Value);
                    }

                    tw.WriteLine(format);

                    if (!item.pickupIconTexture) continue;
                    var temp = WikiOutputPath + @"\items\";
                    Directory.CreateDirectory(temp);
                    try
                    {
                        if (itemName == "")
                        {
                            Log.Debug("item name is blank ! using toke n");
                            exportTexture(item.pickupIconSprite.texture, Path.Combine(temp, token + ".png"));
                        }
                        else
                        {
                            exportTexture(item.pickupIconSprite.texture, Path.Combine(temp, itemName.Replace(" ", "_") + ".png"));
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Debug(e);
                        Log.Debug("erm ,,.,. failed to export equip icon with proper name ,,. trying with tokenm !!");
                        exportTexture(item.pickupIconSprite.texture, Path.Combine(temp, token + ".png"));
                    }
                }
                catch (Exception e)
                {
                    Log.Error("Error while exporting item: " + e);
                }
            }

            tw.Close();
        }

        public static void FormatEquipment()
        {
            string path = Path.Combine(WikiOutputPath, WIKI_OUTPUT_EQUIPMENT);
            string f = "equipments[\u0022{0}\u0022] = {{\n\tRarity = \u0022{1}\u0022,\n\tQuote = \u0022{2}\u0022,\n\tDesc = \u0022{3}\u0022,\n\tUnlock = \u0022{4}\u0022,\n\t ID = ,\n\tLocalizationInternalName = \u0022{5}\u0022,\n\t}}";
            
            TextWriter tw = new StreamWriter(path, false);
            
            foreach (EquipmentIndex index in EquipmentCatalog.allEquipment)
            {
                try{
                    var equip = EquipmentCatalog.GetEquipmentDef(index);

                    if (equip == null) continue; // you never know 
                    
                    string itemName = "";
                    bool isLunar = equip.isLunar; // this should be fine and if it isnt ill cry 
                    string rarity = isLunar ? "Lunar Equipment" : "Equipment";
                    string pickup = "";
                    string desc = "";
                    string unlock = "";
                    string token = "";
                    
                    if (equip.nameToken != null)
                    {
                        itemName = Language.GetString(equip.nameToken);
                    }
                    
                    if (equip.nameToken != null)
                    {
                        itemName = Language.GetString(equip.nameToken);
                        
                        if (equip.nameToken != null && equip.nameToken.EndsWith("_NAME"))
                        {
                            token = equip.nameToken.Remove(equip.nameToken.Length - 5); // remove _NAME
                        }
                        else if (equip.nameToken != null)
                        {
                            token = equip.nameToken; 
                        }
                    }

                    if (equip.pickupToken != null)
                    {
                        pickup = Language.GetString(equip.pickupToken);
                    }

                    if (equip.descriptionToken != null)
                    {
                        desc = Language.GetString(equip.descriptionToken);
                    }
                    
                    if (equip.unlockableDef)
                    {
                        var nameToken = AchievementManager.GetAchievementDefFromUnlockable(equip.unlockableDef.cachedName)?.nameToken;
                        if (nameToken != null)
                            unlock = Language.GetString(nameToken);
                    }

                    string format = Language.GetStringFormatted(f, itemName, rarity, pickup, desc, unlock, token);

                    foreach (KeyValuePair<string, string> kvp in FormatR2ToWiki)
                    {
                        format = format.Replace(kvp.Key, kvp.Value);
                    }
                    tw.WriteLine(format);

                    if (!equip.pickupIconTexture) continue;

                    var temp = WikiOutputPath + @"\equips\";
                    Directory.CreateDirectory(temp);
                    try
                    {
                        if (itemName == "")
                        {
                            Log.Debug("equip name is blank ! using toke n");
                            exportTexture(equip.pickupIconSprite.texture, Path.Combine(temp, token + ".png"));
                        }
                        else
                        {
                            exportTexture(equip.pickupIconSprite.texture, Path.Combine(temp, itemName.Replace(" ", "_") + ".png"));
                        }
                    }
                    catch
                    {
                        Log.Debug("erm ,,.,. failed to export equip icon with proper name ,,. trying with tokenm !!");
                        exportTexture(equip.pickupIconSprite.texture, Path.Combine(temp, token + ".png"));
                    }
                }
                catch (Exception e)
                {
                    Log.Error("Error while exporting equipment: " + e);
                }
            }
            tw.Close();
        }
        
        public static void FormatSurvivor()
        {
            string path = Path.Combine(WikiOutputPath, WIKI_OUTPUT_SURVIVORS);

            string f = "survivors[\u0022{0}\u0022] = {{\n";
            f += "\tName = \u0022{1}\u0022,\n";
            f += "\tImage = \u0022{2}\u0022,\n";
            f += "\tBaseHealth = {3},\n";
            f += "\tScalingHealth = {4},\n";
            f += "\tBaseDamage = {5},\n";
            f += "\tScalingDamage = {6},\n";
            f += "\tBaseHealthRegen = {7},\n";
            f += "\tScalingHealthRegen = {8},\n";
            f += "\tBaseSpeed = {9},\n";
            f += "\tBaseArmor = {10},\n";
            f += "\tDescription = \u0022{11}\u0022,\n";
            f += "\tUnlock = \u0022{17}\u0022,\n";
            f += "\tUmbra= \u0022{18}\u0022,\n";
            f += "\tPhraseEscape = \u0022{12}\u0022,\n";
            f += "\tPhraseVanish = \u0022{13}\u0022,\n";
            f += "\tClass = \u0022\u0022,\n";
            f += "\tMass = {14},\n";
            f += "\tLocalizationInternalName = \u0022{15}\u0022,\n";
            f += "\tColor = \u0022{16}\u0022,\n";
            f += "\t}}";

            if (!Directory.Exists(WikiOutputPath))
            {
                Directory.CreateDirectory(WikiOutputPath);
            }
            TextWriter tw = new StreamWriter(path, false);
            
            foreach (SurvivorDef surv in SurvivorCatalog.allSurvivorDefs)
            {
                try
                {
                    string survName = "";
                    string desc = "";
                    string unlock = "";
                    string token = "";
                    string color = "";
                    float basehealth = 99999999999999;
                    float scalinghealth = 99999999999999;
                    float damage = 99999999999999;
                    float scalingdamage = 99999999999999;
                    float regen = 99999999999999;
                    float scalingregen = 99999999999999;
                    float speed = 99999999999999;
                    float armor = 99999999999999;
                    float mass = 99999999999999;
                    string mainendingescape = "";
                    string outroFlavor = "";
                    string umbrasubtitle = "";
                    string unlocktoken = "";
                    // I HEART NULL CHECKS !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

                    if (surv.displayNameToken != null)
                    {
                        survName = Language.GetString(surv.displayNameToken);
                    }

                    if (surv.descriptionToken != null)
                    {
                        desc = Language.GetString(surv.descriptionToken);
                    }

                    if (surv.primaryColor != null)
                    {
                        color = ColorUtility.ToHtmlStringRGB(surv.primaryColor);
                    }

                    if (surv.unlockableDef != null)
                    {
                        var nameToken = AchievementManager
                            .GetAchievementDefFromUnlockable(surv.unlockableDef.cachedName)?.nameToken;
                        if (nameToken != null)
                            unlock = Language.GetString(nameToken);
                    }

                    if (surv.displayNameToken != null && surv.displayNameToken.EndsWith("_NAME"))
                    {
                        token = surv.displayNameToken.Remove(surv.displayNameToken.Length - 5); // remove _NAME
                    }
                    else if (surv.displayNameToken != null)
                    {
                        token = surv.displayNameToken;
                    }

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

                    if (surv.bodyPrefab.TryGetComponent(out CharacterMotor motor))
                    {
                        mass = motor.mass;
                    }

                    if (surv.mainEndingEscapeFailureFlavorToken != null)
                        mainendingescape = Language.GetString(surv.mainEndingEscapeFailureFlavorToken);
                    if (surv.outroFlavorToken != null)
                        outroFlavor = Language.GetString(surv.outroFlavorToken);
                    if (body.subtitleNameToken != null)
                        umbrasubtitle = Language.GetString(body.subtitleNameToken);

                    if (surv.unlockableDef)
                    {
                        unlocktoken = Language.GetString(surv.unlockableDef.nameToken);
                    }

                    string format = Language.GetStringFormatted(f, survName, survName,
                        survName.Replace(" ", "_") + ".png", basehealth, scalinghealth, damage, scalingdamage, regen,
                        scalingregen, speed, armor, desc, outroFlavor, mainendingescape, mass, token, "#" + color,
                        unlocktoken, umbrasubtitle);

                    foreach (KeyValuePair<string, string> kvp in FormatR2ToWiki)
                    {
                        format = format.Replace(kvp.Key, kvp.Value);
                    }

                    tw.WriteLine(format);

                    if (!SurvivorCatalog.GetSurvivorPortrait(surv.survivorIndex)) continue;

                    var temp = WikiOutputPath + @"\survivors\";
                    Directory.CreateDirectory(temp);
                    try
                    {
                        if (survName == "")
                        {
                            Log.Debug("surv name is blank ! using toke n");
                            exportTexture(SurvivorCatalog.GetSurvivorPortrait(surv.survivorIndex), Path.Combine(temp, token + ".png"));
                        }
                        else
                        {
                            exportTexture(SurvivorCatalog.GetSurvivorPortrait(surv.survivorIndex), Path.Combine(temp, survName.Replace(" ", "_") + ".png"));
                        }
                    }
                    catch
                    {
                        Log.Debug("erm ,,.,. failed to export surv icon with proper name ,,. trying with tokenm !!");
                        exportTexture(SurvivorCatalog.GetSurvivorPortrait(surv.survivorIndex), Path.Combine(temp, token + ".png"));
                    }
                    
                }
                catch (Exception e)
                {
                    Log.Error("Error while exporting survivor: " + e);
                }
            }
            tw.Close();
        }

        public static void FormatSkill()
        {
            string path = Path.Combine(WikiOutputPath, WIKI_OUTPUT_SKILLS);

            string f = "skills[\u0022{0}\u0022] = {{\n";
            f += "\tName = \u0022{1}\u0022,\n";
            f += "\tDesc = \u0022{2}\u0022,\n";
            f += "\tSurvivor = \u0022{3}\u0022,\n";
            f += "\tType = \u0022{4}\u0022,\n";
            f += "\tUnlock = \u0022{5}\u0022,\n";
            f += "\t}}";

            if (!Directory.Exists(WikiOutputPath))
            {
                Directory.CreateDirectory(WikiOutputPath);
            }
            TextWriter tw = new StreamWriter(path, false);
            
            foreach (SurvivorDef surv in SurvivorCatalog.allSurvivorDefs)
            {
                string type = "";
                string name = "";
                string survivor = "";
                string desc = "";
                string unlock = "";
                try
                {
                    if (surv.bodyPrefab.TryGetComponent(out CharacterBody body))
                    {
                        var scripts = body.GetComponents<GenericSkill>();
                        var skilllocator = body.GetComponent<SkillLocator>();
                        foreach (var skill in scripts)
                        {
                            Log.Debug(skill.skillFamily.ToString());
                            survivor = Language.GetString(surv.displayNameToken);
                             
                            foreach (var variant in skill.skillFamily.variants)
                            {

                                if (variant.skillDef.skillNameToken != null)
                                {
                                    name = Language.GetString(variant.skillDef.skillNameToken);
                                    Log.Debug(variant.skillDef.skillNameToken);
                                }

                                if (skill == skilllocator.primary)
                                {
                                    type = "Primary";
                                } else if (skill == skilllocator.secondary)
                                {
                                    type = "Secondary";
                                } else if (skill == skilllocator.utility)
                                {
                                    type = "Utility";
                                } else if (skill == skilllocator.special)
                                {
                                    type = "Special";
                                }
                                
                                if (variant.skillDef.skillDescriptionToken != null)
                                {
                                    desc = Language.GetString(variant.skillDef.skillDescriptionToken);
                                }
                                if (variant.unlockableDef != null)
                                {
                                    var unlockable = AchievementManager.GetAchievementDefFromUnlockable(variant.unlockableDef.cachedName);
                                    if (unlockable != null)
                                    {
                                        unlock = Language.GetString(unlockable.nameToken);
                                    
                                        Log.Debug(unlockable.nameToken);
                                    }
                                }
                                
                                if (!variant.skillDef.icon.texture) continue;
                                
                                var temp = WikiOutputPath + @"\skills\";
                                Directory.CreateDirectory(temp);
                                try
                                {
                                    if (name == "")
                                    {
                                        Log.Debug("skill name is blank ! using toke n");
                                        exportTexture(variant.skillDef.icon, Path.Combine(temp, variant.skillDef.skillNameToken + ".png"));
                                    }
                                    else
                                    {
                                        exportTexture(variant.skillDef.icon, Path.Combine(temp, name.Replace(" ", "_") + ".png"));
                                    }
                                }
                                catch
                                {
                                    Log.Debug("erm ,,.,. failed to export skill icon with proper name ,,. trying with tokenm !!");
                                    exportTexture(variant.skillDef.icon, Path.Combine(temp, variant.skillDef.skillNameToken + ".png"));
                                }
                            }
                            string format = Language.GetStringFormatted(f, name, name, desc, survivor, type, unlock);

                            foreach (KeyValuePair<string, string> kvp in FormatR2ToWiki)
                            {
                                format = format.Replace(kvp.Key, kvp.Value);
                            }
                            tw.WriteLine(format);

                            
                        }
                    }
                    
                }
                catch (Exception e)
                {
                    Log.Error("Error while exporting skill: " + e);
                }
            }
            tw.Close();
        }
        
        public static void FormatChallenges()
        {
            string path = Path.Combine(WikiOutputPath, WIKI_OUTPUT_CHALLENGES);

            string f = "challenges[\u0022{0}\u0022] = {{\n";
            f += "\tType = \u0022{1}\u0022,\n";
            f += "\tUnlock = \u0022{2}\u0022,\n";
            f += "\tDesc = \u0022{3}\u0022,\n";
            f += "\t}}";

            if (!Directory.Exists(WikiOutputPath))
            {
                Directory.CreateDirectory(WikiOutputPath);
            }
            TextWriter tw = new StreamWriter(path, false);

            foreach (var achievement in AchievementManager.achievementDefs)
            {
                string name = "";
                string type = "";
                string unlock = "";
                string desc = "";
                if (achievement.nameToken != null)
                {
                    name = Language.GetString(achievement.nameToken);
                    Log.Debug(name);
                }
                if (achievement.unlockableRewardIdentifier != null)
                {
                    var unlockdef = UnlockableCatalog.GetUnlockableDef(achievement.unlockableRewardIdentifier);
                    Log.Debug(unlockdef);
                    
                    foreach (var item in ItemCatalog.allItemDefs)
                    {
                        if (item.unlockableDef != unlockdef) continue;
                        unlock = Language.GetString(item.nameToken);
                        type = "Items";
                        break;
                    }
                    foreach (var surv in SurvivorCatalog.allSurvivorDefs)
                    {
                        if(unlock != "") break;
                        if (surv.unlockableDef != unlockdef) continue;
                        unlock = Language.GetString(surv.displayNameToken);
                        type = "Survivors";
                        break;
                    }
                    foreach (var equip in EquipmentCatalog.equipmentDefs)
                    {
                        if(unlock != "") break;
                        if (equip.unlockableDef != unlockdef) continue;
                        unlock = Language.GetString(equip.nameToken);
                        type = "Equipment";
                        break;
                    }
                    foreach (var skin in SkinCatalog.allSkinDefs)
                    {
                        if(unlock != "") break;
                        if (skin.unlockableDef != unlockdef) continue;
                        unlock = Language.GetString(skin.nameToken);
                        type = "Skins";
                        break;
                    }
                    foreach (var artifact in ArtifactCatalog.artifactDefs)
                    {
                        if(unlock != "") break;
                        if (artifact.unlockableDef != unlockdef) continue;
                        unlock = Language.GetString(artifact.nameToken);
                        type = "Artifacts";
                        break;
                    }
                    foreach (var skill in SkillCatalog._allSkillFamilies)
                    {
                        if(unlock != "") break;
                        foreach (var variant in skill.variants)
                        {
                            if (variant.unlockableDef != unlockdef) continue;
                            unlock = Language.GetString(variant.skillDef.skillNameToken);
                            type = "Skills";
                            break;
                        }
                    }

                }
                if (achievement.descriptionToken != null)
                {
                    desc = Language.GetString(achievement.descriptionToken);
                }
                    
                string format = Language.GetStringFormatted(f, name, type, unlock, desc);

                foreach (KeyValuePair<string, string> kvp in FormatR2ToWiki)
                {
                    format = format.Replace(kvp.Key, kvp.Value);
                }
                tw.WriteLine(format);
            }
            tw.Close();
        }

        public static void exportTexture(Texture texture, String path)
        {
            RenderTexture tmp = new RenderTexture(texture.width, texture.height, 32);
            tmp.name = "Whatever";
            tmp.enableRandomWrite = true;
            tmp.Create();

            // Blit the pixels on texture to the RenderTexture
            Graphics.Blit(texture, tmp);
                
            // Backup the currently set RenderTexture
            RenderTexture previous = RenderTexture.active;
                
            // Set the current RenderTexture to the temporary one we created
            RenderTexture.active = tmp;
                
            // Create a new readable Texture2D to copy the pixels to it
            Texture2D myTexture2D = new Texture2D(texture.width, texture.height);
                
            // Copy the pixels from the RenderTexture to the new Texture
            myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            myTexture2D.Apply();
                
            // Reset the active RenderTexture
            RenderTexture.active = previous;
                
            // Release the temporary RenderTexture
            //RenderTexture.ReleaseTemporary(tmp);
            System.IO.File.WriteAllBytes(path, myTexture2D.EncodeToPNG());
        }
        
        public static void exportTexture(Sprite sprite, String path)
        {
            RenderTexture tmp = new RenderTexture(sprite.texture.width, sprite.texture.height, 32);
            tmp.name = "Whatever";
            tmp.enableRandomWrite = true;
            tmp.Create();

            // Blit the pixels on texture to the RenderTexture
            Graphics.Blit(sprite.texture, tmp);
                
            // Backup the currently set RenderTexture
            RenderTexture previous = RenderTexture.active;
                
            // Set the current RenderTexture to the temporary one we created
            RenderTexture.active = tmp;
                
            // Create a new readable Texture2D to copy the pixels to it
            Texture2D myTexture2D = new Texture2D(sprite.texture.width, sprite.texture.height);
                
            // Copy the pixels from the RenderTexture to the new Texture
            myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            myTexture2D.Apply();
                
            // Reset the active RenderTexture
            RenderTexture.active = previous;
                
            // Release the temporary RenderTexture
           // RenderTexture.ReleaseTemporary(tmp);
            
            var croppedTexture = new Texture2D( (int)sprite.rect.width, (int)sprite.rect.height );
            var pixels = myTexture2D.GetPixels(  (int)sprite.textureRect.x, 
                (int)sprite.textureRect.y, 
                (int)sprite.textureRect.width, 
                (int)sprite.textureRect.height );
            croppedTexture.SetPixels( pixels );
            croppedTexture.Apply();
            
            System.IO.File.WriteAllBytes(path, croppedTexture.EncodeToPNG());
        }
        
    }
    
    
}
