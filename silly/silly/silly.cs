using BepInEx;
using BepInEx.Bootstrap;
using RoR2;
using UnityEngine;

namespace silly
{
    [BepInDependency("iDeathHD.UnityHotReload", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class silly : BaseUnityPlugin
    {
        private const string PluginGUID = PluginAuthor + "." + PluginName;
        private const string PluginAuthor = "icebro";
        private const string PluginName = "silly";
        private const string PluginVersion = "1.0.0";
        
        public void Awake()
        {
            Log.Init(Logger);

            On.RoR2.RoR2Application.OnLoad += (orig, self) =>
            {
                AssetPaths.UpdateAssetPathsToNames();
                return orig(self);
            };
            
            Edits.addEdits();
        }
        
        [ConCommand(commandName = "type_test", flags = ConVarFlags.None, helpText = "bwaa")]
        public static void typetest(ConCommandArgs args)
        {
            Log.Debug("args = " + args[0]);
            Log.Debug($"type !! = {Utils.GetType(args[0]).Name}");
        }
    }
}
