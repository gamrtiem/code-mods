using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using EntityStates.InfiniteTowerSafeWard;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using On.RoR2.CharacterSpeech;
using R2API;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Items;
using ShaderSwapper;
using SolusHeartReward;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using CharacterSpeechController = RoR2.CharacterSpeech.CharacterSpeechController;
using ClassicStageInfo = On.RoR2.ClassicStageInfo;
using CombatDirector = On.RoR2.CombatDirector;
using DirectorCard = On.RoR2.DirectorCard;
using PluginInfo = BepInEx.PluginInfo;
using RoachController = On.RoR2.RoachController;

namespace SolusHeartReward
{
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    public class SolusHeartReward : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "icebro";
        public const string PluginName = "heartofthecollective";
        public const string PluginVersion = "1.0.0";

        private static ConfigEntry<string> blacklist;
        public static ConfigEntry<bool> logDebug;
        public static ConfigEntry<bool> silenceSPEX;
        public static ConfigEntry<bool> enableProc;
        public static ConfigEntry<int> procChance;
        public static ConfigEntry<int> procDamage;
        private static ConfigEntry<string> descFormat;
        private static ConfigEntry<string> procFormat;
        
        public static GameObject impactEffectPrefab;
        public static ItemDef nullHeart;
        public static List<string> solusFamilyBodyNames = [];
        public static string[] blacklistArray;
        private string assetbundledir;

