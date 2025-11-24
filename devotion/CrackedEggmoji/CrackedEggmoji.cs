using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using MonoMod.Cil;
using R2API;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CrackedEggmoji
{
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    public class CrackedEggmoji : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "icebro";
        public const string PluginName = "CrackedEggmoji";
        public const string PluginVersion = "1.0.0";
        
        enum interactable 
        {
            drones,
            chests,
            barrels
        }
    
        public void Awake()
        {
            Log.Init(Logger);
            
            //lemurian egg director card stuff
            var iscard = Addressables.LoadAssetAsync<InteractableSpawnCard>(RoR2BepInExPack.GameAssetPaths.RoR2_CU8_LemurianEgg.iscLemurianEgg_asset).WaitForCompletion();
            DirectorCard directorCard = new DirectorCard
            {
                selectionWeight = 35, 
                spawnCard = iscard,
            };
            DirectorAPI.DirectorCardHolder directorCardHolder = new DirectorAPI.DirectorCardHolder
            {
                Card = directorCard,
                InteractableCategory = DirectorAPI.InteractableCategory.Drones
            };
            
            
            //i love configs !!!!
            ConfigEntry<int> egglimit = Config.Bind("Egg behavior",
                "Egg limit",
                5,
                "max amount of eggs that can spawn in a stage with devotion enabled,.,.");
            IntSliderConfig sliderConfig = new IntSliderConfig();
            sliderConfig.max = 20;
            sliderConfig.min = 1;
            sliderConfig.formatString = "{0:0}";
            IntSliderOption slider = new IntSliderOption(egglimit, sliderConfig);
            ModSettingsManager.AddOption(slider);
            
            ConfigEntry<int> eggweight = Config.Bind("Egg behavior",
                "Egg weight",
                35,
                "selection weight of lemurian eggs (they dont have one in vanilla so umm this is just a guess !!! heigher = more common ,., lower = less ,,,,");
            IntSliderConfig sliderConfig2 = new IntSliderConfig();
            sliderConfig2.max = 350;
            sliderConfig2.min = 1;
            sliderConfig2.formatString = "{0:0}";
            IntSliderOption slider2 = new IntSliderOption(eggweight, sliderConfig2);
            ModSettingsManager.AddOption(slider2);
            
            ConfigEntry<int> eggcost = Config.Bind("Egg behavior",
                "Egg cost",
                15,
                "director credit cost for egg ,.,. how many it should gobble up !!");
            IntSliderConfig sliderConfig3 = new IntSliderConfig();
            sliderConfig3.max = 150;
            sliderConfig3.min = 1;
            sliderConfig3.formatString = "{0:0}";
            IntSliderOption slider3 = new IntSliderOption(eggcost, sliderConfig3);
            ModSettingsManager.AddOption(slider3);
            
            ConfigEntry<interactable>  interctablecategory = Config.Bind("Egg behavior",
                "director interactable category",
                interactable.drones,
                "sets what interactable category to use !!");
            ModSettingsManager.AddOption(new ChoiceOption(interctablecategory));
            
            
            //hooks ,.,.
            IL.RoR2.SceneDirector.PopulateScene += il =>
            {
                ILCursor c = new ILCursor(il);

                if (c.TryGotoNext(
                        x => x.MatchLdfld("RoR2.InteractableSpawnCard", "skipSpawnWhenDevotionArtifactEnabled"),
                        x => x.MatchBrfalse(out _),
                        x => x.MatchLdarg(0)
                ))
                {
                    c.Index -= 7;
                    //get rid of devotion interactable replacement checks
                    c.RemoveRange(12);
                    
                    //Log.Debug(c);
                    //Log.Debug(il.ToString());
                } else 
                {
                    Log.Error(il.Method.Name + " IL Hook failed!");
                }
            };
            On.RoR2.Run.Start += (orig, self) =>
            {
                DirectorAPI.Helpers.RemoveExistingInteractable(iscard.name);
                
                if (RunArtifactManager.instance.IsArtifactEnabled(CU8Content.Artifacts.Devotion))
                {
                    Log.Debug("user has devotion enabled !! adding egg to director ,.,");
                    iscard.directorCreditCost = eggcost.Value;
                    iscard.maxSpawnsPerStage = egglimit.Value;
                    directorCard.selectionWeight = eggweight.Value;
                    directorCardHolder.InteractableCategory = interctablecategory.Value switch
                    {
                        interactable.drones => DirectorAPI.InteractableCategory.Drones,
                        interactable.chests => DirectorAPI.InteractableCategory.Chests,
                        interactable.barrels => DirectorAPI.InteractableCategory.Barrels,
                        _ => directorCardHolder.InteractableCategory
                    };
                    DirectorAPI.Helpers.AddNewInteractable(directorCardHolder);
                    
                    Log.Debug(directorCard.selectionWeight + " weight !!");
                    Log.Debug(iscard.maxSpawnsPerStage + " cap !!");
                }
                else
                {
                    iscard.maxSpawnsPerStage = 0;
                }

                orig(self);
            };
        }
    }
}
