using System.Collections.Generic;
using System.Linq;
using System.Text;
using kinatoolkit.patches.basegame;
using RoR2;
using UnityEngine;
using DebugToolkit;
using R2API;
using RoR2.Artifacts;
using RoR2.CharacterAI;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace kinatoolkit.patches;

public static class commands
{
    public static bool disableInteractables;
    public static TeamIndex dummyTeamIndex;
    
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
        
        TeamDef dummyTeamDef = new TeamDef
        {
            nameToken = "dummyTeam",
            softCharacterLimit = 999,
            levelUpEffect = TeamCatalog.GetTeamDef(TeamIndex.Monster)?.levelUpEffect,
            friendlyFireScaling = TeamCatalog.GetTeamDef(TeamIndex.Monster)!.friendlyFireScaling,
            levelUpSound = TeamCatalog.GetTeamDef(TeamIndex.Monster)!.levelUpSound
        };
        dummyTeamIndex = TeamsAPI.RegisterTeam(dummyTeamDef, new TeamsAPI.TeamBehavior("kinaToolkitDummy", TeamsAPI.TeamClassification.Enemy));
    }

    private static void OnLoad()
    {
        AutoCompleteParser parser = new AutoCompleteParser();
        
        parser.RegisterStaticVariable("ai", MasterCatalog.allAiMasters.Select(i => $"{(int)i.masterIndex}|{i.name}|{StringFinder.GetLangInvar(StringFinder.GetMasterName(i))}"), 1);
        parser.RegisterStaticVariable("soundID", soundIDs.soundID.Keys, 1);
        parser.RegisterStaticVariable("effect", EffectCatalog.entries.Select(i => i.prefabName), 1);
        parser.Scan(System.Reflection.Assembly.GetExecutingAssembly());
    }

    private static void InteractableSpawnCardOnSpawn(On.RoR2.InteractableSpawnCard.orig_Spawn orig, InteractableSpawnCard self, Vector3 position, Quaternion rotation, DirectorSpawnRequest directorspawnrequest, ref SpawnCard.SpawnResult result)
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
        foreach (string sound in args.userArgs)
        {
            bool parsed = int.TryParse(sound, out int soundIDint);
            if (parsed)
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
            else
            {
                if (sound != "")
                {
                    if(soundIDs.soundID.TryGetValue(sound, out uint soundIDNum))
                    {
                        uint id = AkSoundEngine.PostEvent(soundIDNum, args.senderBody.gameObject);
                        if (id != 0)
                        {
                            Debug.Log($"Started playing sound with id {id}. Use \"stop_sound {id}\" to kill it if it loops forever.");
                        }
                        else
                        {
                            Debug.LogWarning($"Couldnt find sound with id {sound}.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Couldn't find sound with id {sound}.");
                    }
                }
                else
                {
                    Debug.LogWarning("No sound ID provided.");
                }
            }
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
            Debug.Log("No sound id provided.");
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
        SpawnDummy(dummyMasterName, args.senderBody.corePosition, Quaternion.identity, TeamIndex.Monster);
    }
    
    public static CharacterMaster SpawnDummy(string masterName, Vector3 position, Quaternion rotation = default, TeamIndex teamIndex = TeamIndex.None)
    {
        GameObject masterPrefab = MasterCatalog.FindMasterPrefab(masterName);
        if (masterPrefab)
        {
            GameObject instantiatedMaster = Object.Instantiate(masterPrefab);
            CharacterMaster master = instantiatedMaster.GetComponent<CharacterMaster>();
            master.inventory.GiveItemPermanent(RoR2Content.Items.BoostHp, 9999999);
            master.teamIndex = teamIndex != TeamIndex.None ? teamIndex : dummyTeamIndex;
            NetworkServer.Spawn(instantiatedMaster);
            master.SpawnBody(position, rotation);
            foreach (BaseAI ai in master.aiComponents)
            {
                Object.Destroy(ai);
            }
            master.aiComponents = [];
            return master;
        }
        else
        {
            Log.Warning($"Couldn't find master prefab of {masterName}.");
            return null;
        }
    }
    
    [ConCommand(commandName = "reload_json", flags = ConVarFlags.None, helpText = "Loads the current json for debug plains.")]
    public static void LoadJson(ConCommandArgs args)
    {
        foreach (GameObject interactable in JSONObjects.Where(interactable => interactable))
        {
            Object.Destroy(interactable);
        }
        foreach (CharacterMaster master in JSONMasters.Where(master => master))
        {
            master.TrueKill();
        }

        LoadJson();
    }

    public static List<GameObject> JSONObjects = [];
    public static List<CharacterMaster> JSONMasters = [];

    public static void ChangeSpawnPos(CharacterBody body)
    {
        debugplainsJSON.JSONedit jsonEdits = debugplainsJSON.loadJSON();
        TeleportHelper.TeleportBody(body, new Vector3(jsonEdits.spawnPos.position.x, jsonEdits.spawnPos.position.y, jsonEdits.spawnPos.position.z), true);
        body.gameObject.GetComponent<CharacterMotor>().Motor.SetRotation(Quaternion.Euler(jsonEdits.spawnPos.rotation.x, jsonEdits.spawnPos.rotation.y, jsonEdits.spawnPos.rotation.z));
        PlayerCharacterMasterController._instances[0].master.onBodyStart -= ChangeSpawnPos;
    }

    public static void LoadJson()
    {
        debugplainsJSON.JSONedit jsonEdits = debugplainsJSON.loadJSON();

        PlayerCharacterMasterController._instances[0].master.onBodyStart += ChangeSpawnPos;
        
            
        foreach (debugplainsJSON.Dummy dummy in jsonEdits.dummies)
        {
            JSONMasters.Add(SpawnDummy(dummy.masterName, new Vector3(dummy.position.x, dummy.position.y, dummy.position.z), Quaternion.Euler(dummy.rotation.x, dummy.rotation.y, dummy.rotation.z)));
        }
            
        foreach (debugplainsJSON.Interactables interactable in jsonEdits.interactables)
        {
            InteractableSpawnCard spawnCard = Addressables.LoadAssetAsync<InteractableSpawnCard>(interactable.interactableCard).WaitForCompletion();
            if (!spawnCard)
            {
                Log.Warning($"Couldn't load interactable card {interactable.interactableCard}.");
                break;
            }
            SpawnCard.SpawnResult spawned = spawnCard.DoSpawn(new Vector3(interactable.position.x, interactable.position.y, interactable.position.z), Quaternion.Euler(interactable.rotation.x, interactable.rotation.y, interactable.rotation.z), new DirectorSpawnRequest(spawnCard, null, RoR2Application.rng));
            spawned.spawnedInstance.transform.rotation = Quaternion.Euler(interactable.rotation.x, interactable.rotation.y, interactable.rotation.z);
            JSONObjects.Add(spawned.spawnedInstance);
        }
        
        bool prevCommandEnabled = RunArtifactManager.instance._enabledArtifacts[(int)CommandArtifactManager.myArtifact.artifactIndex];
        RunArtifactManager.instance._enabledArtifacts[(int)CommandArtifactManager.myArtifact.artifactIndex] = true;
        debugplains.enableAllFoodItems = true;
        foreach (debugplainsJSON.commandPickup commandPickup in jsonEdits.commandPickups)
        {
            ItemTier? searchTier = ItemTierCatalog.itemTierDefs.FirstOrDefault(def => def.name.Replace("Def", "") == commandPickup.tier)?.tier;
            PickupIndex? pickupIndex;
            if (searchTier == null)
            {
                if (commandPickup.tier == "LunarTierEquip")
                {
                    pickupIndex = PickupCatalog.FindPickupIndex(EquipmentCatalog.equipmentDefs.FirstOrDefault(def => def.name == "Tonic")!.equipmentIndex);
                }
                else if (commandPickup.tier == "BossTier")
                {
                    pickupIndex = PickupCatalog.FindPickupIndex(RoR2Content.Items.Knurl.itemIndex);
                }
                else
                {
                    Log.Warning($"Couldn't find tier {commandPickup.tier} in ItemTierCatalog.");
                    continue;
                }
            }
            else
            {
                pickupIndex = PickupCatalog.FindPickupIndex(ItemCatalog.itemDefs.First(item => item.tier == searchTier).itemIndex);
            }

            PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo
            {
                pickup = new UniquePickup
                {
                    pickupIndex = (PickupIndex)pickupIndex,
                    decayValue = 0f,
                },
            }, new Vector3(commandPickup.position.x, commandPickup.position.y, commandPickup.position.z), new Vector3(0, 0, 0));
        }

        Run.onRunDestroyGlobal += _ => { debugplains.enableAllFoodItems = false; };
        
        RunArtifactManager.instance._enabledArtifacts[(int)CommandArtifactManager.myArtifact.artifactIndex] = prevCommandEnabled;
    }
}