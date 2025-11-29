using BepInEx.Configuration;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using UnityEngine;

namespace BNR;

public class BNRUtils
{
    public static Color Color255(int r, int g, int b, int a)
    {
        return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
    }
    
    public static Color Color255(float r, float g, float b, float a)
    {
        return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
    }
    
    public static Color Color255(int r, int g, int b)
    {
        return new Color(r / 255f, g / 255f, b / 255f, 1);
    }

    public static void SliderConfig(float min, float max, ConfigEntry<float> config)
    {
        StepSliderConfig stepSliderConfig = new()
        {
            max = max,
            min = min,
            FormatString = "{0:0}"
        };
        StepSliderOption stepSliderOption = new(config, stepSliderConfig);
        ModSettingsManager.AddOption(stepSliderOption);
    }
    
    public static void SliderConfig(int min, int max, ConfigEntry<int> config)
    {
        IntSliderConfig intSliderConfig = new()
        {
            max = max,
            min = min,
            formatString = "{0:0}"
        };
        IntSliderOption intSliderOption = new(config, intSliderConfig);
        ModSettingsManager.AddOption(intSliderOption);
    }
    
    public static void CheckboxConfig(ConfigEntry<bool> config)
    {
        CheckBoxConfig checkBoxConfig = new();
        CheckBoxOption checkBoxOption = new(config, checkBoxConfig);
        ModSettingsManager.AddOption(checkBoxOption);
    }

    public static void StringConfig(ConfigEntry<string> config)
    {
        InputFieldConfig inputFieldConfig = new();
        StringInputFieldOption stringInputFieldOption = new(config, inputFieldConfig);
        ModSettingsManager.AddOption(stringInputFieldOption);
    }
}