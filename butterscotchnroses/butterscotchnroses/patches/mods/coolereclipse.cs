using System;
using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using BNR.patches;
using static BNR.butterscotchnroses;
using HarmonyLib;
using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Stage = On.RoR2.Stage;

namespace BNR;

public class coolereclipse : PatchBase<coolereclipse>
{
    public override string chainLoaderKey => "com.Nuxlar.CoolerEclipse";
    private static int eclipseRNG;
    private static bool pinkEclipse;
    private static Texture2D particleSystemRamp;
    private static Texture2D starRamp;
    private static Texture2D moonRamp;
    private static Material particleSystemMat;
    private static Material starsMat;
    private static Material starsMat2;
    private static Material eclipseMat;
    private static Material moonMat;
    private static ColorGrading colorGrading;
    
    [HarmonyPatch]
    public class CoolerEclipseChanges
    {
        [HarmonyPatch(typeof(CoolerEclipse.CoolerEclipse), "AddSkybox")]
        [HarmonyPrefix]
        public static bool CoolerEclipseAddSkyboxPreFix(On.RoR2.SceneDirector.orig_Start orig, SceneDirector self)
        {
            CoolerEclipse.CoolerEclipse.shouldBeChance.Value = false;
            
            int eclipseRNG;
            bool pinkEclipse;
            if (GameObject.Find("eclipse handler(Clone)")?.TryGetComponent(out EclipseNetworkBehavior eclipseNetworkBehavior) == true)
            {
                eclipseRNG = eclipseNetworkBehavior.eclipseRNGs[Run.instance ? Run.instance.stageClearCount : 0];
                pinkEclipse = eclipseNetworkBehavior.pinkEclipses[Run.instance ? Run.instance.stageClearCount : 0];
                Log.Debug($"rng {eclipseRNG} pink {pinkEclipse} run null {Run.instance == null}");
            }
            else
            {
                Log.Debug("unable to find eclipse handler !! ");
                orig(self);
                return false;
            }
            
            string sceneName = SceneManager.GetActiveScene().name;
           // int rng = Run.instance.runRNG.RangeInt(0, 100);

            if (!whitelistStages.Value.IsNullOrWhiteSpace())
            {
                foreach (string stage in whitelistStages.Value.Split(","))
                {
                    if (!sceneName.Contains(stage)) continue;
                
                    Log.Debug($"{stage} is in whitelist !! forcing !!");
                    return true;
                }
            }
            
            if (!blacklistStages.Value.IsNullOrWhiteSpace())
            {
                foreach (string stage in blacklistStages.Value.Split(","))
                {
                    if (!sceneName.Contains(stage)) continue;
                
                    Log.Debug($"name {stage} is in config !! skipping !!");
                    orig(self);
                    return false;
                }
            }
            
            Log.Debug($"test eclipse !! rng {eclipseRNG} < {eclipseChance.Value} ,,,.. applying ? {eclipseRNG > eclipseChance.Value}");
            if (eclipseRNG > eclipseChance.Value)
            {
                orig(self);
                return false;
            }
            
            return true; 
        }
        
