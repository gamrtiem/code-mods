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
            On.RoR2.UI.PingIndicator.OnEnable += PingIndicatorOnOnEnable;
            PingIndicator.RebuildPing += PingIndicatorOnRebuildPing;
        }
        else
        {
            PingIndicator.OnEnable -= PingIndicatorOnOnEnable;
            PingIndicator.RebuildPing -= PingIndicatorOnRebuildPing;
        }
    }

    private void PingIndicatorOnOnEnable(PingIndicator.orig_OnEnable orig, RoR2.UI.PingIndicator self)
    {
        recolorPingIndicator(self);
        orig(self);
    }

    private void PingIndicatorOnRebuildPing(PingIndicator.orig_RebuildPing orig, RoR2.UI.PingIndicator self)
    {
        recolorPingIndicator(self);
        orig(self);
    }

    private void recolorPingIndicator(RoR2.UI.PingIndicator pingIndicator)
    {
        Log.Debug(pingIndicator.pingColor + "bwaa ");
        pingIndicator.defaultPingColor = pingIndicatorDefault.Value;
        pingIndicator.enemyPingColor = pingIndicatorEnemy.Value;
        pingIndicator.interactablePingColor = pingIndicatorInteractable.Value;

        pingIndicator.pingColor = pingIndicator.pingType switch
        {
            RoR2.UI.PingIndicator.PingType.Default => pingIndicatorDefault.Value,
            RoR2.UI.PingIndicator.PingType.Enemy => pingIndicatorEnemy.Value,
            RoR2.UI.PingIndicator.PingType.Interactable => pingIndicatorInteractable.Value,
            RoR2.UI.PingIndicator.PingType.Count => pingIndicatorCount.Value,
            _ => pingIndicator.pingColor
        };
        pingIndicator.transform.GetChild(0).GetComponent<ParticleSystem>().startColor = pingIndicatorDefault.Value;
        pingIndicator.transform.GetChild(1).GetComponent<ParticleSystem>().startColor = pingIndicatorInteractable.Value;
        pingIndicator.transform.GetChild(2).GetComponent<ParticleSystem>().startColor = pingIndicatorEnemy.Value;
        pingIndicator.positionIndicator.alwaysVisibleObject.transform.GetChild(0).GetComponent<SpriteRenderer>().color = pingIndicatorDefault.Value;
        pingIndicator.positionIndicator.alwaysVisibleObject.transform.GetChild(1).GetComponent<SpriteRenderer>().color = pingIndicatorEnemy.Value;
        pingIndicator.positionIndicator.alwaysVisibleObject.transform.GetChild(2).GetComponent<SpriteRenderer>().color = pingIndicatorInteractable.Value;
        Log.Debug(pingIndicator.positionIndicator.alwaysVisibleObject.transform.GetChild(0).GetComponent<SpriteRenderer>().color + " bwaa2 ");
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