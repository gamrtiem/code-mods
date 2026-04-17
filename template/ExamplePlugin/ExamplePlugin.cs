using BepInEx;
using BepInEx.Bootstrap;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ExamplePlugin
{
    [BepInDependency("iDeathHD.UnityHotReload", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class ExamplePlugin : BaseUnityPlugin
    {
        private const string PluginGUID = PluginAuthor + "." + PluginName;
        private const string PluginAuthor = "icebro";
        private const string PluginName = "ExamplePlugin";
        private const string PluginVersion = "1.0.0";

        private static bool UHRInstalled => Chainloader.PluginInfos.ContainsKey("iDeathHD.UnityHotReload");
        
        public void Awake()
        {
            Log.Init(Logger);
        }
        
        private void Update()
        {
#if DEBUG
            if (Input.GetKeyUp(KeyCode.F8))
            {
                if (UHRInstalled)
                {
                    UHRSupport.hotReload(typeof(ExamplePlugin).Assembly, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), "ExamplePlugin.dll"));
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
