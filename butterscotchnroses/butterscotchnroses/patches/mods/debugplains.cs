using System;
using System.Linq;
using BNR.patches;
using BepInEx.Configuration;
using HarmonyLib;
using PhotoMode;
using Rewired;
using RiskOfOptions;
using RiskOfOptions.Options;
using RoR2;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BNR;

public class debugplains : PatchBase<debugplains>
{
	public override string chainLoaderKey => "com.Dragonyck.DebuggingPlains";
	
    [HarmonyPatch]
	public class DebuggingPlainsChanges
	{
		private static bool autoVote;
		private static bool autoMenu;
		private static bool autoPlains;
		
		[HarmonyPatch(typeof(DebuggingPlains.DebuggingPlains), "PreGameRuleVoteController_ServerHandleClientVoteUpdate")]
		[HarmonyPrefix]
		public static bool PreGameRuleVoteController_ServerHandleClientVoteUpdate(DebuggingPlains.DebuggingPlains __instance)
		{
			if (autoVote) return false;
	        
			autoVote = true;
			return true;
		}
		
		[HarmonyPatch(typeof(DebuggingPlains.DebuggingPlains), "MainMenuController_UpdateMenuTransition")]
		[HarmonyPrefix]
		public static bool MainMenuController_UpdateMenuTransition(DebuggingPlains.DebuggingPlains __instance)
		{
			if (autoMenu) return false;
	        
			autoMenu = true;
			return true;
		}
		
		[HarmonyPatch(typeof(DebuggingPlains.DebuggingPlains), "Run_PickNextStageScene")]
		[HarmonyPrefix]
		public static bool Run_PickNextStageScene(DebuggingPlains.DebuggingPlains __instance, On.RoR2.Run.orig_PickNextStageScene orig, Run self, WeightedSelection<SceneDef> choices)
		{
			if (autoPlains)
			{
				//dragonyck destructive hook ,.., yay !!!
				WeightedSelection<SceneDef> weightedSelection = new WeightedSelection<SceneDef>();
				string @string = RoR2.Run.cvRunSceneOverride.GetString();
				weightedSelection.AddChoice(SceneCatalog.GetSceneDefFromSceneName(@string), 1f);
				
				if ((bool)Run.instance.startingSceneGroup)
				{
					Run.instance.startingSceneGroup.AddToWeightedSelection(weightedSelection, Run.instance.CanPickStage);
				}
				else
				{
					for (int j = 0; j < Run.instance.startingScenes.Length; j++)
					{
						if (Run.instance.CanPickStage(Run.instance.startingScenes[j]))
						{
							weightedSelection.AddChoice(Run.instance.startingScenes[j], 1f);
						}
					}
				}

				if (choices.Count != 0)
				{
					if (Run.instance.ruleBook.stageOrder == StageOrder.Normal)
					{
						Run.instance.nextStageScene = choices.Evaluate(Run.instance.nextStageRng.nextNormalizedFloat);
						return false;
					}
					SceneDef[] array = SceneCatalog.allStageSceneDefs.Where(IsValidNextStage).ToArray();
					Run.instance.nextStageScene = Run.instance.nextStageRng.NextElementUniform(array);
				}
				bool IsValidNextStage(SceneDef sceneDef)
				{
					if (Run.instance.nextStageScene != null && Run.instance.nextStageScene.baseSceneName == sceneDef.baseSceneName)
					{
						return false;
					}
					if (!sceneDef.hasAnyDestinations)
					{
						return false;
					}
					if (Run.instance.stageClearCount == 0 && Run.instance.blacklistedScenesForFirstScene.Contains(sceneDef))
					{
						return false;
					}
					return sceneDef.validForRandomSelection;
				}

				return false;
			};

			
			autoPlains = true;
			return true;
		}
	}

    public override void Init()
    {
        if (!enabled.Value) return;
        
        butterscotchnroses.harmony.CreateClassProcessor(typeof(DebuggingPlainsChanges)).Patch();
    }
    
    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - debugging plains",
            "enable patches for debugging plains",
            true,
            "");
        Utils.CheckboxConfig(enabled);
    }

    private ConfigEntry<bool> enabled;
}