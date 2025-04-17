using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using R2API;
using RoR2;
using RoR2.Artifacts;
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
        }
    }


    public static class WikiFormat
    {
        const string WIKI_OUTPUT_FOLDER = "wiki";
        const string WIKI_OUTPUT_ITEM = "Items.txt";
        const string WIKI_OUTPUT_EQUIPMENT = "Equipments.txt";
        const string WIKI_OUTPUT_SURVIVORS = "Survivors.txt";
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
                "items[\u201C{0}\u201C] = {{\n\tRarity = \u201C{1}\u201C,\n\tQuote = \u201C{2}\u201C,\n\tDesc = \u201C{3}\u201C,\n\tCategory = {{ {4} }},\n\tUnlock = \u201C{5}\u201C,\n\tCorrupt = \u201C{6}\u201C, \n\tUncorrupt = \u201C{7}\u201C,\n\tID = ,\n\tStats = {{\n\t\t {{\n\t\t\tStat = \u201C\u201C,\n\t\t\tValue = \u201C\u201C,\n\t\t\tStack = \u201C\u201C,\n\t\t\tAdd = \u201C\u201C\n\t\t}}\n\t}},\n\tLocalizationInternalName = \u201C{8}\u201C,\n\t}}";
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
                        tags += "\u201C" + Enum.GetName(typeof(ItemTag), item.tags[i]) + "\u201C";
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
                        exportTexture(item.pickupIconSprite.texture, Path.Combine(temp, itemName.Replace(" ", "_") + ".png"));
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
            string f = "equipments[\u201C{0}\u201C] = {{\n\tRarity = \u201C{1}\u201C,\n\tQuote = \u201C{2}\u201C,\n\tDesc = \u201C{3}\u201C,\n\tUnlock = \u201C{4}\u201C,\n\t ID = ,\n\tLocalizationInternalName = \u201C{5}\u201C,\n\t}}";
            
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
                        exportTexture(equip.pickupIconSprite.texture, Path.Combine(temp, itemName.Replace(" ", "_") + ".png"));
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

            string f = "survivors[\u201C{0}\u201C] = {{\n";
            f += "\tName = \u201C{1}\u201C,\n";
            f += "\tImage = \u201C{2}\u201C,\n";
            f += "\tBaseHealth = {3},\n";
            f += "\tScalingHealth = {4},\n";
            f += "\tBaseDamage = {5},\n";
            f += "\tScalingDamage = {6},\n";
            f += "\tBaseHealthRegen = {7},\n";
            f += "\tScalingHealthRegen = {8},\n";
            f += "\tBaseSpeed = {9},\n";
            f += "\tBaseArmor = {10},\n";
            f += "\tDescription = \u201C{11}\u201C,\n";
            f += "\tUnlock = \u201C{17}\u201C,\n";
            f += "\tUmbra= \u201C{18}\u201C,\n";
            f += "\tPhraseEscape = \u201C{12}\u201C,\n";
            f += "\tPhraseVanish = \u201C{13}\u201C,\n";
            f += "\tClass = \u201C\u201C,\n";
            f += "\tMass = {14},\n";
            f += "\tLocalizationInternalName = \u201C{15}\u201C,\n";
            f += "\tColor = \u201C{16}\u201C,\n";
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
                        exportTexture(SurvivorCatalog.GetSurvivorPortrait(surv.survivorIndex), Path.Combine(temp, survName.Replace(" ", "_") + ".png"));
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
            RenderTexture.ReleaseTemporary(tmp);
            System.IO.File.WriteAllBytes(path, myTexture2D.EncodeToPNG());
            
            
        }
    }
}