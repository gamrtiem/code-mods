using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using R2API;
using RoR2;
using UnityEngine;
using Path = System.IO.Path;

namespace AssetExtractor
{
    // This is an example plugin that can be put in
// BepInEx/plugins/AssetExtractor/AssetExtractor.dll to test out.
// It's a small plugin that adds a relatively simple item to the game,
// and gives you that item whenever you press F2.

// This attribute specifies that we have a dependency on a given BepInEx Plugin,
// We need the R2API ItemAPI dependency because we are using for adding our item to the game.
// You don't need this if you're not using R2API in your plugin,
// it's just to tell BepInEx to initialize R2API before this plugin so it's safe to use R2API.
    [BepInDependency(ItemAPI.PluginGUID)]

// This one is because we use a .language file for language tokens
// More info in https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Localization/
    [BepInDependency(LanguageAPI.PluginGUID)]

// This attribute is required, and lists metadata for your plugin.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

// This is the main declaration of our plugin class.
// BepInEx searches for all classes inheriting from BaseUnityPlugin to initialize on startup.
// BaseUnityPlugin itself inherits from MonoBehaviour,
// so you can use this as a reference for what you can declare and use in your plugin class
// More information in the Unity Docs: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    public class AssetExtractor : BaseUnityPlugin
    {
        // The Plugin GUID should be a unique ID for this plugin,
        // which is human readable (as it is used in places like the config).
        // If we see this PluginGUID as it is on thunderstore,
        // we will deprecate this mod.
        // Change the PluginAuthor and the PluginName !
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "icebro";
        public const string PluginName = "assetextractor";
        public const string PluginVersion = "1.0.0";
        internal static AssetExtractor Instance { get; private set; }
        // We need our item definition to persist through our functions, and therefore make it a class field.

        // The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            Instance = this;
            // Init our logging class so that we can properly log for debugging
            Log.Init(Logger);
        }
        

        // The Update() method is run on every frame of the game.
        private void Update()
        {
            // This if statement checks if the player has currently pressed F2.
            if (!Input.GetKeyDown(KeyCode.F2)) return;
            Log.Info("F2 pressed ,,. extracting !!!!");

            WikiFormat.FormatItem(args: new ConCommandArgs());
            WikiFormat.FormatEquipment(args: new ConCommandArgs());
            WikiFormat.FormatSurvivor(args: new ConCommandArgs());
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

        [ConCommand(commandName = "wiki_item", flags = ConVarFlags.None,
            helpText = "Print Starstorm 2 item information to a Wiki.GG format.")]
        public static void FormatItem(ConCommandArgs args)
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

                var item = ItemCatalog.GetItemDef(index);

                if (item.nameToken == null)
                    continue;
                string itemName = Language.GetString(item.nameToken);

                ItemTier itemTier = item.tier;
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
                if (itemTier == ItemTier.NoTier) continue;
                if (item.pickupToken == null)
                    continue;
                if (item.descriptionToken == null)
                    continue;
                string pickup = Language.GetString(item.pickupToken);
                string desc = Language.GetString(item.descriptionToken);
                string tags = "";
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

                string token = item.nameToken.Remove(item.nameToken.Length - 5); // remove _NAME

                string format = Language.GetStringFormatted(f, itemName, rarity, pickup, desc, tags, unlock,
                    String.Empty, String.Empty, token);
                foreach (KeyValuePair<string, string> kvp in FormatR2ToWiki)
                {
                    format = format.Replace(kvp.Key, kvp.Value);
                }

                //if(item.cont)
                tw.WriteLine(format);

                if (!item.pickupIconTexture) continue;
                var temp = WikiOutputPath + @"\items\";
                Directory.CreateDirectory(temp);
                exportTexture(item.pickupIconSprite.texture, Path.Combine(temp, token + ".png"));
            }

