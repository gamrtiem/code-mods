using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AK.Wwise;
using BepInEx;
using BNR.patches;
using BNR.items;
using HarmonyLib;
using RoR2;
using SS2.Items;
using UnityEngine;
using UnityEngine.AddressableAssets;
using SceneDirector = On.RoR2.SceneDirector;
using ShaderSwapper;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BNR
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    public class butterscotchnroses : BaseUnityPlugin
    {
        public const string PluginGUID = "zzz" + PluginAuthor + "." + PluginName;

        public const string PluginAuthor = "icebro";
        public const string PluginName = "BNR";
        public const string PluginVersion = "0.1.2";

        public static AssetBundle carvingKitBundle;
        public void Awake()
        {
            //TODO add making inferno + ESBM config not give them double jumps TT 
            //TODO add mod options button (uses something different i think idk( and highlighted text color change configfs 
            //TODO cleanesthud color force instead of survivor color 
            //TODO main menu pink color option like wolfo qol 
            Log.Init(Logger);
            Logger.LogDebug("loading mod !!");
            carvingKitBundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), "carvingkit_assets"));
            StartCoroutine(carvingKitBundle.UpgradeStubbedShadersAsync());

            Harmony harmony = new(Info.Metadata.GUID);
            var patches = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(PatchBase)));
            foreach (Type patch in patches)
            {
                try
                {
                    PatchBase patchBase = (PatchBase)Activator.CreateInstance(patch);
                    patchBase.Config(Config);
                    patchBase.Init(harmony);
                }
                catch (Exception e)
                {
                    Log.Warning("failed to patch something ! probably fine if you dont have whatever mod that was attempted to be patched enabled ,..,,.");
                    Log.Warning(e);
                }
            }
            
            var buffTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(BuffBase)));
            foreach (var buffType in buffTypes)
            {
                BuffBase buff = (BuffBase)System.Activator.CreateInstance(buffType);
                buff.AddBuff();
            }
            
            var itemTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(ItemBase)));
            foreach (var itemType in itemTypes)
            {
                ItemBase item = (ItemBase)System.Activator.CreateInstance(itemType);
                item.Init(Config);
            }
            
            On.RoR2.SceneDirector.Start += SceneDirectorOnStart;
        }

        public static Material skyboxMaterial;
        private void SceneDirectorOnStart(SceneDirector.orig_Start orig, RoR2.SceneDirector self)
        {
            orig(self);
            if (SceneManager.GetActiveScene().name == "testscene")
            {
                Log.Debug($"new scene !! {RenderSettings.skybox} {SceneManager.GetActiveScene().name}");
                
                if (skyboxMaterial != null)
                {
                    skyboxMaterial = Object.Instantiate(RenderSettings.skybox);
                    skyboxMaterial.SetColor("_Tint", BNRUtils.Color255(152, 122, 144, 255));
                }
                
                RenderSettings.skybox = skyboxMaterial;
            }
            
        }

        private void Update()
        {
#if DEBUG
            if (Input.GetKeyUp(KeyCode.F5))
            {
                UnityHotReloadNS.UnityHotReload.LoadNewAssemblyVersion(typeof(butterscotchnroses).Assembly, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location)!, "butterscotchnroses.dll"));
            }
#endif  
        }
        
        private static List<uint> sounds = [];
        [ConCommand(commandName = "play_sound", flags = ConVarFlags.None, helpText = "https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Developer-Reference/Sound-%E2%80%90-Wwise-Events/")]
        public static void akplaysound(ConCommandArgs args)
        {
            Debug.Log("args = " + args[0]);
            Debug.Log($"sender body = {args.senderBody}");
            uint soundid = AkSoundEngine.PostEvent(args[0], ((Component)(object)args.senderBody).gameObject);
            Debug.Log($"sound id = {soundid}");
            sounds.Add(soundid);
        }

        [ConCommand(commandName = "stop_sound", flags = ConVarFlags.None, helpText = "stop looping sounds from playsound !!!!!")]
        public static void akstopsound(ConCommandArgs args)
        {
            Debug.Log($"sender body = {args.senderBody}");
            foreach (uint sound in sounds.ToList())
            {
                AkSoundEngine.StopPlayingID(sound);
                Debug.Log($"stopped sound {sound} !!!");
                sounds.Remove(sound);
            }
        }
        
        [ConCommand(commandName = "play_effect", flags = ConVarFlags.None, helpText = "play an effect !!!!!")]
        [ConCommand(commandName = "spawn_effect", flags = ConVarFlags.None, helpText = "play an effect !!!!!")]
        public static void spawnEffect(ConCommandArgs args)
        {
            Debug.Log("args = " + args[0] + " " + args[1]);
            
            EffectDef effect = null;
            foreach (EffectDef effectDef in EffectCatalog.entries)
            {
                if (!effectDef.prefabName.Contains(args[0])) continue;
                
                effect = effectDef;
                break;
            }

            if (effect == null)
            {
                Log.Warning($"couldnt find effect {args[0]} !!!");
                return;
            }
            
            EffectManager.SpawnEffect(effect.index, new EffectData
            {
                origin = args.senderBody.transform.position,
                scale = Convert.ToSingle(args[1]),
                rotation = Util.QuaternionSafeLookRotation(Vector3.up)
            }, true);
        }
    }
}