        public void Awake()
        {
            Log.Init(Logger);

            assetbundledir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), "solusheartrewardassets");
            AssetBundle assetbundle = AssetBundle.LoadFromFileAsync(assetbundledir).assetBundle;
            StartCoroutine(assetbundle.UpgradeStubbedShadersAsync());
            
            foreach (Material material in assetbundle.LoadAllAssets<Material>())
            {
                //replace trans scrolling texture since no dummy shader for that and add the in game textures so theyre not in the asset bundle and make it take like 5mb !!!
                if(material.name == "matTransparentChip")
                {
                    material.shader = Addressables.LoadAssetAsync<Shader>("RoR2/DLC3/TransparentTextureScrolling.shader").WaitForCompletion();
                    material.SetTexture("_MainTex", Addressables.LoadAssetAsync<Texture2D>("RoR2/DLC3/texSolusWireGridBoss.tga").WaitForCompletion());
                    material.SetTexture("_RemapTex", Addressables.LoadAssetAsync<Texture2D>("RoR2/DLC3/texBossWire.png").WaitForCompletion());
                    material.SetTexture("_ScrollTex", Addressables.LoadAssetAsync<Texture2D>("RoR2/Base/Common/TiledTextures/texDepth2.jpg").WaitForCompletion());
                    Log.Debug("swapped chip material shader");
                }
                
                if(material.name == "matSolusHeart")
                {
                    material.SetTexture("_Cloud1Tex", Addressables.LoadAssetAsync<Texture2D>("RoR2/DLC3/Drone Tech/texNanoPistolAOE_1c.png").WaitForCompletion());
                    material.SetTexture("_Cloud2Tex", Addressables.LoadAssetAsync<Texture2D>("RoR2/DLC3/Drone Tech/texNanoPistolAOE_1d.png").WaitForCompletion());
                    material.SetTexture("_RemapTex", Addressables.LoadAssetAsync<Texture2D>("RoR2/Base/Common/ColorRamps/texRampTritone3.png").WaitForCompletion());
                    Log.Debug("swapped solus heart reward material shader");
                }
            }
            
            FamilyDirectorCardCategorySelection solusFamilyCard = Addressables.LoadAssetAsync<FamilyDirectorCardCategorySelection>("RoR2/DLC3/dccsSuperRoboBallpitFamily.asset").WaitForCompletion();
            if (solusFamilyCard)
            {
                foreach (DirectorCardCategorySelection.Category category in solusFamilyCard.categories)
                {
                    foreach (RoR2.DirectorCard card in category.cards)
                    {
                        if (card.spawnCard && card.spawnCard.prefab)
                        {
                            solusFamilyBodyNames.Add(card.spawnCard.prefab.name);
                        }
                    }
                }
            }
            
            impactEffectPrefab = assetbundle.LoadAsset<GameObject>("SolusHeartRewardImpactEffect");
            ContentAddition.AddEffect(impactEffectPrefab);
            
            nullHeart = ScriptableObject.CreateInstance<ItemDef>();
            nullHeart.name = "ITEM_SOLUSHEARTREWARD_NAME";
            nullHeart.nameToken = "ITEM_SOLUSHEARTREWARD_NAME";
            nullHeart.pickupToken = "ITEM_SOLUSHEARTREWARD_PICKUP";
            nullHeart.descriptionToken = "ITEM_SOLUSHEARTREWARD_DESC";
            nullHeart.loreToken = "ITEM_SOLUSHEARTREWARD_LORE";
            nullHeart._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common.BossTierDef_asset).WaitForCompletion();
            nullHeart.pickupIconSprite = assetbundle.LoadAsset<Sprite>("SolusHeartPickup");
            nullHeart.pickupModelPrefab = assetbundle.LoadAsset<GameObject>("SolusHeartReward");
            nullHeart.tags = [ItemTag.BrotherBlacklist, ItemTag.AIBlacklist, ItemTag.CannotDuplicate, ItemTag.CannotSteal, ItemTag.CannotCopy, ItemTag.WorldUnique];
            nullHeart.hidden = false;
            nullHeart.canRemove = true;
            var displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(nullHeart, displayRules));

            Configs();
            
            IL.EntityStates.SolusHeart.Death.SolusHeartFinaleSequence.Death.OnEnter += DeathOnOnEnter;
            On.RoR2.BodyCatalog.SetBodyPrefabs += (orig, prefabs) => // NREs if we try to go through body catalog in awake .,,..
            {
                orig(prefabs);
                updateEnemyList();
            }; 
        }

        private void Configs()
        {
            blacklist = Config.Bind("Solus Heart Reward",
                "enemy blacklist",
                "MinePod,ExtractorUnit,DefectiveUnit", 
                "internal enemy body names (can be found on wiki !!( seperated by comma to disable when have solus heart reward !!!");
            blacklist.SettingChanged += (sender, args) =>
            {
                updateEnemyList();
            };
            ModSettingsManager.AddOption(new StringInputFieldOption(blacklist));
            
            logDebug = Config.Bind("Solus Heart Reward",
                "log debug",
                false, 
                "log names of spawn cards that arent in enabled config when attempted to spawn !!");
            ModSettingsManager.AddOption(new CheckBoxOption(logDebug));
            
            silenceSPEX = Config.Bind("Solus Heart Reward",
                "silence spex !!!!",
                true, 
                "makes spex silent post defeating solus heart !!");
            ModSettingsManager.AddOption(new CheckBoxOption(silenceSPEX));
            
            enableProc = Config.Bind("Solus Heart Reward",
                "Solus family on hit proc",
                true, 
                "gives null heart a on hit chance to deal extra damage to solus family enemies !!");
            ModSettingsManager.AddOption(new CheckBoxOption(enableProc));
            
            procChance = Config.Bind("Solus Heart Reward",
                "On hit proc chance",
                15, 
                "chance for null heart to proc against enemies !!");
            IntSliderConfig slideconfig = new()
            {
                formatString = "{0}%",
                min = 1,
                max = 100,
            };
            ModSettingsManager.AddOption(new IntSliderOption(procChance, slideconfig));
            
            procDamage = Config.Bind("Solus Heart Reward",
                "Null heart damage",
                50, 
                "percent of damage null heart should give extra on proc !!");
            IntSliderConfig slideconfig2 = new()
            {
                formatString = "{0}%",
                min = 1,
                max = 100,
            };
            ModSettingsManager.AddOption(new IntSliderOption(procDamage, slideconfig2));
            
            descFormat = Config.Bind("Solus Heart Reward",
                "description format",
                "Prevent <style=\"cIsUtility\">{0}</style> from spawning{1}.", 
                "like evil version of lang file for description .,.,., {0} is the list of enemies prevented from spawning and {1} is the proc description !!!!");
            ModSettingsManager.AddOption(new StringInputFieldOption(descFormat));
            
            procFormat = Config.Bind("Solus Heart Reward",
                "proc format",
                "and have a <style=\"cIsUtility\">{0}%</style> chance to deal <style=\"cIsDamage\">{1}%</style> total extra damage to solus enemies", 
                "like evil version of lang file for proc .,.,., {0} is the chance and {1} is the damage !!!!");
            ModSettingsManager.AddOption(new StringInputFieldOption(procFormat));
        }

        private void updateEnemyList()
        {
            Log.Debug("updating item desc !!");
            blacklistArray = blacklist.Value.Replace(" ", "").Replace("body", "").Split(',');
                
            string descAddition = "";
            foreach (var enemy in blacklistArray)
            {
                foreach (GameObject bodyPrefab in BodyCatalog.bodyPrefabs)
                {
                    if (!string.Equals(bodyPrefab.name, (enemy + "Body"), StringComparison.CurrentCultureIgnoreCase)) continue;
                    
                    if (bodyPrefab.GetComponent<CharacterBody>() == null) continue;
                    
                    Log.Debug(bodyPrefab.GetComponent<CharacterBody>().baseNameToken);
                    descAddition += Language.GetString(bodyPrefab.GetComponent<CharacterBody>().baseNameToken) + "s</style>, <style=\"cIsUtility\">";
                }
            }
            descAddition = descAddition[..^30];
            
            int lastComma = descAddition.LastIndexOf(", ", StringComparison.Ordinal);
            if (lastComma != -1)
            {
                descAddition = descAddition.Remove(lastComma, 2).Insert(lastComma, " and ");
            }

            string procstring = "";
            if (enableProc.Value)
            {
                procstring = " " + string.Format(procFormat.Value, procChance.Value, procDamage.Value);
            }
            
            LanguageAPI.Add("ITEM_SOLUSHEARTREWARD_DESC", string.Format(descFormat.Value, descAddition, procstring));
        }

        private void DeathOnOnEnter(ILContext il)
        {
            ILCursor c = new(il);
            
            if(c.TryGotoNext(
                   x => x.MatchLdcI4(0),
                   x => x.MatchStloc(7)))
            {
                // add 1 to the quat angle for the new item
                c.Emit(OpCodes.Ldarg_0); 
                c.EmitDelegate<Func<EntityStates.SolusHeart.Death.SolusHeartFinaleSequence.Death, Quaternion>>((_) => Quaternion.AngleAxis(360f / (Run.instance.participatingPlayerCount + 1), Vector3.up));
                c.Emit(OpCodes.Stloc_S, (byte)6); // store back in quat
                
                // spawn new item
                c.Emit(OpCodes.Ldarg_0); 
                c.Emit(OpCodes.Ldloc_3); // corepos
                c.Emit(OpCodes.Ldloc_S, (byte)5); // vector
                c.EmitDelegate<Action<EntityStates.SolusHeart.Death.SolusHeartFinaleSequence.Death, Vector3, Vector3>>(
                    (death, vector, corepos) =>
                    {
                        RoR2.PickupDropletController.CreatePickupDroplet(
                            PickupCatalog.FindPickupIndex(nullHeart.itemIndex),
                            vector,
                            corepos
                        );
                    }
                );
                
                //multiply quat 
                c.Emit(OpCodes.Ldloc_S, (byte)6); // quad
                c.Emit(OpCodes.Ldloc_S, (byte)5); // vector
                c.Emit(OpCodes.Call, typeof(Quaternion).GetMethod("op_Multiply", [typeof(Quaternion), typeof(Vector3)]));
                c.Emit(OpCodes.Stloc_S, (byte)5); // store back in vector
            }
            else
            {
                Log.Fatal("failed il hook on solus heart finale sequence death on enter !!");
            }
        }

         private void Update()
         {
             // if (Input.GetKeyDown(KeyCode.F1))
             // {
             //     Transform transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;
             //
             //     Log.Info($"Player pressed F2. Spawning our custom item at coordinates {transform.position}");
             //     PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(nullHeart.itemIndex),
             //         transform.position, transform.forward * 20f);
             // }
         }

        public sealed class Behavior : BaseItemBodyBehavior, IOnDamageDealtServerReceiver//, IOnDamageDealtServerReceiver, IOnIncomingDamageServerReceiver
        {
            [ItemDefAssociation]
            private static ItemDef GetItemDef() => nullHeart;


            public void OnEnable()
            {
                body.onInventoryChanged += BodyOnonInventoryChanged;
                
                if (GameObject.Find("watcher")) return;
                
                GameObject watcher = new();
                watcher.AddComponent<SolusHeartRewardHelper>();
                DontDestroyOnLoad(watcher);
                watcher.name = "watcher";
                Log.Debug("spawned solus heart reward helper");
            }

            private void BodyOnonInventoryChanged()
            {
                if (body.inventory.GetItemCountEffective(nullHeart) == 0)
                {
                    Destroy(GameObject.Find("watcher"));
                }
            }

            public void OnDisable()
            {
                body.onInventoryChanged -= BodyOnonInventoryChanged;
            }
            
            public void OnDamageDealtServer(DamageReport damageReport)
            {
                if (!enableProc.Value)
                {
                    return;
                }

                if (!damageReport.victimBody || !damageReport.victimBody.master ||
                    (!solusFamilyBodyNames.Contains(damageReport.victimBody.master.name.Replace("(Clone)", "")) &&
                     !(damageReport.victimBody.master.name.Contains("Solus") ||
                       damageReport.victimBody.master.name.Contains("RoboBallMini")) &&
                        damageReport.victimBody.master.name != "SolusVendorMaster"))
                {
                    return;
                }

                if (!Util.CheckRoll(procChance.Value * damageReport.damageInfo.procCoefficient, damageReport.attackerBody.master))
                {
                    return;
                }
                
                DamageInfo damageInfo = new()
                {
                    damage = damageReport.damageInfo.damage * procDamage.Value/100,
                    attacker = damageReport.attackerBody ? damageReport.attackerBody.gameObject : null,
                    inflictor = damageReport.attackerBody ? damageReport.attackerBody.gameObject : null,
                    position = damageReport.victimBody.corePosition,
                    force = Vector3.zero,
                    crit = damageReport.attackerBody.RollCrit(),
                    damageColorIndex = DamageColorIndex.Luminous,
                    damageType = DamageType.Generic,
                    procCoefficient = 0
                };
                damageReport.victimBody.healthComponent.TakeDamage(damageInfo);
                
                EffectData effectData = new()
                {
                    origin = damageReport.victimBody.corePosition,
                    start = damageReport.victimBody.corePosition,
                    rotation = Quaternion.identity,
                    scale = damageReport.victimBody.radius
                };
                EffectManager.SpawnEffect(impactEffectPrefab, effectData, true);
            }
        }
    }
}


