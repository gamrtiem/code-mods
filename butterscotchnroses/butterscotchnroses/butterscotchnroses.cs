using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BNR.patches;
using BNR.items;
using butterscotchnroses;
using butterscotchnroses.artifacts;
using butterscotchnroses.skills;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Networking;
using R2API.Utils;
using UnityEngine;
using ShaderSwapper;

namespace BNR
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency("com.Viliger.EnemiesReturns", BepInDependency.DependencyFlags.SoftDependency)]
    public class butterscotchnroses : BaseUnityPlugin
    {
        private const string PluginGUID = "zzz" + PluginAuthor + "." + PluginName;

        private const string PluginAuthor = "icebro";
        private const string PluginName = "BNR";
        private const string PluginVersion = "0.3.0";

        public static AssetBundle carvingKitBundle;
        public static AssetBundle redmanBundle;
        public static AssetBundle bnrBundle;
        public static butterscotchnroses instance;
        public static List<PatchBase> patchBases = [];
        public static Harmony harmony;
        public static ConfigEntry<bool> clientSide;
        public void Awake()
        {
            //TODO add making inferno + ESBM config not give them double jumps TT 
            //TODO add mod options button (uses something different i think idk( and highlighted text color change configfs 
            //TODO cleanesthud color force instead of survivor color 
            //TODO main menu pink color option like wolfo qol 
            
            instance = this;
            Log.Init(Logger);
            Logger.LogDebug("loading mod !!");
            
            clientSide = Config.Bind("BNR", "client side", false, "whether the mod should run in clientside mode or not .,,. disables content/changed behavior that would otherwise cause desyncs automatically !!!");

            carvingKitBundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location)!, "carvingkit_assets"));
            StartCoroutine(carvingKitBundle.UpgradeStubbedShadersAsync());
            bnrBundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location)!, "bnrbundle"));

            harmony = new Harmony(Info.Metadata.GUID);
            
            IEnumerable<Type> patches = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(PatchBase)));
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

            if (!clientSide.Value)
            {
                IEnumerable<Type> buffTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(BuffBase)));
                foreach (Type buffType in buffTypes)
                {
                    BuffBase buff = (BuffBase)Activator.CreateInstance(buffType);
                    buff.AddBuff();
                }
            
                IEnumerable<Type> itemTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(ItemBase)));
                foreach (Type itemType in itemTypes)
                {
                    ItemBase item = (ItemBase)Activator.CreateInstance(itemType);
                    item.Init(Config);
                }
            
                IEnumerable<Type> skills = Assembly.GetExecutingAssembly().GetTypes().Where(x => !x.IsAbstract && x.IsSubclassOf(typeof(SkillBase)));
                //Log.Debug($"skills loaded: {skills.Count()}");
                foreach (Type skill in skills) {
                    SkillBase skillBase = (SkillBase)Activator.CreateInstance(skill);
                    skillBase.Init();
                }
            
                IEnumerable<Type> artifacts = Assembly.GetExecutingAssembly().GetTypes().Where(x => !x.IsAbstract && x.IsSubclassOf(typeof(ArtifactBase)));
                //Log.Debug($"skills loaded: {skills.Count()}");
                foreach (Type artifact in artifacts) {
                    ArtifactBase artifactBase = (ArtifactBase)Activator.CreateInstance(artifact);
                    artifactBase.Init(Config);
                }
            }
            
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
            foreach (PatchBase patch in patchBases)
            {
                patch.Update();
            }
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