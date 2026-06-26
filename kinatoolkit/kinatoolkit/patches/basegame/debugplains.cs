using BepInEx.Configuration;
using RoR2;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Console = RoR2.Console;

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

    public static bool enableAllFoodItems;
    
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
            On.RoR2.PickupTransmutationManager.GetGroupFromPickupIndex += PickupTransmutationManagerOnGetGroupFromPickupIndex;
        }
        else
        {
            On.RoR2.UI.MainMenu.BaseMainMenuScreen.OnEnter -= BaseMainMenuScreenOnOnEnter;
            On.RoR2.UI.CharacterSelectController.Awake -= CharacterSelectControllerOnAwake;
            On.RoR2.Run.OnEnable -= RunOnOnEnable;
            On.RoR2.Stage.GetPlayerSpawnTransform -= StageOnGetPlayerSpawnTransform;
            Run.onRunStartGlobal -= OnRunStart;
            On.RoR2.Stage.Start -= StageOnStart;
            On.RoR2.PickupTransmutationManager.GetGroupFromPickupIndex -= PickupTransmutationManagerOnGetGroupFromPickupIndex;
        }
    }
    

    private PickupIndex[] PickupTransmutationManagerOnGetGroupFromPickupIndex(On.RoR2.PickupTransmutationManager.orig_GetGroupFromPickupIndex orig, PickupIndex pickupindex)
    {
        PickupIndex[] returnIndex = orig(pickupindex);
        
        if (enableAllFoodItems)
        {
            if (pickupindex.pickupDef?.itemTier == ItemTier.FoodTier)
            {
                return PickupCatalog.entries.Where(entry => entry.itemTier == pickupindex.pickupDef?.itemTier && entry.itemIndex != ItemIndex.None).Select(entry => entry.pickupIndex).ToArray();
            }
        }
        
        return returnIndex;
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
    
    private static Transform StageOnGetPlayerSpawnTransform(On.RoR2.Stage.orig_GetPlayerSpawnTransform orig, Stage self)
    {
        Transform spawnPoint = orig(self);
        
        //this is run twice ? .,., for only once ? ,.., curious ., ,.
        if (!enteredScene || changedSpawnTransform >= 2) return spawnPoint;
        
        changedSpawnTransform++;

        if (changedSpawnTransform == 2)
        {
            commands.LoadJson();
        }
            
        if (disableInteractables.Value)
        {
            commands.disableInteractables = oldDisableInteractables;
        }

        return spawnPoint;
    }

    private static void RunOnOnEnable(On.RoR2.Run.orig_OnEnable orig, Run self)
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
    
    private static IEnumerator StageOnStart(On.RoR2.Stage.orig_Start orig, Stage self)
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
    private static ConfigEntry<string> runCommands;
    private static ConfigEntry<string> stageCommands;
}