public class SolusHeartRewardHelper : MonoBehaviour
{
    public void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void OnEnable()
    {
        DirectorCard.IsAvailable += DirectorCardOnIsAvailable;
        On.RoR2.Run.Start += RunOnStart;
        SolusVendorShrineSpeechDriver.SendReponseFromPool += SolusVendorShrineSpeechDriverOnSendReponseFromPool;
    }

    private void SolusVendorShrineSpeechDriverOnSendReponseFromPool(SolusVendorShrineSpeechDriver.orig_SendReponseFromPool orig, RoR2.CharacterSpeech.SolusVendorShrineSpeechDriver self, CharacterSpeechController.SpeechInfo[] responsePool)
    {
        if (!SolusHeartReward.SolusHeartReward.silenceSPEX.Value)
        {
            orig(self, responsePool);
        }
    }

    private void RunOnStart(On.RoR2.Run.orig_Start orig, Run self)
    {
        orig(self);
        
        if (Util.GetItemCountForTeam(TeamIndex.Player, SolusHeartReward.SolusHeartReward.nullHeart.itemIndex, false) == 0)
        {
            Destroy(gameObject);
        }
    }

    public void OnDisable()
    {
        DirectorCard.IsAvailable -= DirectorCardOnIsAvailable;
        On.RoR2.Run.Start -= RunOnStart;
        SolusVendorShrineSpeechDriver.SendReponseFromPool -= SolusVendorShrineSpeechDriverOnSendReponseFromPool;
    }
    
