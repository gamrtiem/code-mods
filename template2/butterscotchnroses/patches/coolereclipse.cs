using static BNA.butterscotchnroses;
using HarmonyLib;
using RoR2;
using UnityEngine.SceneManagement;

namespace BNA;

public class coolereclipse
{
    [HarmonyPatch]
    public class CoolerEclipseChanges
    {
        [HarmonyPatch(typeof(CoolerEclipse.CoolerEclipse), "AddSkybox")]
        [HarmonyPrefix]
        public static bool CoolerEclipseAddSkyboxPreFix(On.RoR2.SceneDirector.orig_Start orig, SceneDirector self)
        {
            //__result = "test";
            CoolerEclipse.CoolerEclipse.shouldBeChance.Value = false;
            string sceneName = SceneManager.GetActiveScene().name;
            int rng = Run.instance.runRNG.RangeInt(0, 100);

            Log.Debug($"test ecluipse !! rng {rng} < {eclipseChance.Value} ,,,.. applying ? {rng < eclipseChance.Value}");
            if (!(rng < eclipseChance.Value))
            {
                orig(self);
                return false;
            }

            if(!blacklistStages.Value.Equals(""))
            {
                foreach (var stage in whitelistStages.Value.Split(","))
                {
                    if (sceneName.Contains(stage))
                    {
                        Log.Debug($"{stage} is in whitelist !! forcing !!");
                        return true;
                    }
                }
                foreach (var stage in blacklistStages.Value.Split(","))
                {
                    if (sceneName.Contains(stage))
                    {
                        Log.Debug($"name {stage} is in config !! skipping !!");
                        orig(self);
                        return false;
                    }
                }
            }
            
            Log.Debug($"appling eclipse to {sceneName}");
            return true; 
        }
    }
}