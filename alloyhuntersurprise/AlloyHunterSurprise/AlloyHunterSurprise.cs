using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.EntitlementManagement;
using RoR2.ExpansionManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using R2API.Utils;

namespace AlloyHunterSurprise
{
    [BepInDependency("iDeathHD.UnityHotReload", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync)]
    public class AlloyHunterSurprise : BaseUnityPlugin
    {
        private const string PluginGUID = PluginAuthor + "." + PluginName;
        private const string PluginAuthor = "icebro";
        private const string PluginName = "AlloyHunterSurprise";
        private const string PluginVersion = "1.0.0";

        private static bool UHRInstalled => Chainloader.PluginInfos.ContainsKey("iDeathHD.UnityHotReload");
        private GameObject alloyHunterMasterOriginalRef;
        private GameObject alloyHunterMaster;
        private ConfigEntry<float> invasionChance;
        private ConfigEntry<int> minStageCount;
        private ConfigEntry<string> invaderReplacement;
        private ConfigEntry<float> healthPercent;
        private ConfigEntry<bool> asManyInvasionsAsBosses;
        private ConfigEntry<bool> dontInvadeHordes;
        private ConfigEntry<string> invasionWhitelist;
        private List<string> invasionWhitelistReal = [];

        public void Awake()
        {
            Log.Init(Logger);

            invasionChance = Config.Bind("AlloyHunterSurprise",
                "invasion chance.,.,.,",
                30f,
                "chance for alloy hunter to invade (0-100% .,.,");

            minStageCount = Config.Bind("AlloyHunterSurprise",
                "minimum stage count,..,.,",
                3,
                "chance for alloy hunter to invade (0-100% .,.,");

            invaderReplacement = Config.Bind("AlloyHunterSurprise",
                "invader replacement !! put master name ,. (eg. BrotherMaster",
                "",
                "itsl ike !! what autocompletes when doing spawn_ai .,., leave blank for no replacement!!");
            invaderReplacement.SettingChanged += (_, _) =>
            {
                replaceInvader();
            };
            
            invasionWhitelist = Config.Bind("AlloyHunterSurprise",
                "invasion whitelist !! seperate with comma ,.,.",
                "RoboBallBossMaster,SolusAmalgamatorMaster",
                "get from spawn_ai autocompletes !! eg. (RoboBallBossMaster,SolusAmalgamatorMaster)!!");
            UpdateWhitelist();
            invasionWhitelist.SettingChanged += (_, _) =>
            {
                UpdateWhitelist();
            };
            
            asManyInvasionsAsBosses = Config.Bind("AlloyHunterSurprise",
                "have as many invasions as spawned bosses,.,.",
                false,
                "like !! if theres multiple bosses have mutliple invasion .,.,");
            
            dontInvadeHordes = Config.Bind("AlloyHunterSurprise",
                "dont invade hordes of many,..,",
                true,
                "explanitory i think.,,. .,.,");
            
            healthPercent = Config.Bind("AlloyHunterSurprise",
                "health percentage before steal !!!",
                0.75f,
                "health threshold before killing !!!!");

            On.RoR2.TeleporterInteraction.Awake += TeleporterInteractionOnAwake;
            On.RoR2.VultureFightOverride.TriggerVultureOnMasterServer += VultureFightOverrideOnTriggerVultureOnMasterServer;
            IL.RoR2.VultureFightOverride.TriggerServer += VultureFightOverrideOnTriggerServer;
            On.RoR2.MasterCatalog.Init += MasterCatalogOnInit;
            
            AsyncOperationHandle<GameObject> matVultureHunterAddressable = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_35_0.RoR2_DLC3_VultureHunter.VultureHunterMaster_prefab);
            matVultureHunterAddressable.Completed += _ =>
            {
                alloyHunterMasterOriginalRef = matVultureHunterAddressable.Result;
            };
        }

        private void UpdateWhitelist()
        {
            invasionWhitelistReal = [];
            foreach (string masterName in invasionWhitelist.Value.Split(","))
            {
                if (!masterName.Trim().IsNullOrWhiteSpace())
                {
                    invasionWhitelistReal.Add(masterName.Trim());
                }
            }
        }
        private void MasterCatalogOnInit(On.RoR2.MasterCatalog.orig_Init orig)
        {
            orig();
            replaceInvader();
        }

        private void replaceInvader()
        {
            GameObject invaderReplace = MasterCatalog.GetMasterPrefab(MasterCatalog.FindMasterIndex(invaderReplacement.Value));
            if (invaderReplace != null)
            {
                //Log.Debug($"replacing invader with {invaderReplace} !!");
                alloyHunterMaster = invaderReplace;
            }
            else
            {
                Log.Debug($"failed to find invader {invaderReplacement.Value},.., falling back on alloyhunter !!");
                alloyHunterMaster = alloyHunterMasterOriginalRef;
            }
        }
        