    private bool DirectorCardOnIsAvailable(DirectorCard.orig_IsAvailable orig, RoR2.DirectorCard self)
    {
        if (self.spawnCard && self.spawnCard.prefab)
        {
            if (IsCharacterSpawnCard(self.spawnCard))
            {
                if (SolusHeartReward.SolusHeartReward.blacklistArray.Contains(self.spawnCard.name.Replace("csc", "")))
                {
                    if (SolusHeartReward.SolusHeartReward.logDebug.Value)
                    {
                        Log.Debug("disabling spawn card " + self.spawnCard.name.Replace("csc", ""));
                    }

                    return false;
                }

                if (SolusHeartReward.SolusHeartReward.logDebug.Value)
                {
                    Log.Debug("character spawn card = " + self.spawnCard.name.Replace("csc", ""));
                }
            }
        }
        
        return orig(self);
    }
    
    //stolen from content disabler sorry !!!
    private static bool IsCharacterSpawnCard(SpawnCard spawnCard)
    {
        if (spawnCard == null) return false;

        CharacterMaster charMaster = spawnCard.prefab.GetComponent<CharacterMaster>();
        if (charMaster && charMaster.bodyPrefab)
        {
            CharacterBody charBody = charMaster.bodyPrefab.GetComponent<CharacterBody>();
            if (charBody)
            {
                return true;
            }
        }

        return false;
    }
}