        [HarmonyPatch(typeof(CoolerEclipse.CoolerEclipse), "AddSkybox")]
        [HarmonyPostfix]
        public static void CoolerEclipseAddSkyboxPostFix(On.RoR2.SceneDirector.orig_Start orig, SceneDirector self)
        {
            if (!GameObject.Find("Eclipse")) return;
            
            int eclipseRNG;
            bool pinkEclipse;
            if (GameObject.Find("eclipse handler(Clone)")?.TryGetComponent(out EclipseNetworkBehavior eclipseNetworkBehavior) == true)
            {
                eclipseRNG = eclipseNetworkBehavior.eclipseRNGs[Run.instance ? Run.instance.stageClearCount : 0];
                pinkEclipse = eclipseNetworkBehavior.pinkEclipses[Run.instance ? Run.instance.stageClearCount : 0];
                Log.Debug($"rng {eclipseRNG} pink {pinkEclipse} run null {Run.instance == null}");
            }
            else
            {
                Log.Debug("unable to find eclipse handler !! ");
                return;
            }
            
            GameObject pp = GameObject.Find("PP + Amb");
            if (pp.TryGetComponent(out PostProcessVolume ppvBase))
            {
                RampFog rf = ppvBase.sharedProfile.GetSetting<RampFog>();
                rf.fogColorStart.value = new Color32(55, 87, 82, 15); 
                rf.fogColorMid.value = new Color32(54, 74, 89, 100); 
                rf.fogColorEnd.value = new Color32(47, 63, 82, 200);
                if (!colorGrading)
                {
                    colorGrading = ppvBase.sharedProfile.GetSetting<ColorGrading>();
                }
                //ppvBase.sharedProfile.AddSettings<ColorGrading>();
                if (!ppvBase.sharedProfile.HasSettings<ColorGrading>())
                {
                    ppvBase.sharedProfile.AddSettings<ColorGrading>();
                    ppvBase.sharedProfile.settings[ppvBase.sharedProfile.settings.FindIndex(settings => settings is ColorGrading)] = colorGrading;
                }
            }
            
            if (!pinkEclipse) return;
            
            if (pp.TryGetComponent(out SetAmbientLight amb))
            {
                
                amb.ambientSkyColor = Utils.Color255(207, 97, 182);
                amb.ambientEquatorColor = Utils.Color255(207, 97, 165);
                amb.ambientGroundColor = Utils.Color255(146, 32, 93);
                amb.ApplyLighting();
            }
            
            if (pp.TryGetComponent(out PostProcessVolume ppv))
            {
                RampFog rf = ppv.sharedProfile.GetSetting<RampFog>();

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
               //ppv.sharedProfile.GetSetting<ColorGrading>().
               if (!colorGrading)
               {
                   colorGrading = ppvBase.sharedProfile.GetSetting<ColorGrading>();
               }
               ppv.sharedProfile.RemoveSettings<ColorGrading>();
            }
            
            GameObject weather = GameObject.Find("Weather (Locked Position/Rotation)");
            if (weather && weather.transform.Find("Embers"))
            {
                GameObject embers = weather.transform.Find("Embers").gameObject;
                
                if (embers.TryGetComponent(out ParticleSystemRenderer particleSystem))
                {
                    if (particleSystemMat == null)
                    {
                        particleSystemMat = Object.Instantiate(particleSystem.sharedMaterial);
                        particleSystemMat.SetTexture(RemapTex, particleSystemRamp);
                    }

                    particleSystem.sharedMaterial = particleSystemMat;
                }
            }
            
            GameObject stars = GameObject.Find("Sphere, Stars");
            if (stars.TryGetComponent(out MeshRenderer starsRenderer))
            {
                if (starsRenderer)
                {
                    if (starsMat == null)
                    {
                        starsMat = Object.Instantiate(starsRenderer.sharedMaterial);
                        starsMat.SetTexture(RemapTex, starRamp);
                    }
                   
                    starsRenderer.sharedMaterial = starsMat;
                }
            }
            
            GameObject stars2 = GameObject.Find("Sphere, Stars 2");
            if (stars2.TryGetComponent(out MeshRenderer starsRenderer2))
            {
                if (starsRenderer2)
                {
                    if (starsMat2 == null)
                    {
                        starsMat2 = Object.Instantiate(starsRenderer2.sharedMaterial);
                        starsMat2.SetTexture(RemapTex, starRamp);
                    }
                    
                    starsRenderer2.sharedMaterial = starsMat2;
                }
            }
            
            GameObject moon = GameObject.Find("Sphere, Moon");
            if (moon.TryGetComponent(out MeshRenderer moonRenderer))
            {
                if (moonRenderer)
                {
                    if (moonMat == null)
                    {
                        moonMat = Object.Instantiate(moonRenderer.sharedMaterial);
                        moonMat.SetTexture(RemapTex, moonRamp);
                        moonMat.SetColor(TintColor, new Vector4(149 / 255f, 71 / 255f, 75 / 255f, 1));
                        moonMat.SetColor(SpecColor, new Vector4(255 / 255f, 0 / 255f, 0 / 255f, 1));
                    }
                    
                    moonRenderer.sharedMaterial = moonMat;
                }
            }
            
            GameObject eclipse = GameObject.Find("Eclipse");
            if (eclipse.TryGetComponent(out MeshRenderer eclipseRenderer))
            {
                if (eclipseRenderer)
                {
                    if (eclipseMat == null)
                    {
                        eclipseMat = Object.Instantiate(eclipseRenderer.sharedMaterial);
                        eclipseMat.SetTexture(RemapTex, moonRamp);
                    }
                    
                    //brightness boost
                    eclipseRenderer.sharedMaterial = eclipseMat;
                }
            }
        }
    }

