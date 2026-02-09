using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using BNR.patches;
using BNR.items;
using HarmonyLib;
using UnityEngine;


namespace BNR
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    public class butterscotchnroses : BaseUnityPlugin
    {
        public const string PluginGUID = "zzz" + PluginAuthor + "." + PluginName;

        public const string PluginAuthor = "icebro";
        public const string PluginName = "BNR";
        public const string PluginVersion = "0.1.1";

        public void Awake()
        {
            //TODO add making inferno + ESBM config not give them double jumps TT 
            //TODO add mod options button (uses something different i think idk( and highlighted text color change configfs 
            //TODO cleanesthud color force instead of survivor color 
            //TODO main menu pink color option like wolfo qol 
            Log.Init(Logger);
            Logger.LogDebug("loading mod !!");
            
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
    }
}