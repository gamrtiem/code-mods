using BNR.patches;
using BepInEx.Configuration;
using HarmonyLib;
using On.RoR2;
using On.RoR2.UI;
using RiskOfOptions;
using RiskOfOptions.Options;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BNR;

public class testsceneskybox : PatchBase<testsceneskybox>
{
    public override void Init(Harmony harmony)
    {
        applyHooks();
    }

    private void applyHooks()
    {
        if (enabled.Value)
        {
            On.RoR2.SceneDirector.Start += SceneDirectorOnStart;
        }
        else
        {
            On.RoR2.SceneDirector.Start -= SceneDirectorOnStart;
        }
    }
    
    private static Material skyboxMaterial;
    private void SceneDirectorOnStart(SceneDirector.orig_Start orig, RoR2.SceneDirector self)
    {
        orig(self);
        
        if (SceneManager.GetActiveScene().name == "testscene")
        {
            Log.Debug($"new scene !! {RenderSettings.skybox} {SceneManager.GetActiveScene().name}");
                
            if (skyboxMaterial == null)
            {
                skyboxMaterial = Object.Instantiate(RenderSettings.skybox);
                skyboxMaterial.SetColor("_Tint", skyboxColor.Value);
            }
                
            RenderSettings.skybox = skyboxMaterial;
        }
    }

    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - testscene",
            "enable patches for testscene",
            true,
            "");
        BNRUtils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) =>
        {
            applyHooks();
        };
        
        skyboxColor = config.Bind("BNR - testscene", 
            "skyboxColor", 
            BNRUtils.Color255(152, 122, 144),
            "tint of skybox in testscene !!!");
        ModSettingsManager.AddOption(new ColorOption(skyboxColor));
    }
    
    private static ConfigEntry<Color> skyboxColor;
    private ConfigEntry<bool> enabled;
}