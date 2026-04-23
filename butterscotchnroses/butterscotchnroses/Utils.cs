using System;
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
    
    public static void TryCatchThrow(string message, Action action)
    {
        try
        {
            action?.Invoke();
        }
        catch (Exception e)
        {
            Log.Error($"{message} {e}");
        }
    }
    
    public static Texture2D makeReadable(Texture texture)
    {
        var tmp = RenderTexture.GetTemporary(texture.width, texture.height, 32);
        tmp.name = "Whatever";
        tmp.enableRandomWrite = true;
        tmp.Create();
            
        // Create a temporary RenderTexture of the same size as the texture
        // RenderTexture tmp = RenderTexture.GetTemporary(
        //     texture.width,
        //     texture.height,
        //     0,
        //     RenderTextureFormat.Default,
        //     RenderTextureReadWrite.Linear);

        // Blit the pixels on texture to the RenderTexture
        UnityEngine.Graphics.Blit(texture, tmp);
        // Backup the currently set RenderTexture
        RenderTexture previous = RenderTexture.active;
        // Set the current RenderTexture to the temporary one we created
        RenderTexture.active = tmp;
        // Create a new readable Texture2D to copy the pixels to it
        Texture2D myTexture2D = new Texture2D(texture.width, texture.height);
        // Copy the pixels from the RenderTexture to the new Texture
        myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
        myTexture2D.Apply();
        // Reset the active RenderTexture
        RenderTexture.active = previous;
        // Release the temporary RenderTexture
        RenderTexture.ReleaseTemporary(tmp);

        return myTexture2D;
    }

    public static Texture2D hueShiftTexture(Texture2D texture, float hueShift)
    {
        Texture2D readableTex = makeReadable(texture);
        Color[] texPixels = readableTex.GetPixels(0, 0, readableTex.width, readableTex.height);
				
        for (int i = 0; i < texPixels.Length; i++)
        {
            Color pixelColor = texPixels[i];
            Color.RGBToHSV(pixelColor, out float h, out float s, out float v);
            h = (h + hueShift/360f) % 1f;
            if (h < 0f) h += 1f;
            texPixels[i] = Color.HSVToRGB(h, s, v);
        }
            
        readableTex.SetPixels(texPixels);
        readableTex.Apply();
        readableTex.name = "newtex";
        return readableTex;
    }
}