        private void VultureFightOverrideOnTriggerServer(ILContext il)
        {
            ILCursor c = new(il);
            
            /*
            // TeleporterInteraction.instance.bossDirector.OverrideNextBossCard(ForcedBossFight, singularScaledBoss && !flag);
               IL_0059: call class RoR2.TeleporterInteraction RoR2.TeleporterInteraction::get_instance()
               IL_005e: ldfld class RoR2.CombatDirector RoR2.TeleporterInteraction::bossDirector
               IL_0063: ldarg.0
               IL_0064: ldfld class RoR2.DirectorCard RoR2.VultureFightOverride::ForcedBossFight
               IL_0069: ldarg.0
               IL_006a: ldfld bool RoR2.VultureFightOverride::singularScaledBoss
               IL_006f: brfalse.s IL_0077
            */
            
            if (c.TryGotoNext(x => x.MatchCall<TeleporterInteraction>("get_instance"), 
                    x => x.MatchLdfld<TeleporterInteraction>("bossDirector"),
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<VultureFightOverride>("ForcedBossFight"),
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<VultureFightOverride>("singularScaledBoss")))

            {
                c.RemoveRange(13); // remove original overridenextcard ..,,.,.
                c.Emit(OpCodes.Ldarg_0); // load self.,. 
                c.EmitDelegate<Action<VultureFightOverride>>(
                    (vultureOverride) =>
                    {
                        if (vultureOverride.ForcedBossFight != null) // if youre um .,,. hooking onto this too .,., idk i hate you <33 .,, 
                        {
                            TeleporterInteraction.instance.bossDirector.OverrideNextBossCard(vultureOverride.ForcedBossFight, vultureOverride.singularScaledBoss && !((bool)TeleporterInteraction.instance && TeleporterInteraction.instance.shrineBonusStacks > 0));
                        }
                    }
                );
                
                Log.Debug("added il hook to null chcek forcedbossfight !!");
            }
        }

        private void VultureFightOverrideOnTriggerVultureOnMasterServer(On.RoR2.VultureFightOverride.orig_TriggerVultureOnMasterServer orig, VultureFightOverride self, CharacterMaster boss)
        {
            if (self.singularKiller && self.GetFieldValue<int>("bossReplacementsPerformed") > 0)
            {
                return;
            }
            
            if (self.ForcedBossFight == null)
            {
                WeightedSelection<DirectorCard> monsterSelection = ClassicStageInfo.instance.monsterSelection;
                DirectorCard bossCard = null;
            
                for (int i = 0; i < monsterSelection._count; i++)
                {
                    if (monsterSelection.choices[i].value?.spawnCard?.prefab?.GetComponent<CharacterMaster>()?.bodyPrefab != boss?.bodyPrefab) continue;

                    bossCard = monsterSelection.choices[i].value;
                    Log.Debug($"found {boss.name} s card !!! ");
                    
                    if (boss.name.Contains("RoboBallBoss") || boss.name.Contains("SolusAmalgamator"))
                    {
                        EntityStates.VultureHunter.Body.SpawnAfterMuder.idleAfterTeleportDelay = 1.85f;
                        EntityStates.VultureHunter.Body.DoMurder.enterDelay = 5.5f;
                        self.healthbarHideDuration = 6.2f;
                    }
                    else
                    {
                        EntityStates.VultureHunter.Body.SpawnAfterMuder.idleAfterTeleportDelay = 1f;
                        EntityStates.VultureHunter.Body.DoMurder.enterDelay = 3f;
                        self.healthbarHideDuration = 4f;
                    }
                    
                    if ((dontInvadeHordes.Value && boss.bodyPrefab && boss.bodyPrefab.TryGetComponent(out CharacterBody bossBody) && !bossBody.isChampion) || (invasionWhitelistReal.Count != 0 && !invasionWhitelistReal.Contains(boss.name.Replace("(Clone)", ""))))
                    {
                        self.watchingSquadMembers.Remove(boss);
                        if (self.watchingSquadMembers.Count == 0)
                        {
                            TeleporterInteraction.instance.bossDirector.combatSquad.delayDefeat = false;
                        }
                        return;
                    }
                    
                    break;
                }
                
                self.ForcedBossFight = bossCard;
            }
            
            orig(self, boss);
        }

        private void TeleporterInteractionOnAwake(On.RoR2.TeleporterInteraction.orig_Awake orig, TeleporterInteraction self)
        {
            orig(self);
            
            if (self.gameObject.GetComponent<VultureFightOverride>())
            {
                EntityStates.VultureHunter.Body.SpawnAfterMuder.idleAfterTeleportDelay = 1.85f;
                EntityStates.VultureHunter.Body.DoMurder.enterDelay = 5f;
                return;
            }

            if (NetworkServer.active && (Run.instance.runRNG.RangeFloat(0, 100) <= invasionChance.Value) && Run.instance.stageClearCount + 1 >= minStageCount.Value)
            {
                VultureFightOverride fightOverride = self.gameObject.AddComponent<VultureFightOverride>();
                fightOverride.singularKiller = !(asManyInvasionsAsBosses.Value);
                fightOverride.triggerOnStart = true;
                fightOverride.nextKillSequenceMin = 2;
                fightOverride.nextKillSequenceMax = 5;
                fightOverride.healthbarHideDuration = 4f;
                fightOverride.replacementAtHealthThreshold = healthPercent.Value;
                fightOverride.replacementMaster = alloyHunterMaster;
                
                if (alloyHunterMaster.TryGetComponent(out CharacterMaster hunterComponent) && hunterComponent.bodyPrefab.TryGetComponent(out CharacterBody body) && body.TryGetComponent(out ExpansionRequirementComponent expRequirement))
                {
                    if (!EntitlementManager.networkUserEntitlementTracker.AnyUserHasEntitlement(expRequirement.requiredExpansion.requiredEntitlement))
                    {
                        Log.Debug("user doesnt have any entitlement !!! gorp ,,.,. get the dlc of whatever youre trying to spawn silly <//3..,.,");
                        Destroy(fightOverride);
                    }
                }
            }
        }
        
#if DEBUG
        private void Update()
        {

            if (Input.GetKeyUp(KeyCode.F8))
            {
                if (UHRInstalled)
                {
                    UHRSupport.hotReload(typeof(AlloyHunterSurprise).Assembly, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), "AlloyHunterSurprise.dll"));
                }
                else
                {
                    Log.Debug("couldnt finds unity hot reload !!");
                }
            }

        }
#endif  
    }
}
