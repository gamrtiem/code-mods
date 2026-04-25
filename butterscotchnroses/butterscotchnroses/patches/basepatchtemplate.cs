/*using BNR.patches;
using BepInEx.Configuration;

namespace BNR;

public class $CLASS$ : PatchBase<$CLASS$>
{
    public override void Init()
    {
        applyHooks();
    }

    private void applyHooks()
    {
        if (enabled.Value)
        {
            
        }
        else
        {
            
        }
    }

    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - $CLASS$",
            "enable patches for $CLASS$",
            true,
            "");
        Utils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) =>
        {
            applyHooks();
        };
    }
    
    private ConfigEntry<bool> enabled;
}*/