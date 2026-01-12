using BepInEx.Configuration;
using HarmonyLib;
using RoR2;
using SS2;

namespace MonarchStarstormEdits.patches;

public class etherealUpgradeChanges : PatchBase<etherealUpgradeChanges>
{
    [HarmonyPatch]
    public class Starstorm2EtherealUpgradeChanges
    {
        [HarmonyPatch(typeof(EtherealBehavior), "UpdateDifficulty")]
        [HarmonyPrefix]
        public static bool BeginTradePrefix(EtherealBehavior __instance)
        {
            DifficultyDef curDiff = DifficultyCatalog.GetDifficultyDef(__instance.run.selectedDifficulty);
            Log.Debug(curDiff + "current dif f,..,.,,. name token = " + curDiff.nameToken);
                
            if (!curDiff.nameToken.Contains("ECLIPSE")) return true;
                
            if (__instance.run.selectedDifficulty != DifficultyIndex.Eclipse8 && etherealEclipseUpgrade.Value)
            {
                Log.Debug("upgrading eclipse number !! ");
                
                __instance.run.selectedDifficulty = __instance.run.selectedDifficulty switch
                {
                    DifficultyIndex.Eclipse1 => DifficultyIndex.Eclipse2,
                    DifficultyIndex.Eclipse2 => DifficultyIndex.Eclipse3,
                    DifficultyIndex.Eclipse3 => DifficultyIndex.Eclipse4,
                    DifficultyIndex.Eclipse4 => DifficultyIndex.Eclipse5,
                    DifficultyIndex.Eclipse5 => DifficultyIndex.Eclipse6,
                    DifficultyIndex.Eclipse6 => DifficultyIndex.Eclipse7,
                    DifficultyIndex.Eclipse7 => DifficultyIndex.Eclipse8,
                    _ => __instance.run.selectedDifficulty
                };
            }
            else
            {
                Log.Debug("e 8 ,.,., stacking instead !! ");
            }
            curDiff.scalingValue = EtherealDifficulty.GetDefaultScaling(__instance.run.selectedDifficulty) + etherealEclipseUpgradeStack.Value;

            return false;
        }
    }

    public override void Init(Harmony harmony)
    {
        if (!etherealEclipseChanges.Value) return;
        harmony.CreateClassProcessor(typeof(Starstorm2EtherealUpgradeChanges)).Patch();
    }

    public override void Config(ConfigFile config)
    {
        etherealEclipseChanges = config.Bind("ethereal stacking change s,s,.,.", 
            "apply any ethereal eclipse changes at al l!!!", 
            true,
            "byeah ,.,");
        etherealEclipseUpgrade = config.Bind("ethereal stacking change s,s,.,.", 
            "upgrade eclipse difficulty levels (eg e1 -> e2 !!", 
            true,
            "byeah ,.,");
        etherealEclipseUpgradeStack = config.Bind("ethereal stacking change s,s,.,.", 
            "amount to upgrade scaling difficulty by alongside eclipse upgrade (default is 0.5 like regular ethereal stacking.,,. applied alongside e1 -> e2 if thats also enabled,.,..", 
            0.5f,
            "byeah ,.,");
    }
    
    private static ConfigEntry<bool> etherealEclipseChanges;
    private static ConfigEntry<bool> etherealEclipseUpgrade;
    private static ConfigEntry<float> etherealEclipseUpgradeStack;
}