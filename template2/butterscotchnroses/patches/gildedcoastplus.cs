using static BNR.butterscotchnroses;
using HarmonyLib;

namespace BNR;

public class gildedcoastplus
{
    [HarmonyPatch]
    public class GoldenCoastChanges
    {
        [HarmonyPatch(typeof(GoldenCoastPlusRevived.Modules.FightChanges), "GoldshoresBossfight_GiveBuff")]
        [HarmonyPrefix]
        public static bool GoldenCoastPlusRevivedGiveBuffPreFix()
        {
            Log.Debug("skipping reward buff !!");
            return !skipGoldenRewards.Value; 
        }
    }
}