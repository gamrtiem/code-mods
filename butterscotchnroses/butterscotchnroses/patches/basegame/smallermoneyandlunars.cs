using static BNR.butterscotchnroses;
using BNR.patches;
using BepInEx.Configuration;
using HarmonyLib;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BNR;

public class smallermoneyandlunars : PatchBase<smallermoneyandlunars>
{
    public override void Init(Harmony harmony)
    {
        if (!enabled.Value)
        {
            return;
        }
        
        HUD.onHudTargetChangedGlobal += HUDOnonHudTargetChangedGlobal;
    }

    private static void HUDOnonHudTargetChangedGlobal(HUD obj)
    {
        GameObject upperLeft = GameObject.Find("UpperLeftCluster");
        if (!upperLeft) return;

        upperLeft.GetComponent<RectTransform>().offsetMin = new Vector2(0, -64);
        upperLeft.GetComponent<VerticalLayoutGroup>().spacing = 0;
                
        GameObject dollar = GameObject.Find("DollarSign");
        if (dollar)
        {
            dollar.transform.localPosition = new Vector3(4, dollar.transform.localPosition.y, dollar.transform.localPosition.z);
        }
                
        GameObject buildLabel = GameObject.Find("SteamBuildLabel");
        if (buildLabel)
        {
            buildLabel.gameObject.SetActive(false);
        }
    }

    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - UI",
            "make currency thing smaller", 
            true, 
            "make the thing that has coins and lunar coins in the top left smaller like pre sotv (i miss you ,.,."); 
        BNRUtils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) =>
        {
            if (enabled.Value)
            {
                HUD.onHudTargetChangedGlobal += HUDOnonHudTargetChangedGlobal;
            }
            else
            {
                HUD.onHudTargetChangedGlobal -= HUDOnonHudTargetChangedGlobal;
            }
        };
    }

    private ConfigEntry<bool> enabled;
}