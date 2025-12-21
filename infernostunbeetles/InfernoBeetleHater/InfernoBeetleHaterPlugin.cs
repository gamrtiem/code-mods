using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using MonoMod.Cil;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace InfernoBeetleHater
{
    [BepInDependency("iDeathHD.UnityHotReload", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("HIFU.Inferno")]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class InfernoBeetleHaterPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "icebro";
        public const string PluginName = "infernobeetlehater";
        public const string PluginVersion = "1.0.0";
        
        public static bool UHRInstalled => Chainloader.PluginInfos.ContainsKey("iDeathHD.UnityHotReload");
        
        public void Awake()
        {
            Log.Init(Logger);
            
            Harmony harmony = new(Info.Metadata.GUID);
            harmony.CreateClassProcessor(typeof(InfernoChanges.InfernoHarmonyPatches)).Patch();
        }
    }

    public class InfernoChanges
    {
        [HarmonyPatch]
        public class InfernoHarmonyPatches
        {
            [HarmonyPatch(typeof(Inferno.Stat_AI.Body), "BodyChanges")]
            [HarmonyILManipulator]
            public static void OnEnterPostFix(ILContext il)
            {
                Log.Debug("ilh ook hate beetle grahhhhh");
                var c = new ILCursor(il);

                if (c.TryGotoNext(x => x.MatchLdsfld(typeof(Inferno.Stat_AI.Body), "beetleIndex")))
                {
                    c.Index += 6;
                    c.RemoveRange(3);
                }
            }
        }
    }
}
