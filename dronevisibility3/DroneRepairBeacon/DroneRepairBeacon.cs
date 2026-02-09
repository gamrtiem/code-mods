using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using On.RoR2.EntitlementManagement;
using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using RoR2.Hologram;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using Console = RoR2.Console;

namespace DroneRepairBeacon
{
    [BepInDependency("iDeathHD.UnityHotReload", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(NetworkingAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class DroneRepairBeacon : BaseUnityPlugin
    {
        private const string PluginGUID = PluginAuthor + "." + PluginName;
        private const string PluginAuthor = "icebro";
        private const string PluginName = "DroneRepairBeacon";
        private const string PluginVersion = "1.0.1";

        private static bool UHRInstalled => Chainloader.PluginInfos.ContainsKey("iDeathHD.UnityHotReload");

        private static GameObject DroneIndicatorHologram;
        public static GameObject DroneIndicatorVFX;
        public static List<Sprite> droneIndicatorSprites = []; // this could probably be done with a dictionary but idk how to use them,. ,.,. 
        public static Material[] droneIndicatorMaterials = [];
        public static List<Sprite> specificDroneIndicatorSprites = []; 
        public static Material[] specificDroneIndicatorMaterials = []; 
        public static ConfigEntry<float> displayDistance;
        public static ConfigEntry<float> helpScale;
        private static ConfigEntry<string> DeathTokenConfigs;
        private static ConfigEntry<string> DroneSpecificDeathTokenConfigs;
        public static ConfigEntry<string> droneSprites;
        public static ConfigEntry<bool> customDroneSprites;
        private static ConfigEntry<bool> alwaysShowHelp;
        private static ConfigEntry<bool> alwaysShowHelpAllowTurrets;
        public static AssetBundle assetbundle;
        public static Material baseMat;

        public void Awake()
        {
            Log.Init(Logger);
            
            string assetbundledir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location)!, "dronevisibilitybundle");
            assetbundle = AssetBundle.LoadFromFileAsync(assetbundledir).assetBundle;
            
            DroneIndicatorVFX = assetbundle.LoadAsset<GameObject>("DroneIndicatorVFX");
            //DroneIndicatorVFX.AddComponent<NetworkIdentity>();
            //DroneIndicatorVFX.RegisterNetworkPrefab();
            
            baseMat = assetbundle.LoadAsset<Material>("HGStandard");
            foreach (Material material in assetbundle.LoadAllAssets<Material>())
            {
                if (material.name != "HGStandard") continue; // this is the only material in the asset bundle but just in case <3 
                
                material.shader = Addressables.LoadAssetAsync<Shader>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Shaders.HGStandard_shader).WaitForCompletion();
                Log.Debug("swapped drone notif shader !");
            }

            #region configs
            DeathTokenConfigs = Config.Bind("drone repair beacon",
                "silly death strings ! split with /,   ...,., set to nothing to disable !",
                "res me twin....,,./,HELP HELP MEEE HEEEEEELP/,ough ,.,.../,babe its {TIME},,, time for your drone reviving session,.,.,.,/,im not broken! im just.... napping... indefinitely..../,REQUESTING HELP.../,LOST CONNECTION.../,YOUR WARRANTY IS 3 DAYS EXPIRED./,oof ouch my bo lts..,/,FUCK",
                "byeah !");
            
            DroneSpecificDeathTokenConfigs = Config.Bind("drone repair beacon",
                "specific drone death strings! formatting like (drone internal name);;(string,string);;!",
                "Drone1Body;;do your own DPS instead... im done...;'.;/,pls revive i had raygun/,drats,..,,almost had em,.,,,;;Drone2Body;;ill,,.,., miss you \ud83e\udd7a/,please don't leave me here.,.,./,I MISS_YOU ALREADY;;CopycatDrone;;i_i feel.,.., cold.,.,/,is it just me or is it f_f_freezing.,.,,;;BombardmentDrone;;ERR: HEAVY DAMAGE SUSTAINED/, RETRIEVING FINAL WISHES... UNDEFINED...;;RechargeDrone;;i've.,.,. failed you,..,./, come back,,,.. you're hurt,,.,,/,i'll always be watching over you,,...;;CleanupDrone;;cleanu;p on aisle,, 6.,.,./,what a mess i've gotten myself into.,,../,missed a spot,,,..;;EmergencyDrone;;i just wanted to protect everyone,,,...,...,,,, /, keep them safe.,,,.for me..,../,why can't we all be friends..?;;HaulerDrone;; i've,,.. lost my way..,.,/, it's the end of the road for me :(/,carry me back home, would you?;;EquipmentDrone;;i wasn't equipped to handle THIS!/,your gift was wasted on me..,../, you dropped something,..,,;;FlameDrone;;couldn't,, take the heat,,,,.,/,gimme a sec, just need to.,,,., cool down real quick,,,.,/,oohHAhothothot!!;;JailerDrone;;can't,,,, move...,./,h_hey, slow down..!/everything's so,,.,, still..,.;;JunkDrone;;i'm not junk,.,.,,,/,please don't scrap me..,.,/,wait.,.., i've still got more to give..,..;;MissileDrone;;target lost,..,,/,look's like team missile's blasting off again.,,.,,/,i can't see them, boss..,.,boss..?",
                "you can get them from debugtoolkit or wiki (wiki ones are missing body sometimes though ! like .,., Drone1Body = gunnerdrone ,.,. Drone2Body = healing drone .,.,,..");
            
            displayDistance = Config.Bind("drone repair beacon",
                "how far should help be visible !!",
                90f,
                "byeah !");
            
            helpScale = Config.Bind("drone repair beacon",
                "how big help should be ! 1 equals 1x ,.,.,.",
                1f,
                "byeah !");
            
            alwaysShowHelp = Config.Bind("drone repair beacon",
                "always show help ,.., like on unpurchased drones !!",
                false,
                "byeah !");
            alwaysShowHelpAllowTurrets = Config.Bind("drone repair beacon",
                "always show help allow turrets to be helped too  !!",
                false,
                "byeah !");
            
            droneSprites = Config.Bind("drone repair beacon",
                "what message sprites to use! randomizes with multiple ,..,. options in config desc !! seperate by , NEEDS RESTART !!",
                "BeaconVFXbrokenheart,BeaconVFXCog,BeaconVFXexplain,BeaconVFXowww,BeaconVFXplead,BeaconVFXresmetwin,BeaconVFXAnar,BeaconVFXLukas",
                "BeaconVFXbrokenheart,BeaconVFXCog,BeaconVFXexplain,BeaconVFXowww,BeaconVFXplead,BeaconVFXresmetwin,BeaconVFXAnar,BeaconVFXAnarColor,BeaconVFXLukas,BeaconVFXLukasColor");
            customDroneSprites = Config.Bind("drone repair beacon",
                "if you have any custom images you wants to use !!! NEEDS RESTART !!",
                false,
                "should be in bepin config directory in DroneBeacon folder .,,. just add there and will appear in rotation !! ,.,., must end in .png !!!!!!!!!!!!!");
            SpriteLoading.ChangeSprites();
            #endregion
            
            Hook();
            
            DroneIndicatorHologram = assetbundle.LoadAsset<GameObject>("DroneIndicatorHologram");
            DroneIndicatorHologram.AddComponent<NetworkIdentity>();
            HologramProjector projector = DroneIndicatorHologram.GetComponent<HologramProjector>();
            projector.contentProvider = DroneIndicatorHologram.AddComponent<deadDroneTracker>();
            projector.displayDistance = displayDistance.Value;
            DroneIndicatorHologram.RegisterNetworkPrefab();
            
            NetworkingAPI.RegisterMessageType<deadDroneTracker.recieveMessageID>();
            NetworkingAPI.RegisterMessageType<deadDroneTracker.sendMessageID>();
        }
        
        
        
        #region Hooks
        private void Hook()
        {
            On.RoR2.SummonMasterBehavior.OnEnable += SummonMasterBehaviorOnOnEnable;
            IL.EntityStates.Drone.DeathState.OnImpactServer += DeathStateOnOnImpactServer;
            //IL.EntityStates.Drone.DeathState.FixedUpdate += DeathStateOnFixedUpdate;
            IL.RoR2.Hologram.HologramProjector.UpdateForViewer += HologramProjectorOnUpdateForViewer;
        }

        private void SummonMasterBehaviorOnOnEnable(On.RoR2.SummonMasterBehavior.orig_OnEnable orig, SummonMasterBehavior self)
        {
            orig(self);
            if(!alwaysShowHelp.Value || !alwaysShowHelpAllowTurrets.Value && self.gameObject.name.Contains("Turret"))
            {
                return;
            }
            
            GameObject indicator = Instantiate(DroneIndicatorHologram, self.transform);

            //HologramProjector projector = indicator.GetComponent<HologramProjector>();
            //projector.contentProvider = indicator.AddComponent<deadDroneTracker>();
            //projector.displayDistance = displayDistance.Value;
        }

        private void HologramProjectorOnUpdateForViewer(ILContext il)
        {
            Log.Debug("tryings il hook hologram updater ,,.");
            ILCursor c = new(il);
            
            /*
            // }
               IL_00ea: ret

            // DestroyHologram();
               IL_00eb: ldarg.0
               IL_00ec: call instance void RoR2.Hologram.HologramProjector::DestroyHologram()
            */
            
            if (c.TryGotoNext(x => x.MatchRet(), 
                    x => x.MatchLdarg(0),
                    x => x.MatchCall<HologramProjector>("DestroyHologram")))
            {
                c.Emit(OpCodes.Ldarg_0); // load self.,. 
                c.EmitDelegate<Action<HologramProjector>>(
                    (hologramProjector) =>
                    {
                        if (hologramProjector.name == "DroneIndicatorHologram(Clone)") // its either this or get component every frame get off my back !!
                        {
                            Transform hologram = hologramProjector.transform.GetChild(0).GetChild(0);
                            if (hologram != null)
                            {
                                Quaternion targetQuat = Quaternion.Euler(new Vector3(0, hologram.eulerAngles.y, 0));
                                targetQuat.x = 0;
                                targetQuat.z = 0;
                                hologram.transform.rotation = targetQuat;
                            }
                        }
                    }
                );

                Log.Debug("added il hook to hologram updater !!");
            }
        }

        private void DeathStateOnOnImpactServer(ILContext il)
        {
            Log.Debug("tryings il hook dead drone ,,.");
            ILCursor c = new(il);

            /*
            // spawnedRepairObject = true;
               IL_00c9: ldarg.0
               IL_00ca: ldc.i4.1
               IL_00cb: stfld bool EntityStates.Drone.DeathState::spawnedRepairObject
            */

            if (c.TryGotoNext(x => x.MatchLdarg(0), 
                    x => x.MatchLdcI4(1),
                    x => x.MatchStfld<EntityStates.Drone.DeathState>("spawnedRepairObject")))
            {
                c.Emit(OpCodes.Ldarg_0); // drone death state (name otken .,,. 
                c.Emit(OpCodes.Ldloc_S, (byte)4); // load dead drone
                c.EmitDelegate<Action<EntityStates.Drone.DeathState, GameObject>>(
                    (deathState, deadDrone) =>
                    {
                        int rng = -1;
                        bool usingSpecificSprite = false;
                        if (droneIndicatorSprites.Count != 0 || specificDroneIndicatorSprites.Count != 0)
                        {
                            List<int> specificSpriteIndexes = [];
                
                            for(int i = 0; i < specificDroneIndicatorSprites.Count; i++)
                            {
                                Log.Debug("bweh " + specificDroneIndicatorSprites[i].name.Replace("Body", ""));
                                Log.Debug(deathState.characterBody.name.Replace("Body", "").Replace("(Clone)", ""));
                                if (specificDroneIndicatorSprites[i].name.Split("_")[0].Replace("Body", "") != deathState.characterBody.name.Replace("Body", "").Replace("(Clone)", "")) continue;
                                Log.Debug($"adding {specificDroneIndicatorSprites[i].name}");
                                specificSpriteIndexes.Add(i);
                            }
                            Log.Debug(specificSpriteIndexes.Count);
                            if (droneIndicatorSprites.Count != 0 || specificSpriteIndexes.Count != 0)
                            {
                                rng = Run.instance.runRNG.RangeInt(0, droneIndicatorSprites.Count + specificSpriteIndexes.Count);
                                if (rng >= droneIndicatorSprites.Count)
                                {
                                    rng = specificSpriteIndexes[Run.instance.runRNG.RangeInt(0, specificSpriteIndexes.Count)];
                                    usingSpecificSprite = true;
                                }
                            
                                Log.Debug(rng);
                                Log.Debug(usingSpecificSprite);
                                if (!usingSpecificSprite)
                                {
                                    Log.Debug(droneIndicatorSprites[rng]);
                                }
                            }
                        }
                        
                        if(!alwaysShowHelp.Value)
                        {
                            GameObject indicator = Instantiate(DroneIndicatorHologram, deadDrone.transform);
                            deadDroneTracker tracker = indicator.GetComponent<deadDroneTracker>();
                            tracker.messageID = rng;
                            tracker.usingSpecificSprite = usingSpecificSprite;

                            NetworkServer.Spawn(indicator);
                            /*deadDroneTracker droneTracker = indicator.GetComponent<deadDroneTracker>();
                            if (droneIndicatorSprites.Count != 0)
                            {
                                int rng = Run.instance.runRNG.RangeInt(0, droneIndicatorSprites.Count);
                                droneTracker.messageID = rng;
                                
                                NetworkIdentity identity = indicator.GetComponent<NetworkIdentity>();
                                if (!identity)
                                {
                                    Log.Warning("indicator did not have net id !!!");
                                    return;
                                }
                                
                                new deadDroneTracker.recieveMessageID(identity.netId, rng).Send(NetworkDestination.Clients);
                            }*/
                        }

                        if (DeathTokenConfigs.Value == "")
                        {
                            return;
                        }
                            
                        List<string> deathTokens = DeathTokenConfigs.Value.Split("/,").ToList();
                        string[] specificTokens = DroneSpecificDeathTokenConfigs.Value.Split(";;");
                        for (int i = 0; i < specificTokens.Length; i++)
                        {
                            if (specificTokens[i].Trim().Replace("Body", "") != deathState.characterBody.name.Replace("Body", "").Replace("(Clone)", "")) continue;
                                
                            string[] specificTokensReal = specificTokens[i + 1].Split("/,");
                            deathTokens.AddRange(specificTokensReal);
                        }
                        Log.Debug("drone name = " + deathState.characterBody.name);
                            
                        string deathString = deathTokens[Run.instance.runRNG.RangeInt(0, deathTokens.Count)];
                        if (deathString.Contains("{TIME}"))
                        {
                            string time = DateTime.Now.ToString("hh") + DateTime.Now.ToString("tt");
                            if (time.StartsWith("0"))
                            {
                                time = time.Substring(1, time.Length - 1);
                            }

                            deathString = deathString.Replace("{TIME}", time.ToLower());
                        }
                            
                        deathString = deathString.Trim();
                        Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                        {
                            baseToken = $"<style=cEvent>{Language.GetStringFormatted(deathState.characterBody.baseNameToken)}: {deathString}</style>"
                        });
                    }
                );

                Log.Debug("added tracker to dead drones !!");
            }
        }
        #endregion

        private void Update()
        {
#if DEBUG
            if (Input.GetKeyUp(KeyCode.F5))
            {
                if (UHRInstalled)
                {
                    UHRSupport.hotReload(typeof(DroneRepairBeacon).Assembly, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location)!, "DroneRepairBeacon.dll"));
                }
                else
                {
                    Log.Debug("couldnt finds unity hot reload !!");
                }
            }
#endif  
        }
    }
}

