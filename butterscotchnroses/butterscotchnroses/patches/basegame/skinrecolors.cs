using System;
using System.Collections.Generic;
using BNR.patches;
using BepInEx.Configuration;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine;

namespace BNR;

public class skinrecolors : PatchBase<skinrecolors>
{
    public override void Init()
    {
        applyHooks();
    }

    private void applyHooks()
    {
        if (enabled.Value)
        {
            RoR2.BodyCatalog.availability.onAvailable += RecolorSkins;
        }
        else
        {

        }
    }

    private void RecolorSkins()
    {
        string[] newSkins = skinRecolors.Value.Split(";;");
        foreach (string skin in newSkins)
        {
            string[] skinArgs = skin.Split(",");
            try
            {
                if (skinArgs.Length == 7)
                {
                    Utils.skinRecolor(skinArgs[0], skinArgs[1], float.Parse(skinArgs[2]), float.Parse(skinArgs[3]), float.Parse(skinArgs[4]), skinArgs[5], skinArgs[6]);
                }
                else
                {
                    Utils.skinRecolor(skinArgs[0], skinArgs[1], float.Parse(skinArgs[2]), float.Parse(skinArgs[3]), float.Parse(skinArgs[4]), skinArgs[5]);
                }
            }
            catch (Exception e)
            {
                Log.Warning(e);
            }
        }
    }
    
    [ConCommand(commandName = "list_skin", flags = ConVarFlags.None, helpText = "list internal skins.,,.")]
    public static void spawnEffect(ConCommandArgs args)
    {
        Debug.Log("args = " + args[0] + " " );
        var bodyPrefab = BodyCatalog.FindBodyPrefab(args[0]);
        if (!bodyPrefab)
        {
            Log.Warning("body no existey ,,.");
            return;
        }

        var modelLocator = bodyPrefab.GetComponent<ModelLocator>();
        if (!modelLocator)
        {
            Log.Warning("model locator no existey .,.,");
            return;
        }

        var mdl = modelLocator.modelTransform.gameObject;
        var skinController = mdl ? mdl.GetComponent<ModelSkinController>() : null;
        if (!skinController)
        {
            Log.Warning("model skin controller no existey .,,.");
            return;
        }
        
        foreach (SkinDef skinControllerSkinDef in skinController.skins)
        {
            Debug.Log(skinControllerSkinDef.name);
        }
    }

    [ConCommand(commandName = "clear_skin", flags = ConVarFlags.None, helpText = "recolor current skins.,,.")]
    public static void clearSkin(ConCommandArgs args)
    {
        bodyNameToPrev.Clear();
    }
    
    private static Dictionary<string, int> bodyNameToPrev = [];
    [ConCommand(commandName = "recolor_skin", flags = ConVarFlags.None, helpText = "recolor current skins.,,.")]
    public static void recolorSkin(ConCommandArgs args)
    {
        Debug.Log("args = " + args[0] + " ");

        var modelLocator = args.senderBody.GetComponent<ModelLocator>();
        if (!modelLocator)
        {
            Log.Warning("model locator no existey .,.,");
            return;
        }

        var mdl = modelLocator.modelTransform.gameObject;
        var skinController = mdl ? mdl.GetComponent<ModelSkinController>() : null;
        if (!skinController)
        {
            Log.Warning("model skin controller no existey .,,.");
            return;
        }

        int currentIndex = skinController.currentSkinIndex;
        if (bodyNameToPrev.TryGetValue(args.senderBody.name, out int value))
        {
            currentIndex = value;
        }
        else
        {
            bodyNameToPrev.Add(args.senderBody.name, skinController.currentSkinIndex);
        }
        SkinDef currentSkin = skinController.skins[currentIndex];
        SkinDef replacementSkin = Utils.skinRecolor(currentSkin.name, args.senderBody.name, float.Parse(args[0]), float.Parse(args[1]), float.Parse(args[2]), "temp", "", false);

        Array.Resize(ref skinController.skins, skinController.skins.Length + 1);
        skinController.skins[^1] = replacementSkin;
        skinController.currentSkinIndex = skinController.skins.Length - 1;
        args.senderBody.skinIndex = (uint)(skinController.skins.Length - 1);
        skinController.ApplySkin(skinController.currentSkinIndex);
        
        Log.Debug("bwaa");
    }

    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - skinrecolors",
            "enable patches for skinrecolors",
            true,
            "");
        Utils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) => { applyHooks(); };
        
        skinRecolors = config.Bind("BNR - skinrecolors",
            "skin recolors",
            "skinCommandoAlt,CommandoBody,100,0,0,Test Skin;;skinCommandoAlt,CommandoBody,200,0,0,Test Skin 2;;skinCommandoDefault,CommandoBody,290,-40,-10,Awesome Skin ,.,.",
            "follows \"string baseSkinDefName, string bodyName, float hue, float saturation, float value, string skinName, string prefix\" where prefix is optional (used for like ,., Red on wolfo qol merc.,., use list_skins to get internal names or prodz debugging mod ,., split with ;; ..,,. you can temporarily try out recolors with recolor_skin hue saturation value ,.,,.");
    }

    private ConfigEntry<string> skinRecolors;
    private ConfigEntry<bool> enabled;
}