            tw.Close();
        }

        [ConCommand(commandName = "wiki_equipment", flags = ConVarFlags.None, helpText = "Print Starstorm 2 equipment information to a Wiki.GG format.")]
        public static void FormatEquipment(ConCommandArgs args)
        {
            string path = Path.Combine(WikiOutputPath, WIKI_OUTPUT_EQUIPMENT);
            string f = "equipments[\u201C{0}\u201C] = {{\n\tRarity = \u201C{1}\u201C,\n\tQuote = \u201C{2}\u201C,\n\tDesc = \u201C{3}\u201C,\n\tUnlock = \u201C{4}\u201C,\n\t ID = ,\n\tLocalizationInternalName = \u201C{5}\u201C,\n\t}}";
            if (!Directory.Exists(WikiOutputPath))
            {
                Directory.CreateDirectory(WikiOutputPath);
            }
            TextWriter tw = new StreamWriter(path, false);
            foreach (EquipmentIndex index in EquipmentCatalog.allEquipment)
            {

                var equip = EquipmentCatalog.GetEquipmentDef(index);
                string itemName = Language.GetString(equip.nameToken);
                bool isLunar = equip.isLunar;
                string rarity = isLunar ? "Lunar Equipment" : "Equipment";
                string pickup = Language.GetString(equip.pickupToken);
                string desc = Language.GetString(equip.descriptionToken);
                string unlock = "";
                if (equip.unlockableDef)
                {
                    var nameToken = AchievementManager.GetAchievementDefFromUnlockable(equip.unlockableDef.cachedName)?.nameToken;
                    if (nameToken != null)
                        unlock = Language.GetString(nameToken);
                }

                string token = equip.nameToken.Remove(equip.nameToken.Length - 5); // remove _NAME

                string format = Language.GetStringFormatted(f, itemName, rarity, pickup, desc, unlock, token);
                foreach (KeyValuePair<string, string> kvp in FormatR2ToWiki)
                {
                    format = format.Replace(kvp.Key, kvp.Value);
                }
                tw.WriteLine(format);
                    
                if (!equip.pickupIconTexture) continue;
                var temp = WikiOutputPath + @"\equips\";
                Directory.CreateDirectory(temp);
                exportTexture(equip.pickupIconSprite.texture, Path.Combine(temp, token + ".png"));
            }
            tw.Close();
        }
        
        [ConCommand(commandName = "wiki_equipment", flags = ConVarFlags.None, helpText = "Print Starstorm 2 equipment information to a Wiki.GG format.")]
        public static void FormatSurvivor(ConCommandArgs args)
        {
            string path = Path.Combine(WikiOutputPath, WIKI_OUTPUT_SURVIVORS);
            string f = "equipments[\u201C{0}\u201C] = {{\n\tRarity = \u201C{1}\u201C,\n\tQuote = \u201C{2}\u201C,\n\tDesc = \u201C{3}\u201C,\n\tUnlock = \u201C{4}\u201C,\n\t ID = ,\n\tLocalizationInternalName = \u201C{5}\u201C,\n\t}}";
            if (!Directory.Exists(WikiOutputPath))
            {
                Directory.CreateDirectory(WikiOutputPath);
            }
            TextWriter tw = new StreamWriter(path, false);
            foreach (SurvivorDef surv in SurvivorCatalog.allSurvivorDefs)
            {

                string survName = Language.GetString(surv.displayNameToken);
                string pickup = Language.GetString(surv.descriptionToken);
                string desc = Language.GetString(surv.descriptionToken);
                string unlock = "";
                if (surv.unlockableDef)
                {
                    var nameToken = AchievementManager.GetAchievementDefFromUnlockable(surv.unlockableDef.cachedName)?.nameToken;
                    if (nameToken != null)
                        unlock = Language.GetString(nameToken);
                }

                string token = surv.displayNameToken.Remove(surv.displayNameToken.Length - 5); // remove _NAME

                string format = Language.GetStringFormatted(f, survName, "", pickup, desc, unlock, token);
                foreach (KeyValuePair<string, string> kvp in FormatR2ToWiki)
                {
                    format = format.Replace(kvp.Key, kvp.Value);
                }
                tw.WriteLine(format);
                
                if (!SurvivorCatalog.GetSurvivorPortrait(surv.survivorIndex)) continue;
                var temp = WikiOutputPath + @"\survivors\";
                Directory.CreateDirectory(temp);
                exportTexture(SurvivorCatalog.GetSurvivorPortrait(surv.survivorIndex), Path.Combine(temp, token + ".png"));
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