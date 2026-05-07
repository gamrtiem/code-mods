using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using RoR2;
using UnityEngine;

namespace BNR.patches;

public class commands
{
    #region debugcommands
    [ConCommand(commandName = "skin_create", flags = ConVarFlags.None, helpText = "list internal skins.,,.")]
    public static void CreateSkin(ConCommandArgs args)
    {
        if (skinrecolors.baseSkinName == null)
        {
            Log.Warning("base skin null !!");
            return;
        }

        if (args.Count >= 1)
        {
            skinrecolors.newSkinName = args[0];
        }
        
        string skinName = !skinrecolors.newSkinName.IsNullOrWhiteSpace() ? skinrecolors.newSkinName : "Generated Skin";
        skinrecolors.skinRecolors.Value += $";;{skinrecolors.baseSkinName.name},{skinrecolors.currentBody.name[..^7]},{skinrecolors.hsv[0]},{skinrecolors.hsv[1]},{skinrecolors.hsv[2]},{skinName}";
        Debug.Log($"added ;;{skinrecolors.baseSkinName.name},{skinrecolors.currentBody.name[..^7]},{skinrecolors.hsv[0]},{skinrecolors.hsv[1]},{skinrecolors.hsv[2]},{skinName} to the config !!! restart your game to see it in lobby .,,.");
    }
    
    [ConCommand(commandName = "skin_list", flags = ConVarFlags.None, helpText = "list internal skins.,,.")]
    public static void ListSkins(ConCommandArgs args)
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

    [ConCommand(commandName = "skin_clear", flags = ConVarFlags.None, helpText = "recolor current skins.,,.")]
    public static void clearSkin(ConCommandArgs args)
    {
        bodyNameToPrev.Clear();
    }
    
    private static Dictionary<string, int> bodyNameToPrev = [];
    [ConCommand(commandName = "skin_recolor", flags = ConVarFlags.None, helpText = "recolor current skins.,,.")]
    public static void recolorSkin(ConCommandArgs args)
    {
        ModelSkinController skinController = Utils.GetModelLocator(args.GetSenderBody().gameObject);

        int currentIndex = skinController.currentSkinIndex;
        if (bodyNameToPrev.TryGetValue(args.senderBody.name, out int value))
        {
            currentIndex = value;
        }
        else
        {
            bodyNameToPrev.Add(args.senderBody.name, skinController.currentSkinIndex);
        }
        SkinDef baseSkin = skinController.skins[currentIndex];

        float HSVsat = 0;
        float HSVvalue = 0;
        if (float.TryParse(args[0], out float HSVhue))
        {
            if (args.Count >= 2)
            {
                HSVsat = float.Parse(args[1]);
            }
        
            if (args.Count >= 3)
            {
                HSVvalue = float.Parse(args[2]);
            }
        }
        else
        {
            skinrecolors.newSkinName = args[0];
            
            if (args.Count >= 2)
            {
                HSVhue = float.Parse(args[1]);
            }
        
            if (args.Count >= 3)
            {
                HSVsat = float.Parse(args[2]);
            }
        
            if (args.Count >= 4)
            {
                HSVvalue = float.Parse(args[3]);
            }
        }
        
        
        SkinDef replacementSkin = skinrecolors.skinRecolor(baseSkin.name, args.senderBody.name, HSVhue, HSVsat, HSVvalue, "temp", "", true);

        Array.Resize(ref skinController.skins, skinController.skins.Length + 1);
        skinController.skins[^1] = replacementSkin;
        skinController.currentSkinIndex = skinController.skins.Length - 1;
        args.senderBody.skinIndex = (uint)(skinController.skins.Length - 1);
#pragma warning disable CS0618 // Type or member is obsolete
        skinController.ApplySkin(skinController.currentSkinIndex);
#pragma warning restore CS0618 // Type or member is obsolete
        
        skinrecolors.baseSkinName = baseSkin;
        skinrecolors.hsv[0] = HSVhue;
        skinrecolors.hsv[1] = HSVsat;
        skinrecolors.hsv[2] = HSVvalue;
        skinrecolors.currentBody = args.senderBody;
        
        //Log.Debug("bwaa");
    }
    #endregion
    
    private static List<uint> sounds = [];
    [ConCommand(commandName = "play_sound", flags = ConVarFlags.None, helpText = "https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Developer-Reference/Sound-%E2%80%90-Wwise-Events/")]
    public static void akplaysound(ConCommandArgs args)
    {
        Debug.Log("args = " + args[0]);
        Debug.Log($"sender body = {args.senderBody}");
        uint soundid = AkSoundEngine.PostEvent(args[0], ((Component)(object)args.senderBody).gameObject);
        Debug.Log($"sound id = {soundid}");
        sounds.Add(soundid);
    }

    [ConCommand(commandName = "stop_sound", flags = ConVarFlags.None, helpText = "stop looping sounds from playsound !!!!!")]
    public static void akstopsound(ConCommandArgs args)
    {
        Debug.Log($"sender body = {args.senderBody}");
        foreach (uint sound in sounds.ToList())
        {
            AkSoundEngine.StopPlayingID(sound);
            Debug.Log($"stopped sound {sound} !!!");
            sounds.Remove(sound);
        }
    }
    
    [ConCommand(commandName = "play_effect", flags = ConVarFlags.None, helpText = "play an effect !!!!!")]
    [ConCommand(commandName = "spawn_effect", flags = ConVarFlags.None, helpText = "play an effect !!!!!")]
    public static void spawnEffect(ConCommandArgs args)
    {
        float scale = 1;
        if (args.Count > 1)
        {
            scale = Convert.ToSingle(args[1]);
        }
        Debug.Log("args = " + args[0] + " " + scale);
        
        EffectDef effect = null;
        foreach (EffectDef effectDef in EffectCatalog.entries)
        {
            if (!effectDef.prefabName.Contains(args[0])) continue;
            
            effect = effectDef;
            break;
        }

        if (effect == null)
        {
            Log.Warning($"couldnt find effect {args[0]} !!!");
            return;
        }
        
        EffectManager.SpawnEffect(effect.index, new EffectData
        {
            origin = args.senderBody.transform.position,
            scale = scale,
            rotation = Util.QuaternionSafeLookRotation(Vector3.up)
        }, true);
    }
}