    public static GameObject eclipseHandler;
    public override void Init()
    {
        if (!applyCE.Value) return;
        
        harmony.CreateClassProcessor(typeof(CoolerEclipseChanges)).Patch();
        RoR2.Run.onRunStartGlobal += RunOnonRunStartGlobal;
        Log.Debug("ptached cooler eclipse !!");
        
        eclipseHandler = PrefabAPI.CreateEmptyPrefab("eclipse handler", true);
        eclipseHandler.AddComponent<EclipseNetworkBehavior>();
        
        Addressables.LoadAssetAsync<Texture2D>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common_ColorRamps.texRampGolem_png).Completed += handle => { starRamp = handle.Result; };
        Addressables.LoadAssetAsync<Texture2D>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common_ColorRamps.texRampArchWisp_png).Completed += handle => { moonRamp = handle.Result; };
        Addressables.LoadAssetAsync<Texture2D>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common_ColorRamps.texRampDiamondLaser_png).Completed += handle => { particleSystemRamp = handle.Result; };
    }

    private void RunOnonRunStartGlobal(Run obj)
    {
        GameObject eclipseHandler = GameObject.Find("eclipse handler(Clone)");
        if (eclipseHandler)
        {
            Object.Destroy(eclipseHandler);
        }

        if (NetworkServer.active)
        {
            eclipseHandler = Object.Instantiate(coolereclipse.eclipseHandler);
            NetworkServer.Spawn(eclipseHandler);
        }
        
    }

    public override void Config(ConfigFile config)
    {
        applyCE = config.Bind("Mods - CoolerEclipse",
            "apply cooler eclipse patches !!",
            true,
            "");
        Utils.CheckboxConfig(applyCE);
        
        eclipseChance = config.Bind("Mods - CoolerEclipse", 
                "chance for eclipse", 
                15f, 
                "bwaa,  (0-100 !!!");
        Utils.SliderConfig(0, 100, eclipseChance);
        
        pinkEclipseChance = config.Bind("Mods - CoolerEclipse", 
            "chance for pink eclipse if enabled !", 
            50f, 
            "bwaa,  (0-100 !!! if regular eclipse is rolled rolls this percent chance on top .,,. set to 0 to disable !!");
        Utils.SliderConfig(0, 100, pinkEclipseChance);
        
        
        blacklistStages = config.Bind("Mods - CoolerEclipse", 
            "stage blacklist", 
            "goldshores,bazaar,solutionalhaunt,ss2_voidshop,testscene,voidraid,arena", 
            "eclipse stage blacklist (seperate by , !! (eg golemplains,blackbeach!!");
        Utils.StringConfig(blacklistStages);
        
        
        whitelistStages = config.Bind("Mods - CoolerEclipse", 
            "stage whitelist", 
            "", 
            "what stages to force eclipses on (seperate by , !! (eg golemplains,blackbeach!! will not work with moon2, ,..");
        Utils.StringConfig(whitelistStages);
    }

    private static ConfigEntry<bool> applyCE;
    private static ConfigEntry<float> eclipseChance;
    public static ConfigEntry<float> pinkEclipseChance;
    public static ConfigEntry<string> blacklistStages;
    public static ConfigEntry<string> whitelistStages;
    private static readonly int RemapTex = Shader.PropertyToID("_RemapTex");
    private static readonly int TintColor = Shader.PropertyToID("_TintColor");
    private static readonly int SpecColor = Shader.PropertyToID("_SpecColor");
}

class EclipseNetworkBehavior() : NetworkBehaviour
{
    public SyncListInt eclipseRNGs = new SyncListInt();
    public SyncListBool pinkEclipses = new SyncListBool();

    private void Awake()
    {
        Log.Debug("asdas343d");
        Object.DontDestroyOnLoad(this);
    }

    private void Start()
    {
        

    }

    private void OnServerInitialized()
    {
        
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        
        Log.Debug($"rng null {eclipseRNGs == null}");
        
        if (NetworkServer.active)
        {
            for (int i = 0; i < 10; i++)
            {
                eclipseRNGs.Add(Random.RandomRangeInt(0, 100));
                pinkEclipses.Add(Random.RandomRangeInt(0, 100) > coolereclipse.pinkEclipseChance.Value);
            }
        }
        Log.Debug($"rng count {eclipseRNGs.Count}");
        Log.Debug($"rng count {eclipseRNGs[0]}");
        Log.Debug($"rng null {eclipseRNGs == null}");
    }


    private void OnEnable()
    {
        Log.Debug("asdasd");
        On.RoR2.Stage.Start += StageOnStart;
    }
    
    bool ranFixedUpdate = false;

    private void FixedUpdate()
    {
        if (!ranFixedUpdate)
        {
            
            ranFixedUpdate = true;
        }
    }

    private void start()
    {
        
    }
    private void OnDisable()
    {
        On.RoR2.Stage.Start -= StageOnStart;
    }

    private IEnumerator StageOnStart(Stage.orig_Start orig, RoR2.Stage self)
    {
        if (NetworkServer.active)
        {
            int rng = Run.instance.runRNG.RangeInt(0, 100);
            Log.Debug($"eclipse rng {rng}");
            int pinkRNG = Run.instance.runRNG.RangeInt(0, 100);
            Log.Debug($"pink rng {(pinkRNG > coolereclipse.pinkEclipseChance.Value)}");
            
            eclipseRNGs.Add(rng);
            pinkEclipses.Add((pinkRNG > coolereclipse.pinkEclipseChance.Value));
            
            //new SyncEclipse(eclipseRNG, pinkEclipse).Send(NetworkDestination.Clients);
        }
        
        return orig(self);
    }
}