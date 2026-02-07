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
        private const string PluginVersion = "1.0.0";

        private static bool UHRInstalled => Chainloader.PluginInfos.ContainsKey("iDeathHD.UnityHotReload");

        private static GameObject DroneIndicatorHologram;
        public static GameObject DroneIndicatorVFX;
        public static List<Sprite> droneIndicatorSprites = []; // this could probably be done with a dictionary but idk how to use them,. ,.,. 
        public static List<Sprite> specificDroneIndicatorSprites = []; 
        public static Material[] droneIndicatorMaterials = [];
        public static ConfigEntry<float> displayDistance;
        public static ConfigEntry<float> helpScale;
        private static ConfigEntry<string> DeathTokenConfigs;
        private static ConfigEntry<string> DroneSpecificDeathTokenConfigs;
        private static ConfigEntry<string> droneSprites;
        private static ConfigEntry<bool> customDroneSprites;
        private static ConfigEntry<bool> alwaysShowHelp;
        private static ConfigEntry<bool> alwaysShowHelpAllowTurrets;
        private static AssetBundle assetbundle;
        private static Material baseMat;

        public void Awake()
        {
            Log.Init(Logger);
            
            string assetbundledir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location)!, "dronevisibilitybundle");
            assetbundle = AssetBundle.LoadFromFileAsync(assetbundledir).assetBundle;
            
            DroneIndicatorVFX = assetbundle.LoadAsset<GameObject>("DroneIndicatorVFX");
            DroneIndicatorVFX.AddComponent<NetworkIdentity>();
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
                "Drone1Body;;do your own DPS instead... im done...;'.;/,pls revive i had raygun;;Drone2Body;;ill,,.,., miss you 🥺/,please don't leave me here.,.,./,I MISS_YOU ALREADY;",
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
            ChangeSprites();
            #endregion
            
            Hook();
            
            DroneIndicatorHologram = assetbundle.LoadAsset<GameObject>("DroneIndicatorHologram");
            DroneIndicatorHologram.AddComponent<NetworkIdentity>();
            //DroneIndicatorHologram.RegisterNetworkPrefab();
            HologramProjector projector = DroneIndicatorHologram.GetComponent<HologramProjector>();
            projector.contentProvider = DroneIndicatorHologram.AddComponent<deadDroneTracker>();
            projector.displayDistance = displayDistance.Value;
            DroneIndicatorHologram.RegisterNetworkPrefab();
            
            NetworkingAPI.RegisterMessageType<deadDroneTracker.SyncBaseStats>();
        }
        
        private void ChangeSprites()
        {
            //droneIndicatorSprites.Clear();
            foreach (string spriteName in droneSprites.Value.Split(","))
            {
                if (assetbundle.Contains(spriteName.Trim()))
                {
                    droneIndicatorSprites.Add(assetbundle.LoadAsset<Sprite>(spriteName.Trim()));
                }
                else
                {
                    Log.Error($"couldnt find sprite {spriteName.Trim()} in asset bundle !!");
                }
            }

            if (customDroneSprites.Value)
            {
                string dir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Paths.ConfigPath)!, "config", "DroneBeacon");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                
                string specificDir = System.IO.Path.Combine(dir, "Specific");
                if (!Directory.Exists(specificDir))
                {
                    Directory.CreateDirectory(specificDir);
                }

                string [] fileEntries = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
                foreach (string fileName in fileEntries)
                {
                    string file = System.IO.Path.Combine(dir, fileName.Trim());

                    
                    if (!file.EndsWith(".png") && !file.EndsWith(".psd"))
                    {
                        Log.Debug($"file {file} does not end in png ,..,,. skipping !!");
                        continue;
                    }
                    
                    Sprite loadedSprite = LoadSpriteFromFile(file);
                    if(loadedSprite != null)
                    {
                        if (file.Contains(specificDir))
                        {
                        
                        }
                        else
                        {
                            droneIndicatorSprites.Add(loadedSprite);
                        }
                    }
                    else
                    {
                        Log.Error($"couldnt find sprite {file.Trim()} in files !!");
                    }
                }
            }

            // foreach (var material in droneIndicatorMaterials)
            // {
            //     if (material != null)
            //     {
            //         Object.Destroy(material);
            //     }
            // }
            droneIndicatorMaterials = new Material[droneIndicatorSprites.Count];
            for (int i = 0; i < droneIndicatorMaterials.Length; i++)
            {
                droneIndicatorMaterials[i] = Instantiate(baseMat);
                droneIndicatorMaterials[i].name = droneIndicatorSprites[i].name;
                droneIndicatorMaterials[i].SetTexture("_EmTex", droneIndicatorSprites[i].texture);
            }
        }
        
        private Sprite LoadSpriteFromFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            
            int sprite_width = 100;
            int sprite_height = 100;
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(sprite_width, sprite_height, TextureFormat.RGB24, false);
            texture.LoadImage(bytes);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            return sprite;
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
                        if(!alwaysShowHelp.Value)
                        {
                            GameObject indicator = Instantiate(DroneIndicatorHologram, deadDrone.transform);
                            
                            NetworkServer.Spawn(indicator);
                            
                            deadDroneTracker droneTracker = indicator.GetComponent<deadDroneTracker>();
                            if (droneIndicatorSprites.Count != 0)
                            {
                                int rng = Run.instance.runRNG.RangeInt(0, droneIndicatorSprites.Count);
                                droneTracker.messageID = rng;
                                NetworkIdentity identity = indicator.GetComponent<NetworkIdentity>();
                                if (!identity)
                                {
                                    Log.Warning("indicator did not have a NetworkIdentity component!");
                                    return;
                                }
                                
                                new deadDroneTracker.SyncBaseStats(identity.netId, rng).Send(NetworkDestination.Clients);
                                //new deadDroneTracker.SyncBaseStats(identity.netId, rng).Send(NetworkDestination.Server);
                            }
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

