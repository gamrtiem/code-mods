using System;
using System.Linq;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MSU.Config;
using On.EntityStates.AffixVoid;
using RoR2;
using RoR2BepInExPack.GameAssetPaths;
using SS2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.TextCore.Text;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace MonarchStarstormEdits.patches;

public class eliteEvents : PatchBase<eliteEvents>
{
    [HarmonyPatch]
    public class Starstorm2EliteEventChanges
    {
        [HarmonyPatch(typeof(EliteEventMissionController), "SpawnBoss")]
        [HarmonyPrefix]
        public static bool EliteEventMissionControllerPrefix(EliteEventMissionController __instance)
        {
            if (!preventBossSpawns.Value) return true;
            
            __instance.hasSpawnedBoss = true; // useless since we just end the event anyway but hehe ,. 
            __instance.StopEvent(); 
                
            return false;
        }
        
        [HarmonyPatch(typeof(EventDirector), "CreateEventTimeline")]
        [HarmonyILManipulator]
        public static void EventDirectorIL(ILContext il)
        {
            if (disableEliteEvents.Value == false) return;
            
            Log.Debug("fkgndflkgdfg ,,.");
            var c = new ILCursor(il);
            ILLabel _ilLabel = null;
            
            /*
            // if (ConfiguredVariable<bool>.op_Implicit((ConfiguredVariable<bool>)(object)SS2Config.enableBeta) && flag)
            IL_005a: stloc.2
            IL_005b: ldsfld class [MSU.Runtime]MSU.Config.ConfiguredBool SS2.SS2Config::enableBeta
            IL_0060: call !0 class [MSU.Runtime]MSU.Config.ConfiguredVariable`1<bool>::op_Implicit(class [MSU.Runtime]MSU.Config.ConfiguredVariable`1<!0>)
            IL_0065: ldloc.2
            IL_0066: and
            IL_0067: brfalse IL_01b8
            */

            if (c.TryGotoNext(x => x.MatchStloc(2),
                x => x.MatchLdsfld("SS2.SS2Config", "enableBeta"),
                x => x.MatchCall(typeof(MSU.Config.ConfiguredVariable<>).MakeGenericType(typeof(bool)).GetMethod("op_Implicit")), // had to use duck ai for this im going to kms ,.,. 
                x => x.MatchLdloc(2),
                x => x.MatchAnd(),
                x => x.MatchBrfalse(out _ilLabel)))
            {
                c.Index += 6;

                c.Emit(OpCodes.Ldc_I4_0);
                c.Emit(OpCodes.Brfalse, _ilLabel);

                Log.Debug("managed to ilhook elite events !! they will never show up ever again .,,.");
            }
            else
            {
                Log.Warning("failed il hook elite events!!");
            }
        }
    }

    public override void Init(Harmony harmony)
    {
        if (!eliteEventChanges.Value) return;
        harmony.CreateClassProcessor(typeof(Starstorm2EliteEventChanges)).Patch();
    }

    public override void Config(ConfigFile config)
    {
        eliteEventChanges = config.Bind("elite event change s,s,.,.", 
            "apply any elite event changes at al l !!", 
            true,
            "byeah ,.,");
        disableEliteEvents = config.Bind("elite event change s,s,.,.", 
            "disable elite events entirely !!!!", 
            false,
            "byeah ,.,");
        preventBossSpawns = config.Bind("elite event change s,s,.,.", 
            "disable boss spawns during elite events ,..,", 
            false,
            "byeah ,.,");
    }
    
    private static ConfigEntry<bool> eliteEventChanges;
    private static ConfigEntry<bool> disableEliteEvents;
    private static ConfigEntry<bool> preventBossSpawns;
}