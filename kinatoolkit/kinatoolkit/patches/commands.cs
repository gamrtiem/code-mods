using System.Linq;
using System.Text;
using kinatoolkit.patches.basegame;
using RoR2;
using UnityEngine;
using DebugToolkit;

namespace kinatoolkit.patches;

public static class commands
{
    public static bool disableInteractables;
    
    [ConCommand(commandName = "disable_interactables", flags = ConVarFlags.None)]
    public static void CreateSkin(ConCommandArgs args)
    {
        bool? interactableBool = args.TryGetArgBool(0);
        if (interactableBool != null)
        {
            disableInteractables = interactableBool.Value;
        }
        else
        {
            disableInteractables = !disableInteractables;
        }
        string color = disableInteractables ? "green" : "red";
        Debug.Log($"Disabled interactables <color={color}>{disableInteractables}</color>.");
    }

    public static void Init()
    {
        On.RoR2.InteractableSpawnCard.Spawn += InteractableSpawnCardOnSpawn;
        RoR2Application.onLoad += OnLoad;
    }

    private static void OnLoad()
    {
        AutoCompleteParser parser = new AutoCompleteParser();
        
        parser.RegisterStaticVariable("ai", MasterCatalog.allAiMasters.Select(i => $"{(int)i.masterIndex}|{i.name}|{StringFinder.GetLangInvar(StringFinder.GetMasterName(i))}"), 1);
        parser.RegisterStaticVariable("soundID", soundIDs.soundID.Keys, 1);
        parser.RegisterStaticVariable("effect", EffectCatalog.entries.Select(i => i.prefabName), 1);
        parser.Scan(System.Reflection.Assembly.GetExecutingAssembly());
    }

    private static void InteractableSpawnCardOnSpawn(On.RoR2.InteractableSpawnCard.orig_Spawn orig, RoR2.InteractableSpawnCard self, Vector3 position, Quaternion rotation, DirectorSpawnRequest directorspawnrequest, ref SpawnCard.SpawnResult result)
    {
        if (disableInteractables)
        {
            return;
        }
        
        orig(self, position, rotation, directorspawnrequest, ref result);
    }
    
    [ConCommand(commandName = "play_sound", flags = ConVarFlags.None)]
    [AutoComplete("Requires 1 argument: {soundID}")]
    public static void akplaysound(ConCommandArgs args)
    {
        int? soundIDint = args.TryGetArgInt(0);
        if (soundIDint != null)
        {
            uint id = AkSoundEngine.PostEvent((uint)soundIDint, args.senderBody.gameObject);
            if (id != 0)
            {
                Debug.Log($"Started playing sound with id {id}. Use \"stop_sound {id}\" to kill it if it loops forever.");
            }
            else
            {
                Debug.LogWarning($"Couldnt find sound with id {soundIDint}.");
            }
        }
        
        string soundID = args.TryGetArgString(0);
        if (soundID != "")
        {
            if(soundIDs.soundID.TryGetValue(soundID, out uint soundIDNum))
            {
                uint id = AkSoundEngine.PostEvent(soundIDNum, args.senderBody.gameObject);
                if (id != 0)
                {
                    Debug.Log($"Started playing sound with id {id}. Use \"stop_sound {id}\" to kill it if it loops forever.");
                }
                else
                {
                    Debug.LogWarning($"Couldnt find sound with id {soundID}.");
                }
            }
            else
            {
                Debug.LogWarning($"Couldn't find sound with id {soundID}.");
            }
        }
        else
        {
            Debug.LogWarning("No sound ID provided.");
        }
    }

    [ConCommand(commandName = "stop_sound", flags = ConVarFlags.None)]
    public static void akstopsound(ConCommandArgs args)
    {
        int? soundID = args.TryGetArgInt(0);
        if (soundID != null)
        {
            AkSoundEngine.StopPlayingID((uint)soundID);
            Debug.Log($"Stopped sound {soundID}.");
        }
        else
        {
            Debug.Log("Bo sound id provided.");
        }
    }
    
    [ConCommand(commandName = "list_effectdef", flags = ConVarFlags.None)]
    public static void listEffect(ConCommandArgs args)
    {
        StringBuilder effectLog = new StringBuilder();
        
        foreach (EffectDef effect in EffectCatalog.entries)
        {
            effectLog.Append(effect.prefabName);
            effectLog.Append("\n");
        }
        
        Log.Info(effectLog);
    }
    
    [ConCommand(commandName = "spawn_effectdef", flags = ConVarFlags.None)]
    [AutoComplete("Requires 1 argument: {effect}")]
    public static void spawnEffect(ConCommandArgs args)
    {
        float scale = 1;
        if (args.TryGetArgFloat(1) != null)
        {
            scale = args.GetArgFloat(1);
        }
        
        string effectName = args.TryGetArgString(0);
        if (effectName == "") return;
        
        EffectDef effect = EffectCatalog.entries.FirstOrDefault(effectDef => effectDef.prefabName == effectName);
        if (effect == null)
        {
            Debug.LogWarning($"Couldnt find effect {effectName}.");
            return;
        }
        
        EffectManager.SpawnEffect(effect.index, new EffectData
        {
            origin = args.senderBody.footPosition,
            scale = scale,
            rotation = RoR2.Util.QuaternionSafeLookRotation(Vector3.up)
        }, true);
        Debug.Log($"Spawned effect {effectName}.");
    }
    
    [ConCommand(commandName = "spawn_dummy", flags = ConVarFlags.None, helpText = "Spawns a character master with no AI and 9999999 boost healths.")]
    [AutoComplete("Requires 1 argument: {ai}")]
    public static void SpawnDummy(ConCommandArgs args)
    {
        string dummyMasterName = args.TryGetArgString(0);
        if (dummyMasterName == "") return;
        debugplains.SpawnDummy(dummyMasterName, args.senderBody.corePosition, Quaternion.identity);
    }
}