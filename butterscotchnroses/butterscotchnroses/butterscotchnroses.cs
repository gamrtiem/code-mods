using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BNR.patches;
using BNR.items;
using butterscotchnroses;
using HarmonyLib;
using RoR2;
using UnityEngine;
using ShaderSwapper;
using UnityEngine.UI;

namespace BNR
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency("com.Viliger.EnemiesReturns", BepInDependency.DependencyFlags.SoftDependency)]

    public class butterscotchnroses : BaseUnityPlugin
    {
        private const string PluginGUID = "zzz" + PluginAuthor + "." + PluginName;

        private const string PluginAuthor = "icebro";
        private const string PluginName = "BNR";
        private const string PluginVersion = "0.2.0";

        public static AssetBundle carvingKitBundle;
        public static butterscotchnroses instance;
        public static List<PatchBase> patchBases = [];
        public static Harmony harmony;
        
        public void Awake()
        {
            instance = this;
            //TODO add making inferno + ESBM config not give them double jumps TT 
            //TODO add mod options button (uses something different i think idk( and highlighted text color change configfs 
            //TODO cleanesthud color force instead of survivor color 
            //TODO main menu pink color option like wolfo qol 
            Log.Init(Logger);
            Logger.LogDebug("loading mod !!");
            carvingKitBundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location)!, "carvingkit_assets"));
            StartCoroutine(carvingKitBundle.UpgradeStubbedShadersAsync());

            harmony = new Harmony(Info.Metadata.GUID);
            
            var patches = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(PatchBase)));
            foreach (Type patch in patches)
            {
                try
                {
                    PatchBase patchBase = (PatchBase)Activator.CreateInstance(patch);
                    patchBase.Config(Config);
                    patchBase.PreInit();
                    patchBases.Add(patchBase);
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
                BuffBase buff = (BuffBase)Activator.CreateInstance(buffType);
                buff.AddBuff();
            }
            
            var itemTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(ItemBase)));
            foreach (var itemType in itemTypes)
            {
                ItemBase item = (ItemBase)Activator.CreateInstance(itemType);
                item.Init(Config);
            }

            RoR2.Console.CheatsConVar.instance.boolValue = true;
            oldconfigs.fixOldConfigs();
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

        private void FixedUpdate()
        {
            foreach (PatchBase patch in patchBases)
            {
                patch.FixedUpdate();
            }
        }
    }
}