using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RiskOfOptions;
using RiskOfOptions.Options;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using CombatDirector = On.RoR2.CombatDirector;
using DirectorCard = On.RoR2.DirectorCard;
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

        private static ItemDef myItemDef;
        private static ConfigEntry<string> blacklist;
        private static string[] blacklistArray;

        public void Awake()
        {
            Log.Init(Logger);

            myItemDef = ScriptableObject.CreateInstance<ItemDef>();
            myItemDef.name = "EXAMPLE_SOLUSHEARTREWARD_NAME";
            myItemDef.nameToken = "EXAMPLE_SOLUSHEARTREWARD_NAME";
            myItemDef.pickupToken = "EXAMPLE_SOLUSHEARTREWARD_PICKUP";
            myItemDef.descriptionToken = "EXAMPLE_SOLUSHEARTREWARD_DESC";
            myItemDef.loreToken = "EXAMPLE_SOLUSHEARTREWARD_LORE";
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common.BossTierDef_asset).WaitForCompletion();
            myItemDef.pickupIconSprite = Addressables.LoadAssetAsync<Sprite>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common_MiscIcons.texMysteryIcon_png).WaitForCompletion();
            myItemDef.pickupModelReference = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Mystery.PickupMystery_prefab);
            myItemDef.tags = [ItemTag.BrotherBlacklist, ItemTag.AIBlacklist, ItemTag.CannotDuplicate, ItemTag.CannotSteal, ItemTag.CannotCopy, ItemTag.WorldUnique];
            myItemDef.hidden = false;
            myItemDef.canRemove = true;
            
            var displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));
            
            blacklist = Config.Bind("Solus Heart Reward",
                "enemy blacklist",
                "Beetle", 
                "internal enemy body names (can be found on wiki !!( seperated by comma to disable when have solus heart reward !!!");
            blacklistArray = blacklist.Value.Replace(" ", "").Split(',');
            blacklist.SettingChanged += (sender, args) => { blacklistArray = blacklist.Value.Replace(" ", "").Split(','); };
            ModSettingsManager.AddOption(new StringInputFieldOption(blacklist));
            
            
            GlobalEventManager.onCharacterDeathGlobal += GlobalEventManager_onCharacterDeathGlobal;
            On.RoR2.CombatDirector.Spawn += CombatDirectorOnSpawn;
            IL.EntityStates.SolusHeart.Death.SolusHeartFinaleSequence.Death.OnEnter += DeathOnOnEnter;
        }

        private bool CombatDirectorOnSpawn(CombatDirector.orig_Spawn orig, RoR2.CombatDirector self, SpawnCard spawnCard, EliteDef eliteDef, Transform spawnTarget, DirectorCore.MonsterSpawnDistance spawnDistance, bool preventOverhead, float valueMultiplier, DirectorPlacementRule.PlacementMode placementMode, bool singleScaledBoss)
        {
            Log.Debug(spawnCard.prefab?.GetComponent<CharacterMaster>()?.name); // this has the Master affix after the name though ,.,. look into ,..,
            if(blacklistArray.Contains(spawnCard.prefab?.GetComponent<CharacterMaster>()?.name) && Util.GetItemCountForTeam(TeamIndex.Player, myItemDef.itemIndex, true) > 0)
            {
                Log.Debug($"trying to spawn {spawnCard.prefab?.GetComponent<CharacterMaster>()?.name} !! stopping ,..");
                return false;
            }
            return orig(self, spawnCard, eliteDef, spawnTarget, spawnDistance, preventOverhead, valueMultiplier, placementMode, singleScaledBoss);
        }

        private void DeathOnOnEnter(ILContext il)
        {
            ILCursor c = new(il);
            
            if(c.TryGotoNext(
                   x => x.MatchLdcI4(0),
                   x => x.MatchStloc(7)))
            {
                //Log.Debug(c);
                //Log.Debug(il.ToString());

                // add 1 to the quat angle for the new item
                c.Emit(OpCodes.Ldarg_0); 
                c.EmitDelegate<Func<EntityStates.SolusHeart.Death.SolusHeartFinaleSequence.Death, Quaternion>>(
                    (death) => Quaternion.AngleAxis(360f / (Run.instance.participatingPlayerCount + 1), Vector3.up));
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
                
                //multiple quat 
                c.Emit(OpCodes.Ldloc_S, (byte)6); // quad
                c.Emit(OpCodes.Ldloc_S, (byte)5); // vector
                c.Emit(OpCodes.Call, typeof(UnityEngine.Quaternion).GetMethod("op_Multiply", [typeof(Quaternion), typeof(Vector3)]));
                c.Emit(OpCodes.Stloc_S, (byte)5); // store back in vector
                Log.Debug(il.ToString());
                
            }
            else
            {
                Log.Fatal("failed il hook on solus heart finale sequence death on enter !!");
            }
        }

        private void GlobalEventManager_onCharacterDeathGlobal(DamageReport report)
        {
            if (!report.attacker || !report.attackerBody)
            {
                return;
            }

            var attackerCharacterBody = report.attackerBody;

            if (attackerCharacterBody.inventory)
            {
                var garbCount = attackerCharacterBody.inventory.GetItemCount(myItemDef.itemIndex);
                if (garbCount > 0 &&
                    Util.CheckRoll(50, attackerCharacterBody.master))
                {
                    attackerCharacterBody.AddTimedBuff(RoR2Content.Buffs.Cloak, 3 + garbCount);
                }
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;
                
                Log.Info($"Player pressed F2. Spawning our custom item at coordinates {transform.position}");
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(myItemDef.itemIndex), transform.position, transform.forward * 20f);
            }
        }
    }
}
