using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using MonarchStarstormEdits.patches;
using SS2;
using UnityEngine;

namespace MonarchStarstormEdits
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(SS2Main.GUID)]
    public class MonarchStarstormEdits : BaseUnityPlugin
    {
        private const string PluginGUID = PluginAuthor + "." + PluginName;
        private const string PluginAuthor = "icebro";
        private const string PluginName = "MonarchStarstormEdits";
        private const string PluginVersion = "0.0.1";

        private static bool UHRInstalled => Chainloader.PluginInfos.ContainsKey("iDeathHD.UnityHotReload");

        public void Awake()
        {
            Log.Init(Logger);
            
            Harmony harmony = new(Info.Metadata.GUID);

            IEnumerable<Type> patches = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(PatchBase)));
            foreach (Type patch in patches)
            {
                PatchBase patchBase = (PatchBase)Activator.CreateInstance(patch);

                try
                {
                    patchBase.Config(Config);
                    patchBase.Init(harmony);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }
        
        private void Update()
        {
#if DEBUG
            if (Input.GetKeyUp(KeyCode.F5))
            {
                if (UHRInstalled)
                {
                    UHRSupport.hotReload(typeof(MonarchStarstormEdits).Assembly, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), "MonarchStarstormEdits.dll"));
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
