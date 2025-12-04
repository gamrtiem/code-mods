using BepInEx;
using BepInEx.Configuration;
using BNR.patches;
using HarmonyLib;
using On.RoR2.UI.MainMenu;
using UnityEngine;
using UnityEngine.UI;
using MainMenuController = RoR2.UI.MainMenu.MainMenuController;
using Path = System.IO.Path;
namespace BNR;

public class logoreplace : PatchBase<logoreplace>
{
    public override void Init(Harmony harmony)
    {
        Hook();
    }

    private void Hook()
    {
        if (enabled.Value)
        {
            BaseMainMenuScreen.OnEnter += BaseMainMenuScreenOnOnEnter;
        }
        else
        {
            BaseMainMenuScreen.OnEnter -= BaseMainMenuScreenOnOnEnter;
        }
    }
    
    
    private void BaseMainMenuScreenOnOnEnter(BaseMainMenuScreen.orig_OnEnter orig, RoR2.UI.MainMenu.BaseMainMenuScreen self, MainMenuController mainMenuController)
    {
        orig(self, mainMenuController);
        
        GameObject obj = GameObject.Find("LogoImage");
        if (obj == null) return;
        obj.transform.localPosition = new Vector3(0, 20, 0);
        obj.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        obj.GetComponent<Image>().sprite = ProdzModpackUtils.Utils.Load(Path.Combine(Paths.ConfigPath, "logo.png"));
        Log.Debug("Changed Logo Image");
    }

    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - replace logo",
            "enable patches for logoreplace",
            true,
            "replaces logo with a logo.png in the config folder (like realermodpackutils (rest in peace ,,.,.");
        BNRUtils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) => { Hook(); };
    }

    private ConfigEntry<bool> enabled;
}