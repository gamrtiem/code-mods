using BNR.patches;
using BepInEx.Configuration;
using HarmonyLib;
using On.RoR2.UI;
using RiskOfOptions;
using RiskOfOptions.Options;
using UnityEngine;

namespace BNR;

public class pingrecolor : PatchBase<pingrecolor>
{
    public override void Init(Harmony harmony)
    {
        applyHooks();
    }

    private void applyHooks()
    {
        if (enabled.Value)
        {
            PingIndicator.RebuildPing += PingIndicatorOnRebuildPing;
        }
        else
        {
            PingIndicator.RebuildPing -= PingIndicatorOnRebuildPing;
        }
    }

    private void PingIndicatorOnRebuildPing(PingIndicator.orig_RebuildPing orig, RoR2.UI.PingIndicator self)
    {
        orig(self);

        self.pingColor = self.pingType switch
        {
            RoR2.UI.PingIndicator.PingType.Default => pingIndicatorDefault.Value,
            RoR2.UI.PingIndicator.PingType.Enemy => pingIndicatorEnemy.Value,
            RoR2.UI.PingIndicator.PingType.Interactable => pingIndicatorInteractable.Value,
            RoR2.UI.PingIndicator.PingType.Count => pingIndicatorCount.Value,
            _ => self.pingColor
        };
    }

    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - pingrecolor",
            "enable patches for pingrecolor",
            true,
            "");
        BNRUtils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) =>
        {
            applyHooks();
        };
        
        pingIndicatorDefault = config.Bind("BNR - pingrecolor",
            "ping recolor for default ping !!",
            BNRUtils.Color255(252, 142, 249),
            "");
        ModSettingsManager.AddOption(new ColorOption(pingIndicatorDefault));
        
        pingIndicatorEnemy = config.Bind("BNR - pingrecolor",
            "ping recolor for enemy ping !!",
            BNRUtils.Color255(252, 142, 249),
            "");
        ModSettingsManager.AddOption(new ColorOption(pingIndicatorEnemy));
        
        pingIndicatorInteractable = config.Bind("BNR - pingrecolor",
            "ping recolor for interactable ping !!",
            BNRUtils.Color255(252, 142, 249),
            "");
        ModSettingsManager.AddOption(new ColorOption(pingIndicatorInteractable));
        
        pingIndicatorCount = config.Bind("BNR - pingrecolor",
            "ping recolor for count ping (unsure what this one actually is !! !!",
            BNRUtils.Color255(252, 142, 249),
            "");
        ModSettingsManager.AddOption(new ColorOption(pingIndicatorCount));
    }

    private ConfigEntry<Color> pingIndicatorDefault;
    private ConfigEntry<Color> pingIndicatorEnemy;
    private ConfigEntry<Color> pingIndicatorInteractable;
    private ConfigEntry<Color> pingIndicatorCount;
    private ConfigEntry<bool> enabled;
}