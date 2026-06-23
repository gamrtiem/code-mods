using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using kinatoolkit.patches;
using UnityEngine;

namespace kinatoolkit
{
    [BepInDependency(DebugToolkit.DebugToolkit.GUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class kinatoolkit : BaseUnityPlugin
    {
        private const string PluginGUID = PluginAuthor + "." + PluginName;

        private const string PluginAuthor = "kina";
        private const string PluginName = "kinaToolkit";
        private const string PluginVersion = "0.1.0";
        
        public static kinatoolkit instance;
        
        public void Awake()
        {
            instance = this;
            Log.Init(Logger);
            
            commands.Init();
            
            IEnumerable<Type> patches = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(PatchBase)));
            foreach (Type patch in patches)
            {
                try
                {
                    PatchBase patchBase = (PatchBase)Activator.CreateInstance(patch);
                    patchBase.Config(Config);
                    patchBase.PreInit();
                }
                catch (Exception e)
                {
                    Log.Warning("failed to patch something ! probably fine if you dont have whatever mod that was attempted to be patched enabled ,..,,.");
                    Log.Warning(e);
                }
            }
        }

        private void Update()
        {
#if DEBUG
            if (Input.GetKeyUp(KeyCode.F7))
            {
                UnityHotReloadNS.UnityHotReload.LoadNewAssemblyVersion(typeof(kinatoolkit).Assembly, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location)!, "kinatoolkit.dll"));
            }
#endif  
        }
    }
}