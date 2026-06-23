using BepInEx.Configuration;
using RoR2;
using System.IO;
using System.Linq;
using BepInEx;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using RoR2.CharacterAI;
using UnityEngine.Networking;
using Console = RoR2.Console;
using Object = UnityEngine.Object;

namespace kinatoolkit.patches.basegame;

public class debugplains : PatchBase<debugplains>
{
    private static bool singleplayerPressed;
    private static bool lobbyPressed;
    private static bool enteredScene;
    private static int changedSpawnTransform;
    private static bool oldDisableInteractables;
    private static bool runStartCommands;
    private static bool stageStartCommands;
    
    public override void Init()
    {
        applyHooks();
    }

    private void applyHooks()
    {
        if (enabled.Value)
        {
            On.RoR2.UI.MainMenu.BaseMainMenuScreen.OnEnter += BaseMainMenuScreenOnOnEnter;
            On.RoR2.UI.CharacterSelectController.Awake += CharacterSelectControllerOnAwake;
            On.RoR2.Run.OnEnable += RunOnOnEnable;
            On.RoR2.Stage.GetPlayerSpawnTransform += StageOnGetPlayerSpawnTransform;
            Run.onRunStartGlobal += OnRunStart;
            On.RoR2.Stage.Start += StageOnStart;
        }
        else
        {
            On.RoR2.UI.MainMenu.BaseMainMenuScreen.OnEnter -= BaseMainMenuScreenOnOnEnter;
            On.RoR2.UI.CharacterSelectController.Awake -= CharacterSelectControllerOnAwake;
            On.RoR2.Run.OnEnable -= RunOnOnEnable;
            On.RoR2.Stage.GetPlayerSpawnTransform -= StageOnGetPlayerSpawnTransform;
            Run.onRunStartGlobal -= OnRunStart;
            On.RoR2.Stage.Start -= StageOnStart;
        }
    }
    
    private static void CharacterSelectControllerOnAwake(On.RoR2.UI.CharacterSelectController.orig_Awake orig, RoR2.UI.CharacterSelectController self)
    {
        orig(self);
        if (lobbyPressed || !skipLobby.Value) return;
        
        lobbyPressed = true;
        Log.Debug("lobbyPressed");
                
        PreGameController.instance.gameObject.GetComponent<VoteController>().ReceiveUserVote(LocalUserManager.GetFirstLocalUser().currentNetworkUser, 0);
    }

    private static void BaseMainMenuScreenOnOnEnter(On.RoR2.UI.MainMenu.BaseMainMenuScreen.orig_OnEnter orig, RoR2.UI.MainMenu.BaseMainMenuScreen self, RoR2.UI.MainMenu.MainMenuController mainmenucontroller)
    {
        orig(self, mainmenucontroller);

        if (singleplayerPressed || !skipTitle.Value) return;
        
        singleplayerPressed = true;
        Log.Debug("singleplayerPressed");
                
        RoR2.UI.MainMenu.TitleMenuController titlemenuController = self.gameObject.GetComponent<RoR2.UI.MainMenu.TitleMenuController>();
        titlemenuController.consoleFunctions.SubmitCmd("transition_command \"gamemode ClassicRun; host 0;\"");
    }
    
