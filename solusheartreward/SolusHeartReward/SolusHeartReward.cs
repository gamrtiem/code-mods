using System;
using System.Collections;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using EntityStates.InfiniteTowerSafeWard;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using On.RoR2.CharacterSpeech;
using R2API;
using RiskOfOptions;
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
        public const string PluginName = "SolusHeartReward";
        public const string PluginVersion = "1.0.0";

        private static ConfigEntry<string> blacklist;
        public static ConfigEntry<bool> logDebug;
        public static ConfigEntry<bool> silenceSPEX;
        
        public static ItemDef myItemDef;
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
                if(material.name == "matTransparentChip")
                {
                    material.shader = Addressables.LoadAssetAsync<Shader>("RoR2/DLC3/TransparentTextureScrolling.shader").WaitForCompletion();
                    Log.Debug("swapped solus heart reward material shader");
                }
            }
            
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();
            myItemDef.name = "ITEM_SOLUSHEARTREWARD_NAME";
            myItemDef.nameToken = "ITEM_SOLUSHEARTREWARD_NAME";
            myItemDef.pickupToken = "ITEM_SOLUSHEARTREWARD_PICKUP";
            myItemDef.descriptionToken = "ITEM_SOLUSHEARTREWARD_DESC";
            myItemDef.loreToken = "ITEM_SOLUSHEARTREWARD_LORE";
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common.BossTierDef_asset).WaitForCompletion();
            myItemDef.pickupIconSprite = assetbundle.LoadAsset<Sprite>("SolusHeartPickup");
            myItemDef.pickupModelPrefab = assetbundle.LoadAsset<GameObject>("SolusHeartReward");
            myItemDef.tags = [ItemTag.BrotherBlacklist, ItemTag.AIBlacklist, ItemTag.CannotDuplicate, ItemTag.CannotSteal, ItemTag.CannotCopy, ItemTag.WorldUnique];
            myItemDef.hidden = false;
            myItemDef.canRemove = true;
            var displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));
            
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
            
            IL.EntityStates.SolusHeart.Death.SolusHeartFinaleSequence.Death.OnEnter += DeathOnOnEnter;
            On.RoR2.BodyCatalog.SetBodyPrefabs += (orig, prefabs) => // NREs if we try to go through body catalog in awake .,,..
            {
                orig(prefabs);
                updateEnemyList();
            }; 
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
                
            LanguageAPI.Add("ITEM_SOLUSHEARTREWARD_DESC", $"Prevent <style=\"cIsUtility\">{descAddition}</style> from spawning.");
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
                            PickupCatalog.FindPickupIndex(myItemDef.itemIndex),
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

        // private void Update()
        // {
        //     if (Input.GetKeyDown(KeyCode.F1))
        //     {
        //         Transform transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;
        //
        //         Log.Info($"Player pressed F2. Spawning our custom item at coordinates {transform.position}");
        //         PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(myItemDef.itemIndex),
        //             transform.position, transform.forward * 20f);
        //     }
        // }

        public sealed class Behavior : BaseItemBodyBehavior//, IOnDamageDealtServerReceiver, IOnIncomingDamageServerReceiver
        {
            [ItemDefAssociation]
            private static ItemDef GetItemDef() => myItemDef;


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
                if (body.inventory.GetItemCountEffective(myItemDef) == 0)
                {
                    Destroy(GameObject.Find("watcher"));
                }
            }

            public void OnDisable()
            {
                body.onInventoryChanged -= BodyOnonInventoryChanged;
            }

            // public void OnDamageDealtServer(DamageReport damageReport)
            // {
            //     //idk apply a debuff or something !!
            // }
            //
            // public void OnIncomingDamageServer(DamageInfo damageInfo)
            // {
            //     //damageInfo.damage *= 0.1f;
            // }
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
        
        if (Util.GetItemCountForTeam(TeamIndex.Player, SolusHeartReward.SolusHeartReward.myItemDef.itemIndex, false) == 0)
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
                    Log.Debug("disabling spawn card " + self.spawnCard.name.Replace("csc", ""));
                
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