using BepInEx.Configuration;
using BNR.patches;
using static BNR.butterscotchnroses;
using HarmonyLib;

namespace BNR;

public class gildedcoastplus : PatchBase<gildedcoastplus>
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

    public override void Init(Harmony harmony)
    {
        if (!applyGCP.Value) return;
        harmony.CreateClassProcessor(typeof(GoldenCoastChanges)).Patch();
    }

    public override void Config(ConfigFile config)
    {
        applyGCP = config.Bind("apply patches",
            "try to apply gilded coast plus reival patches !!",
            true,
            "");
        BNRUtils.CheckboxConfig(applyGCP);
        
        skipGoldenRewards = config.Bind("golden coast plus", 
            "skip hidden buff", 
            true, 
            "");
        BNRUtils.CheckboxConfig(skipGoldenRewards);
    }
    
    public static ConfigEntry<bool> skipGoldenRewards;
    public static ConfigEntry<bool> applyGCP;
}