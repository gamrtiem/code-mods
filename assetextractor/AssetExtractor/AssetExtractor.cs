using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using Newtonsoft.Json.Utilities;
using R2API;
using R2API.ContentManagement;
using RoR2;
using RoR2.Artifacts;
using RoR2.ContentManagement;
using RoR2.ExpansionManagement;
using RoR2.Projectile;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;
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
            
            foreach (var readOnlyContentPack in ContentManager.allLoadedContentPacks)
            {
                WikiFormat.WikiOutputPath = Path.Combine(Path.Combine(Path.GetDirectoryName(AssetExtractor.Instance.Info.Location) ?? throw new InvalidOperationException(), "wiki"), readOnlyContentPack.identifier);
                
                WikiFormat.FormatItem(readOnlyContentPack);
                WikiFormat.FormatEquipment(readOnlyContentPack);
                WikiFormat.FormatSurvivor(readOnlyContentPack);
                WikiFormat.FormatSkill(readOnlyContentPack);
                WikiFormat.FormatChallenges(readOnlyContentPack);
                WikiFormat.FormatBodies(readOnlyContentPack);
                
                int fCount = Directory.GetFiles(WikiFormat.WikiOutputPath, "*", SearchOption.TopDirectoryOnly).Length;
                if(fCount <= 0) Directory.Delete(WikiFormat.WikiOutputPath);
            }
            
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
        const string WIKI_OUTPUT_BODIES = "Bodies.txt";
        
        public static string WikiOutputPath = Path.Combine(Path.GetDirectoryName(AssetExtractor.Instance.Info.Location) ?? throw new InvalidOperationException(), WIKI_OUTPUT_FOLDER);
        static Dictionary<string, string> FormatR2ToWiki = new Dictionary<string, string>()
        {
            { "</style>", "}}"},
            { "<style=cStack>", "{{Stack|" },
            { "<style=cIsDamage>", "{{Color|d|" },
            { "<style=cIsHealing>", "{{Color|h|" },
            { "<style=cIsUtility>", "{{Color|u|" },
            { "<style=cIsHealth>", "{{Color|hp|" },
            { "<style=cDeath>", "{{Color|hp|" },
            { "<style=cIsVoid>", "{{Color|v|" },
            { "<style=cIsLunar>", "{{Color|lunar|" },
            { "<style=cHumanObjective>", "{{Color|human|"},
            { "<style=cShrine>", "{{Color|boss|" }, // idk about this one
        };
        
        public static void FormatItem(ReadOnlyContentPack readOnlyContentPack)
        {
            string path = Path.Combine(WikiOutputPath, WIKI_OUTPUT_ITEM);
            string f =
                "items[\u0022{0}\u0022] = {{\n\tRarity = \u0022{1}\u0022,\n\tQuote = \u0022{2}\u0022,\n\tDesc = \u0022{3}\u0022,\n\tCategory = {{ {4} }},\n\tUnlock = \u0022{5}\u0022,\n\tCorrupt = \u0022{6}\u0022, \n\tUncorrupt = \u0022{7}\u0022,\n\tID = ,\n\tStats = {{\n\t\t {{\n\t\t\tStat = \u0022\u0022,\n\t\t\tValue = \u0022\u0022,\n\t\t\tStack = \u0022\u0022,\n\t\t\tAdd = \u0022\u0022\n\t\t}}\n\t}},\n\tLocalizationInternalName = \u0022{8}\u0022,\n\t}}";
            if (!Directory.Exists(WikiOutputPath))
            {
                Directory.CreateDirectory(WikiOutputPath);
            }

            TextWriter tw = new StreamWriter(path, false);
            foreach (var def in readOnlyContentPack.itemDefs)
            {
                try{
                    var item = def;
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
            
            long length = new FileInfo(path).Length;
            if (length <= 0) File.Delete(path);
        }

        public static void FormatEquipment(ReadOnlyContentPack readOnlyContentPack)
        {
            string path = Path.Combine(WikiOutputPath, WIKI_OUTPUT_EQUIPMENT);
            string f = "equipments[\u0022{0}\u0022] = {{\n\tRarity = \u0022{1}\u0022,\n\tQuote = \u0022{2}\u0022,\n\tDesc = \u0022{3}\u0022,\n\tUnlock = \u0022{4}\u0022,\n\t ID = ,\n\tLocalizationInternalName = \u0022{5}\u0022,\n\t}}";
            
            TextWriter tw = new StreamWriter(path, false);
            
            foreach (var def in readOnlyContentPack.equipmentDefs)
            {
                try{
                    var equip = def;

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
            
            long length = new FileInfo(path).Length;
            if (length <= 0) File.Delete(path);
        }
        
        public static void FormatSurvivor(ReadOnlyContentPack readOnlyContentPack)
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
            
            foreach (SurvivorDef surv in readOnlyContentPack.survivorDefs)
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
                        var achievement = AchievementManager.GetAchievementDefFromUnlockable(surv.unlockableDef.cachedName);
                        unlocktoken = Language.GetString(achievement.nameToken);
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

                    foreach (var skin in SkinCatalog.allSkinDefs)
                    {
                        var temp2 = WikiOutputPath + @"\skins\";
                        Directory.CreateDirectory(temp2);
                        Log.Debug(surv.bodyPrefab);
                        if (surv.bodyPrefab.TryGetComponent(out ModelLocator modellocator))
                        {
                            if (modellocator.modelTransform.name != null && skin.rootObject != null)
                            {
                                if (modellocator.modelTransform.name == skin.rootObject.name)
                                {
                                    try
                                    {
                                        string filename = "";
                                        var skinlang = Language.GetString(skin.nameToken);
                                        if (skinlang == "Default")
                                        {
                                            filename = "Default " + survName;
                                        }
                                        else
                                        {
                                            filename = Language.GetString(skin.nameToken);
                                        }

                                        exportTexture(skin.icon,
                                            Path.Combine(temp2, filename.Replace(" ", "_") + ".png"));
                                    }
                                    catch
                                    {
                                        Log.Debug(
                                            "erm ,,.,. failed to export surv icon with proper name ,,. trying with tokenm !!");
                                        exportTexture(skin.icon, Path.Combine(temp2, skin.nameToken + ".png"));
                                    }
                                }
                            }
                        }
                    }
                    
                }
                catch (Exception e)
                {
                    Log.Error("Error while exporting skin: " + e);
                }
            }
            tw.Close();
            
            long length = new FileInfo(path).Length;
            if (length <= 0) File.Delete(path);
        }
        

        public static void FormatSkill(ReadOnlyContentPack readOnlyContentPack)
        {
            On.RoR2.BlastAttack.Fire += (orig, self) =>
            {
                Log.Debug("Firing blast proc " + self.procCoefficient);
                return orig(self);
            };
            On.RoR2.Projectile.ProjectileManager.FireProjectileClient += (orig, self, info, client, time) =>
            {
                Log.Debug("Firing projectile" + info.projectilePrefab);
                if(info.projectilePrefab.TryGetComponent<ProjectileController>(out var controller))
                {
                    Log.Debug("Firing projectile proc " + controller.procCoefficient);
                }
                else
                {
                    Log.Error("could not get projectile controller out of prefab ! ");
                }
                orig(self, info, client, time);
            };
            On.EntityStates.GenericBulletBaseState.GenerateBulletAttack += (orig, self, ray) =>
            {
                Log.Debug("Firing bullet proc " + self.procCoefficient);

                return orig(self, ray);
            };
            On.RoR2.BulletAttack.Fire += (orig, self) =>
            {
                Log.Debug("Firing bullet proc " + self.procCoefficient);
                orig(self);
            };
            On.RoR2.GlobalEventManager.OnHitAll += (orig, self, damageInfo, hit) =>
            {
                Log.Debug("hit by proc " + damageInfo.procCoefficient);
                orig(self, damageInfo, hit);
            };

            string path = Path.Combine(WikiOutputPath, WIKI_OUTPUT_SKILLS);

            string f = "skills[\u0022{0}\u0022] = {{\n";
            f += "\tName = \u0022{1}\u0022,\n";
            f += "\tDesc = \u0022{2}\u0022,\n";
            f += "\tSurvivor = \u0022{3}\u0022,\n";
            f += "\tType = \u0022{4}\u0022,\n";
            f += "\tCooldown = \u0022{5}\u0022,\n";
            f += "\tUnlock = \u0022{6}\u0022,\n";
            

            if (!Directory.Exists(WikiOutputPath))
            {
                Directory.CreateDirectory(WikiOutputPath);
            }
            TextWriter tw = new StreamWriter(path, false);
            
            foreach (SurvivorDef surv in readOnlyContentPack.survivorDefs)
            {
                
                try
                {
                    if (surv.bodyPrefab.TryGetComponent(out CharacterBody body))
                    {
                        
                        var scripts = body.GetComponents<GenericSkill>();
                        var skilllocator = body.GetComponent<SkillLocator>();
                        foreach (var skill in scripts)
                        {
                            string type = "Passive";
                            string name = "";
                            string survivor = "";
                            string desc = "";
                            string unlock = "";
                            float cooldown = 0;
                            float proc = 0;
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
                                else
                                {
                                    type = "Passive";
                                }
                                
                                if(variant.skillDef.baseRechargeInterval != 0)
                                {
                                    cooldown = variant.skillDef.baseRechargeInterval;
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

                                if (variant.skillDef.activationState.stateType != null) // must be passive then
                                {
                                    try{
                                        var entitystate = EntityStateCatalog.InstantiateState(EntityStateCatalog.stateTypeToIndex[variant.skillDef.activationState.stateType]);
                                        if (entitystate != null)
                                        {
                                            foreach (var readOnlyContentPack2 in ContentManager.allLoadedContentPacks)
                                            {
                                                Log.Warning("lookin through ,., " + readOnlyContentPack2.identifier);

                                                foreach (var config in readOnlyContentPack2.entityStateConfigurations)
                                                {
                                                    if (!config.name.Contains(variant.skillDef.activationState.stateType.ToString())) continue;
                                                    Log.Warning("found config ! " + config.name);
                                                    foreach (var field in config.serializedFieldsCollection.serializedFields)
                                                    {
                                                        if(field.fieldName.ToLower().Contains("proc")) // wowie ! proc was just sitting there !
                                                        {
                                                            proc = float.Parse(field.fieldValue.stringValue);
                                                            Log.Debug("proc coefficient is " + proc);
                                                            break;
                                                        } else if (field.fieldName.ToLower().Contains("projectile") && proc == 0) // check inside projectile for its proc
                                                        {
                                                            if(field.fieldValue.objectValue != null)
                                                            {
                                                                GameObject projectile = Object.Instantiate(field.fieldValue.objectValue) as GameObject;
                                                                
                                                                if (projectile.TryGetComponent<ProjectileController>(out var controller))
                                                                {
                                                                    proc = controller.procCoefficient;
                                                                    Log.Debug("proc coefficient is " + proc);
                                                                    break;
                                                                }
                                                                else
                                                                {
                                                                    Log.Error("could not get projectile controller out of prefab ! ");
                                                                }
                                                            }
                                                        } else if (field.fieldName.ToLower().Contains("damage") && proc == 0) // default proc, but still has
                                                        {
                                                            proc = 1;
                                                            Log.Debug("no proc found ! defaulting to 1 ,., ");
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    } catch (Exception e)
                                    {
                                        Log.Error("Error while getting proc coefficient: " + e);
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
                                
                                string format = "";
                                string tempformat = f;
                                if(proc == 0) // no proc found, dont add 
                                {
                                    tempformat += "\t}},";
                                    format = Language.GetStringFormatted(tempformat, name, name, desc, survivor, type, cooldown, unlock);
                                }
                                else
                                {
                                    tempformat += "\tProc = \u0022{7}\u0022,\n";
                                    tempformat += "\t}},";
                                    format = Language.GetStringFormatted(tempformat, name, name, desc, survivor, type, cooldown, unlock, proc);
                                }

                                foreach (KeyValuePair<string, string> kvp in FormatR2ToWiki)
                                {
                                    format = format.Replace(kvp.Key, kvp.Value);
                                }
                                tw.WriteLine(format);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error("Error while exporting skill: " + e);
                }
            }
            tw.Close();
            
            long length = new FileInfo(path).Length;
            if (length <= 0) File.Delete(path);
        }
        
        public static void FormatChallenges(ReadOnlyContentPack readOnlyContentPack)
        {
            string path = Path.Combine(WikiOutputPath, WIKI_OUTPUT_CHALLENGES);

            string f = "challenges[\u0022{0}\u0022] = {{\n";
            f += "\tType = \u0022{1}\u0022,\n";
            f += "\tUnlock = {{ \u0022{2}\u0022 }} ,\n";
            f += "\tDesc = \u0022{3}\u0022,\n";
            f += "\t}}";

            if (!Directory.Exists(WikiOutputPath))
            {
                Directory.CreateDirectory(WikiOutputPath);
            }
            TextWriter tw = new StreamWriter(path, false);

            foreach (var unlockdef in readOnlyContentPack.unlockableDefs)
            {
                string name = "";
                string type = "";
                string unlock = "";
                string desc = "";
                var achievement = AchievementManager.GetAchievementDefFromUnlockable(unlockdef.cachedName);
                if (achievement == null) continue;
                if (achievement.nameToken != null)
                {
                    name = Language.GetString(achievement.nameToken);
                    Log.Debug(name);
                }
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
                
                try
                {
                    string format = string.Format(f, name, type, unlock, desc);

                    foreach (KeyValuePair<string, string> kvp in FormatR2ToWiki)
                    {
                        format = format.Replace(kvp.Key, kvp.Value);
                    }
                    tw.WriteLine(format);
                } catch (Exception e)
                {
                    Log.Error("Error while exporting challenge: " + e);
                }
                
            }
            tw.Close();
            
            long length = new FileInfo(path).Length;
            if (length <= 0) File.Delete(path);
        }
        
        public static void FormatBodies(ReadOnlyContentPack readOnlyContentPack)
        {
            string path = Path.Combine(WikiOutputPath, WIKI_OUTPUT_BODIES);

            string f = "monsters[\u0022{0}\u0022] = {{\n";
            f += "\tBaseArmor = \u0022{1}\u0022,\n";
            f += "\tBaseDamage = \u0022{2}\u0022,\n";
            f += "\tBaseHealth = {3},\n";
            f += "\tBaseHealthRegen = {4},\n";
            f += "\tBaseSpeed = {5},\n";
            f += "\tBaseHealth = {6},\n";
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
            
            foreach (var bodyprefab in readOnlyContentPack.bodyPrefabs)
            {
                try
                {
                    string bodyName = "";
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
                    bool breakout = false;
                    foreach (var survdef in SurvivorCatalog.allSurvivorDefs)
                    {
                        if (survdef.bodyPrefab == bodyprefab)
                        {
                            breakout = true;
                            break;
                        }
                    }

                    if (breakout) continue;
                    
                    if (bodyprefab.TryGetComponent(out CharacterBody charbody))
                    {
                        if (charbody.baseNameToken != null)
                        {
                            bodyName = Language.GetString(charbody.baseNameToken);
                        }

                        if (charbody.subtitleNameToken != null)
                        {
                            desc = Language.GetString(charbody.subtitleNameToken);
                        }

                        if (charbody.bodyColor != null)
                        {
                            color = ColorUtility.ToHtmlStringRGB(charbody.bodyColor);
                        }

                        if (charbody.TryGetComponent(out ExpansionRequirementComponent expansion))
                        {
                            if (expansion != null)
                            {
                                unlock = Language.GetString(expansion.name);
                            }
                        }
                        

                        if (charbody.baseNameToken != null && charbody.baseNameToken.EndsWith("_NAME"))
                        {
                            token = charbody.baseNameToken.Remove(charbody.baseNameToken.Length - 5); // remove _NAME
                        }
                        else if (charbody.baseNameToken != null)
                        {
                            token = charbody.baseNameToken;
                        }


                        basehealth = charbody.baseMaxHealth;
                        scalinghealth = charbody.levelMaxHealth;
                        damage = charbody.baseDamage;
                        scalingdamage = charbody.levelDamage;
                        regen = charbody.baseRegen;
                        scalingregen = charbody.levelRegen;
                        speed = charbody.baseMoveSpeed;
                        armor = charbody.baseArmor;
                        

                        if (charbody.TryGetComponent(out CharacterMotor motor))
                        {
                            mass = motor.mass;
                        }

                        // if (charbody.mainEndingEscapeFailureFlavorToken != null)
                        //     mainendingescape = Language.GetString(surv.mainEndingEscapeFailureFlavorToken);
                        // if (surv.outroFlavorToken != null)
                        //     outroFlavor = Language.GetString(surv.outroFlavorToken);
                        // if (body.subtitleNameToken != null)
                        //     umbrasubtitle = Language.GetString(body.subtitleNameToken);

                        // if (surv.unlockableDef)
                        // {
                        //     var achievement =
                        //         AchievementManager.GetAchievementDefFromUnlockable(surv.unlockableDef.cachedName);
                        //     unlocktoken = Language.GetString(achievement.nameToken);
                        // }

                        string format = Language.GetStringFormatted(f, bodyName, bodyName,
                            bodyName.Replace(" ", "_") + ".png", basehealth, scalinghealth, damage, scalingdamage,
                            regen,
                            scalingregen, speed, armor, desc, outroFlavor, mainendingescape, mass, token, "#" + color,
                            unlocktoken, umbrasubtitle);

                        foreach (KeyValuePair<string, string> kvp in FormatR2ToWiki)
                        {
                            format = format.Replace(kvp.Key, kvp.Value);
                        }

                        tw.WriteLine(format);

                        if (!charbody.portraitIcon) continue;

                        var temp = WikiOutputPath + @"\bodies\";
                        Directory.CreateDirectory(temp);
                        try
                        {
                            if (bodyName == "")
                            {
                                Log.Debug("surv name is blank ! using toke n");
                                exportTexture(charbody.portraitIcon,
                                    Path.Combine(temp, token + ".png"));
                            }
                            else
                            {
                                exportTexture(charbody.portraitIcon,
                                    Path.Combine(temp, bodyName.Replace(" ", "_") + ".png"));
                            }
                        }
                        catch
                        {
                            Log.Debug(
                                "erm ,,.,. failed to export surv icon with proper name ,,. trying with tokenm !!");
                            exportTexture(charbody.portraitIcon,
                                Path.Combine(temp, token + ".png"));
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error("Error while exporting body: " + e);
                }
            }
            tw.Close();
            
            long length = new FileInfo(path).Length;
            if (length <= 0) File.Delete(path);
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
