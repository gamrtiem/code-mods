using System;
using System.Collections.Generic;
using System.Linq;
using RoR2;
using UnityEngine;

namespace BNR.patches;

public class commands
{
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