using BepInEx.Configuration;
using BNR.patches;
using static BNR.butterscotchnroses;
using HarmonyLib;
using MonoMod.Cil;

namespace BNR;

public class inferno : PatchBase<inferno>
{
    [HarmonyPatch]
    public class InfernoChanges
    {
        [HarmonyPatch]
        public class InfernoHarmonyPatches
        {
            [HarmonyPatch(typeof(Inferno.Stat_AI.Body), "BodyChanges")]
            [HarmonyILManipulator]
            public static void OnEnterPostFix(ILContext il)
            {
                if (!noBeetleStun.Value)
                {
                    return;
                }
                
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

    public override void Init(Harmony harmony)
    {
        if (!applyInferno.Value) return;
        harmony.CreateClassProcessor(typeof(InfernoChanges)).Patch();
    }

    public override void Config(ConfigFile config)
    {
        applyInferno = config.Bind("Mods - Inferno",
            "apply golden coast plus patches !!",
            true,
            "");
        BNRUtils.CheckboxConfig(applyInferno);
        
        noBeetleStun = config.Bind("Mods - Inferno", 
            "makes beetles able to be stunned i hate beetle stun sm ,,.., ", 
            true, 
            "");
        BNRUtils.CheckboxConfig(noBeetleStun);
    }
    
    public static ConfigEntry<bool> noBeetleStun;
    public static ConfigEntry<bool> applyInferno;
}