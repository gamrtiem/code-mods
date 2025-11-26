using BepInEx.Configuration;
using BNR.patches;
using static BNR.butterscotchnroses;
using HarmonyLib;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.PostProcessing;
namespace BNR;

public class coolereclipse : PatchBase<coolereclipse>
{
    [HarmonyPatch]
    public class CoolerEclipseChanges
    {
        [HarmonyPatch(typeof(CoolerEclipse.CoolerEclipse), "AddSkybox")]
        [HarmonyPrefix]
        public static bool CoolerEclipseAddSkyboxPreFix(On.RoR2.SceneDirector.orig_Start orig, SceneDirector self)
        {
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
        
        [HarmonyPatch(typeof(CoolerEclipse.CoolerEclipse), "AddSkybox")]
        [HarmonyPostfix]
        public static void CoolerEclipseAddSkyboxPostFix(On.RoR2.SceneDirector.orig_Start orig, SceneDirector self)
        {
            int rng = Run.instance.runRNG.RangeInt(0, 100);
            Log.Debug($"pink eclipse ? rng {rng} umm {(rng < pinkEclipseChance.Value)}");
            if (!GameObject.Find("Eclipse")) return;

            //bascially ramp fog stays pink even after ?? shouldnt but reapplying here if we got another eclipse ,.,. 
            GameObject pp = GameObject.Find("PP + Amb");
            if (pp && pp.GetComponent<PostProcessVolume>())
            {
                PostProcessVolume ppv = pp.GetComponent<PostProcessVolume>();
                PostProcessProfile ppf = Object.Instantiate(ppv.sharedProfile);
                
                RampFog rf = ppf.GetSetting<RampFog>();
                rf.fogColorStart.value = new Color32(55, 87, 82, 15); 
                rf.fogColorMid.value = new Color32(54, 74, 89, 100); 
                rf.fogColorEnd.value = new Color32(47, 63, 82, 200);
                
                ppv.sharedProfile = ppf;
            }
            
            if (!(rng < pinkEclipseChance.Value)) return;
            
            if (pp && pp.GetComponent<SetAmbientLight>())
            {
                SetAmbientLight amb = pp.GetComponent<SetAmbientLight>();
                amb.ambientSkyColor = BNRUtils.Color255(207, 97, 182);
                amb.ambientEquatorColor = BNRUtils.Color255(207, 97, 165);
                amb.ambientGroundColor = BNRUtils.Color255(146, 32, 93);
                amb.ApplyLighting();
            }
            if (pp && pp.GetComponent<PostProcessVolume>())
            {
                PostProcessVolume ppv = pp.GetComponent<PostProcessVolume>();
                PostProcessProfile ppf = Object.Instantiate(ppv.sharedProfile);
                
                RampFog rf = ppf.GetSetting<RampFog>();

               // Log.Debug($"start {rf.fogColorStart.value.r * 255} {rf.fogColorStart.value.g * 255} {rf.fogColorStart.value.b * 255} {rf.fogColorStart.value.a * 255}");
                //Log.Debug($"mid {rf.fogColorMid.value.r * 255} {rf.fogColorMid.value.g * 255} {rf.fogColorMid.value.b * 255} {rf.fogColorMid.value.a * 255}");
                //Log.Debug($"end {rf.fogColorEnd.value.r * 255} {rf.fogColorEnd.value.g * 255} {rf.fogColorEnd.value.b * 255} {rf.fogColorEnd.value.a * 255}");
                //coolerstages auterburn
                rf.fogColorStart.value = new Color32(190, 154, 150, 15); 
                rf.fogColorMid.value = new Color32(110, 73, 69, 100); 
                rf.fogColorEnd.value = new Color32(90, 47, 44, 200);

                //these are wayy to strong im thinksies
                //rf.fogColorStart.value = BNRUtils.Color255(100, 51, 102, 18);
                //rf.fogColorMid.value = BNRUtils.Color255(89, 54, 82, 137);
               // rf.fogColorStart.value = BNRUtils.Color255(82, 47, 73, 222);

                ppf.RemoveSettings<ColorGrading>();
                
                ppv.sharedProfile = ppf;
            }
            
            GameObject weather = GameObject.Find("Weather (Locked Position/Rotation)");
            if (weather && weather.transform.Find("Embers"))
            {
                GameObject embers = weather.transform.Find("Embers").gameObject;
                ParticleSystemRenderer particleSystem = embers.GetComponent<ParticleSystemRenderer>();
                
                if (particleSystem)
                {
                    Material newMat = Object.Instantiate(particleSystem.sharedMaterial);
                    Texture2D newRamp = Addressables.LoadAssetAsync<Texture2D>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common_ColorRamps.texRampDiamondLaser_png).WaitForCompletion();
                    newMat.SetTexture("_RemapTex", newRamp);
                    particleSystem.sharedMaterial = newMat;
                }
            }
            
            GameObject stars = GameObject.Find("Sphere, Stars");
            if (stars && stars.GetComponent<MeshRenderer>())
            {
                MeshRenderer starsRenderer = stars.GetComponent<MeshRenderer>();

                if (starsRenderer)
                {
                    Material newMat = Object.Instantiate(starsRenderer.sharedMaterial);
                    Texture2D newRamp = Addressables.LoadAssetAsync<Texture2D>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common_ColorRamps.texRampGolem_png).WaitForCompletion();
                    newMat.SetTexture("_RemapTex", newRamp);
                    starsRenderer.sharedMaterial = newMat;
                }
            }
            