    private static Transform StageOnGetPlayerSpawnTransform(On.RoR2.Stage.orig_GetPlayerSpawnTransform orig, RoR2.Stage self)
    {
        Transform spawnPoint = orig(self);
        
        //this is run twice ? .,., for only once ? ,.., curious ., ,.
        if (!enteredScene || changedSpawnTransform >= 2) return spawnPoint;
        
        changedSpawnTransform++;
            
        Log.Debug($"changing spawn pos !!");
        string[] cordsList = cordinates.Value.Split(",");
        int indexOf = cordsList.ToList().IndexOf(SceneManager.GetActiveScene().name);
        if (indexOf != -1 && cordsList.Length > indexOf + 3)
        {
            spawnPoint.position = new Vector3(float.Parse(cordsList[indexOf + 1].Trim()), float.Parse(cordsList[indexOf + 2].Trim()), float.Parse(cordsList[indexOf + 3].Trim()));
        }
        else
        {
            Log.Warning($"invalid formatting for cords config !! {cordinates.Value}");
        }

        if (changedSpawnTransform == 2)
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Paths.ConfigPath)!, "config", "kinaToolkit");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            
            string jsonPath = System.IO.Path.Combine(dir, "debugPlains.json");

            string[] addressables = ["RoR2/Base/ShrineChance/iscShrineChance.asset"];
            foreach (string addressable in addressables)
            {
                InteractableSpawnCard spawnCard = Addressables.LoadAssetAsync<InteractableSpawnCard>(addressable).WaitForCompletion();
                SpawnCard.SpawnResult spawned = spawnCard.DoSpawn(new Vector3(334.3808f, -52.90586f, -177.3572f), new Quaternion(0f, 180f, 0f, 0f), new DirectorSpawnRequest(spawnCard, null, RoR2Application.rng));
                Log.Debug($"spawned interactable ! {spawned.success}");
            }

            string[] dummies = ["GolemMaster"];
            foreach (string dummyMasterName in dummies)
            {
                SpawnDummy(dummyMasterName, new Vector3(334.3808f, -47.90586f, -177.3572f), Quaternion.identity);
            }

        }
            
        if (disableInteractables.Value)
        {
            commands.disableInteractables = oldDisableInteractables;
        }

        return spawnPoint;
    }

    private static void RunOnOnEnable(On.RoR2.Run.orig_OnEnable orig, RoR2.Run self)
    {
        SceneDef sceneDef = SceneCatalog.GetSceneDefFromSceneName(sceneEntry.Value);
        
        if (!enteredScene && sceneDef != null)
        {
            enteredScene = true;
            
            SceneCollection.SceneEntry sceneCollectionEntry = new SceneCollection.SceneEntry { sceneDef = sceneDef };
            SceneCollection sceneCollection = ScriptableObject.CreateInstance<SceneCollection>();
            sceneCollection._sceneEntries = [sceneCollectionEntry];
        
            self.startingSceneGroup = sceneCollection;

            if (disableInteractables.Value)
            {
                oldDisableInteractables = commands.disableInteractables;
                commands.disableInteractables = true;
            }
        }
        
        orig(self);
    }
    
    private static IEnumerator StageOnStart(On.RoR2.Stage.orig_Start orig, RoR2.Stage self)
    {
        if (!stageStartCommands)
        {
            stageStartCommands = true;
            
            foreach (string runCommand in stageCommands.Value.Split(";"))
            {
                string trimCommand = runCommand.Trim();
                List<string> commandArgs = trimCommand.Split(" ").ToList();
                string command = commandArgs[0];
                commandArgs.RemoveAt(0);

                string commandargs = commandArgs.Aggregate("", (current, arg) => current + (arg + " "));
                Log.Debug($"Running command: {command} with args {commandargs}");
            
                Console.instance.RunCmd(LocalUserManager.GetFirstLocalUser(), command, commandArgs);
            }
        }
        
        yield return orig(self);
    }

    private static void OnRunStart(Run run)
    {
        if (runStartCommands) return;
        
        runStartCommands = true;
            
        foreach (string runCommand in runCommands.Value.Split(";"))
        {
            string trimCommand = runCommand.Trim();
            List<string> commandArgs = trimCommand.Split(" ").ToList();
            string command = commandArgs[0];
            commandArgs.RemoveAt(0);

            string commandargs = commandArgs.Aggregate("", (current, arg) => current + (arg + " "));
            Log.Debug($"Running command: {command} with args {commandargs}");
            
            Console.instance.RunCmd(LocalUserManager.GetFirstLocalUser(), command, commandArgs);
        }
    }

    public static void SpawnDummy(string masterName, Vector3 position, Quaternion rotation = default)
    {
        GameObject masterPrefab = MasterCatalog.FindMasterPrefab(masterName);
        if (masterPrefab)
        {
            GameObject instantiatedMaster = Object.Instantiate(masterPrefab);
            CharacterMaster master = instantiatedMaster.GetComponent<CharacterMaster>();
            master.inventory.GiveItemPermanent(RoR2Content.Items.BoostHp, 9999999);
            NetworkServer.Spawn(instantiatedMaster);
            master.SpawnBody(position, rotation);
            foreach (BaseAI ai in master.aiComponents)
            {
                Object.Destroy(ai);
            }
            master.aiComponents = [];
        }
        else
        {
            Log.Warning($"Couldn't find master prefab of {masterName}.");
        }
    }
    
    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("kinaToolkit - debugplains",
            "Enable DebugPlains",
            true,
            "");
        Utils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) => { applyHooks(); };
        
        skipTitle = config.Bind("kinaToolkit - debugplains", 
            "Skip title screen", 
            true,
            "Whether or not to skip the title screen.");
        Utils.CheckboxConfig(skipTitle);
        
        skipLobby = config.Bind("kinaToolkit - debugplains", 
            "Skip character select", 
            true,
            "Whether or not to skip the character select screen.");
        Utils.CheckboxConfig(skipLobby);
        
        sceneEntry = config.Bind("kinaToolkit - debugplains", 
            "Starting scene", 
            "golemplains",
            "Default scene to send the player to upon starting a run for the first time. Set to blank or an invalid scene name to disable.");
        Utils.StringConfig(sceneEntry);
        
        disableInteractables = config.Bind("kinaToolkit - debugplains", 
            "Disable naturally spawning interactables in Debug Plains", 
            true,
            "Run disable_interactables as the scene loads to prevent any interactables from spawning in.");
        Utils.CheckboxConfig(disableInteractables);
        
        cordinates = config.Bind("kinaToolkit - debugplains", 
            "Spawn cordinates in Debug Plains", 
            "golemplains, 313.3808, -50.90586, -195.3572",
            "Default scene to send the player to upon starting a run for the first time. Set to blank or an invalid scene name to disable.");
        Utils.StringConfig(cordinates);
        
        runCommands = config.Bind("kinaToolkit - debugplains", 
            "Debug Plains run start commands", 
            "no_enemies true; stage1_pod 0",
            "Commands run upon starting a Debug Plains run.");
        Utils.StringConfig(runCommands);
        
        stageCommands = config.Bind("kinaToolkit - debugplains", 
            "Debug Plains stage start commands", 
            "stop_timer 1; give_money 99999",
            "Comamnds run upon starting in the Debug Plains stage.");
        Utils.StringConfig(stageCommands);
    }

    private ConfigEntry<bool> enabled;
    private static ConfigEntry<bool> skipTitle;
    private static ConfigEntry<bool> skipLobby;
    private static ConfigEntry<string> sceneEntry;
    private static ConfigEntry<bool> disableInteractables;
    private static ConfigEntry<string> cordinates;
    private static ConfigEntry<string> runCommands;
    private static ConfigEntry<string> stageCommands;
}