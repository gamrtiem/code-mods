using BepInEx.Configuration;
using RoR2;
using RoR2.ConVar;
using UnityEngine;

namespace kinatoolkit.patches.basegame;

public class lowfpsinbg : PatchBase<lowfpsinbg>
{
    private static int oldFramerate;
    private static bool applyFramerate;
    
    public override void Init()
    {
        applyHooks();
        RoR2Application.onLoad += () =>
        {
            applyFramerate = true; 
            OnApplicationFocus(Application.isFocused);
        };
    }

    private void applyHooks()
    {
        if (enabled.Value)
        {
            On.RoR2.SettingsConVars.FpsMaxConVar.GetString += FpsMaxConVarOnGetString;
            Application.focusChanged += OnApplicationFocus;
        }
        else
        {
            On.RoR2.SettingsConVars.FpsMaxConVar.GetString -= FpsMaxConVarOnGetString;
            Application.focusChanged -= OnApplicationFocus;
        }

        OnApplicationFocus(Application.isFocused);
    }

    private string FpsMaxConVarOnGetString(On.RoR2.SettingsConVars.FpsMaxConVar.orig_GetString orig, BaseConVar self)
    {
        //lie ,.., 
        return oldFramerate != 0 ? oldFramerate.ToString() : orig((SettingsConVars.FpsMaxConVar)self);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        Log.Debug("focues ");   
        if (!applyFramerate) return;
        if (hasFocus && oldFramerate != 0)
        {
            Application.targetFrameRate = oldFramerate;
            oldFramerate = 0;
            Log.Debug($"back in focus ! set fps back to {Application.targetFrameRate} ,..,");
        }
        else if (!hasFocus)
        {
            oldFramerate = Application.targetFrameRate;
            Application.targetFrameRate = targetFPS.Value;
            Log.Debug($"out of focus ! previous fps {oldFramerate} ,..,");
        }
    }
    
    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("kinaToolkit - lowfpsinbg",
            "Enable low FPS in background.",
            true,
            "Lowers the FPS of the game while it is in the background.");
        Utils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) => { applyHooks(); };
        
        targetFPS = config.Bind("kinaToolkit - lowfpsinbg",
            "Target FPS while in background.",
            30,
            "What framerate to target while ROR2 is in the background. Recommend setting it to what the game allows (30, 60, 120, 144, etc) as some numbers can cause the game to not load.");
        Utils.SliderConfig(0, 144, targetFPS);
    }

    private ConfigEntry<bool> enabled;
    private ConfigEntry<int> targetFPS;
}