            GameObject stars2 = GameObject.Find("Sphere, Stars 2");
            if (stars2 && stars2.GetComponent<MeshRenderer>())
            {
                MeshRenderer starsRenderer = stars2.GetComponent<MeshRenderer>();

                if (starsRenderer)
                {
                    Material newMat = Object.Instantiate(starsRenderer.sharedMaterial);
                    Texture2D newRamp = Addressables.LoadAssetAsync<Texture2D>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common_ColorRamps.texRampGolem_png).WaitForCompletion();
                    newMat.SetTexture("_RemapTex", newRamp);
                    starsRenderer.sharedMaterial = newMat;
                }
            }
            
            GameObject moon = GameObject.Find("Sphere, Moon");
            if (moon && moon.GetComponent<MeshRenderer>())
            {
                MeshRenderer moonRenderer = moon.GetComponent<MeshRenderer>();

                if (moonRenderer)
                {
                    Material newMat = Object.Instantiate(moonRenderer.sharedMaterial);
                    Texture2D newRamp = Addressables.LoadAssetAsync<Texture2D>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common_ColorRamps.texRampArchWisp_png).WaitForCompletion();
                    newMat.SetTexture("_RemapTex", newRamp);
                    newMat.SetColor("_TintColor", new Vector4(149 / 255f, 71 / 255f, 75 / 255f, 1));
                    newMat.SetColor("_SpecColor", new Vector4(255 / 255f, 0 / 255f, 0 / 255f, 1));
                    moonRenderer.sharedMaterial = newMat;
                }
            }
            
            GameObject eclipse = GameObject.Find("Eclipse");
            if (eclipse && eclipse.GetComponent<MeshRenderer>())
            {
                MeshRenderer eclipseRenderer = eclipse.GetComponent<MeshRenderer>();

                if (eclipseRenderer)
                {
                    Material newMat = Object.Instantiate(eclipseRenderer.sharedMaterial);
                    Texture2D newRamp = Addressables.LoadAssetAsync<Texture2D>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common_ColorRamps.texRampArchWisp_png).WaitForCompletion();
                    newMat.SetTexture("_RemapTex", newRamp);
                    //brightness boost
                    eclipseRenderer.sharedMaterial = newMat;
                }
            }
            
        }
    }

    public override void Init(Harmony harmony)
    {
        if (!applyCE.Value) return;
        harmony.CreateClassProcessor(typeof(CoolerEclipseChanges)).Patch();
    }

    public override void Config(ConfigFile config)
    {
        applyCE = config.Bind("apply patches",
            "try to apply cooler eclipse patches !!",
            true,
            "");
        BNRUtils.CheckboxConfig(applyCE);
        
        eclipseChance = config.Bind("coolerEclipse", 
                "chance for eclipse", 
                15f, 
                "bwaa,  (0-100 !!!");
        BNRUtils.SliderConfig(0, 100, eclipseChance);
        
        pinkEclipseChance = config.Bind("coolerEclipse", 
            "chance for pink eclipse if enabled !", 
            50f, 
            "bwaa,  (0-100 !!! if regular eclipse is rolled rolls this percent chance on top .,,. set to 0 to disable !!");
        BNRUtils.SliderConfig(0, 100, pinkEclipseChance);
        
        
        blacklistStages = config.Bind("coolerEclipse", 
            "stage blacklist", 
            "goldshores,bazaar,solutionalhaunt", 
            "eclipse stage blacklist (seperate by , !! (eg golemplains,blackbeach!!");
        BNRUtils.StringConfig(blacklistStages);
        
        
        whitelistStages = config.Bind("coolerEclipse", 
            "stage whitelist", 
            "titanicplains", 
            "what stages to force eclipses on (seperate by , !! (eg golemplains,blackbeach!! will not work with moon2, ,..");
        BNRUtils.StringConfig(whitelistStages);
    }
    
    public static ConfigEntry<bool> applyCE;
    public static ConfigEntry<float> eclipseChance;
    public static ConfigEntry<float> pinkEclipseChance;
    public static ConfigEntry<string> blacklistStages;
    public static ConfigEntry<string> whitelistStages;
}