using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using R2API;
using RoR2;
using SS2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using PickupDropTable = On.RoR2.PickupDropTable;

namespace etherealreplace
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(SS2Main.GUID)]
    public class etherealreplace : BaseUnityPlugin
    {
        private const string PluginGUID = PluginAuthor + "." + PluginName;
        private const string PluginAuthor = "icebro";
        private const string PluginName = "etherealreplace";
        private const string PluginVersion = "1.0.0";
        
        private static ConfigEntry<bool> etherealChanges;
        private static ConfigEntry<float> spawnChance;
        private static ConfigEntry<bool> sendChatMessage;
        private static ConfigEntry<string> chatMessage;

        public void Awake()
        {
            Log.Init(Logger);

            etherealChanges = Config.Bind("ethereal shrine change s,s,.,.", 
                "apply any ethereal sapling changes at al l !!", 
                true,
                "byeah ,.,");
            spawnChance = Config.Bind("ethereal shrine change s,s,.,.", 
                "chance in percent to spawn ethereal shrine !! 1 is 100% ,.,,.", 
                0.5f,
                "byeah ,.,");
            sendChatMessage = Config.Bind("ethereal shrine change s,s,.,.", 
                "should if a shrine spawn send a chat message !!", 
                true,
                "byeah ,.,");
            chatMessage = Config.Bind("ethereal shrine change s,s,.,.", 
                "said chat message if chat message enabled ,,.", 
                "<style=\"cIsHealing\">Unknown forces start to coalesce...</style>",
                "byeah ,.,");
            
             Harmony harmony = new(Info.Metadata.GUID);
             if (etherealChanges.Value)
             {
                 harmony.CreateClassProcessor(typeof(Starstorm2EtherealChanges)).Patch();
             }

             harmony.CreateClassProcessor(typeof(Starstorm2GamblerFix)).Patch();
        }

        [HarmonyPatch]
        public class Starstorm2GamblerFix
        {
            [HarmonyPatch(typeof(SS2.RewardDropper), "GeneratePickups")]
            [HarmonyPrefix]
            public static bool PickupPostFix(RewardDropper __instance)
            {
                __instance.pickupQueue = new Queue<RewardDropper.RewardInfo>();
                PickupIndex[] guaranteedPickups = __instance.reward.pickups;
                for(int i = 0; i < guaranteedPickups.Length; i++)
                {
                    __instance.pickupQueue.Enqueue(new RewardDropper.RewardInfo { pickupIndex = guaranteedPickups[i] });
                }

                for(int i = 0; i < __instance.reward.white; i++)
                {
                    PickupIndex pickupIndex = __instance.rng.NextElementUniform<PickupIndex>(Run.instance.availableTier1DropList);
                    __instance.pickupQueue.Enqueue(new RewardDropper.RewardInfo { pickupIndex = pickupIndex });
                }
                for (int i = 0; i < __instance.reward.whiteOption; i++)
                {
                    List<UniquePickup> pickups = new List<UniquePickup>();
                    RewardDropper.dtTier1.GenerateDistinctPickups(pickups, 3, __instance.rng);
                    __instance.pickupQueue.Enqueue(new RewardDropper.RewardInfo { prefabOverride = RewardDropper.optionPrefab, options = PickupPickerController.GenerateOptionsFromList(pickups), pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.Tier1) });
                }
                for(int i = 0; i < __instance.reward.whiteCommand; i++)
                {
                    __instance.pickupQueue.Enqueue(new RewardDropper.RewardInfo { pickupIndex = Run.instance.availableTier1DropList[0], flag = GenericPickupController.PickupArtifactFlag.COMMAND });
                }

                for (int i = 0; i < __instance.reward.green; i++)
                {
                    PickupIndex pickupIndex = __instance.rng.NextElementUniform<PickupIndex>(Run.instance.availableTier2DropList);
                    __instance.pickupQueue.Enqueue(new RewardDropper.RewardInfo { pickupIndex = pickupIndex });
                }
                for (int i = 0; i < __instance.reward.greenOption; i++)
                {
                    List<UniquePickup> pickups = new List<UniquePickup>();
                    RewardDropper.dtTier2.GenerateDistinctPickups(pickups, 3, __instance.rng);
                    __instance.pickupQueue.Enqueue(new RewardDropper.RewardInfo { prefabOverride = RewardDropper.optionPrefab, options = PickupPickerController.GenerateOptionsFromList(pickups), pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.Tier2) });
                }
                for (int i = 0; i < __instance.reward.greenCommand; i++)
                {
                    __instance.pickupQueue.Enqueue(new RewardDropper.RewardInfo { pickupIndex = Run.instance.availableTier2DropList[0], flag = GenericPickupController.PickupArtifactFlag.COMMAND });
                }

                for (int i = 0; i < __instance.reward.red; i++)
                {
                    PickupIndex pickupIndex = __instance.rng.NextElementUniform<PickupIndex>(Run.instance.availableTier3DropList);
                    __instance.pickupQueue.Enqueue(new RewardDropper.RewardInfo { pickupIndex = pickupIndex });
                }
                for (int i = 0; i < __instance.reward.redOption; i++)
                {
                    List<UniquePickup> pickups = new List<UniquePickup>();
                    RewardDropper.dtTier3.GenerateDistinctPickups(pickups, 3, __instance.rng);
                    __instance.pickupQueue.Enqueue(new RewardDropper.RewardInfo { prefabOverride = RewardDropper.optionPrefab, options = PickupPickerController.GenerateOptionsFromList(pickups), pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.Tier3) });
                }
                for (int i = 0; i < __instance.reward.redCommand; i++)
                {
                    __instance.pickupQueue.Enqueue(new RewardDropper.RewardInfo { pickupIndex = Run.instance.availableTier3DropList[0], flag = GenericPickupController.PickupArtifactFlag.COMMAND });
                }
                
                return false;
            }
        }
        
        [HarmonyPatch]
        public class Starstorm2EtherealChanges
        {
            [HarmonyPatch(typeof(SS2.Components.TraderController), "BeginTrade")]
            [HarmonyPrefix]
            public static void BeginTradePrefix(SS2.Components.TraderController __instance, int intPickupIndex)
            {
                Log.Debug(intPickupIndex);
            }
            
            [HarmonyPatch(typeof(EtherealBehavior), "SpawnShrine")]
            [HarmonyPrefix]
            public static bool SpawnShrinePostFix(EtherealBehavior __instance)
            {
                float chance = Run.instance.runRNG.RangeFloat(0, 1);

                if (chance <= spawnChance.Value)
                {
                    Log.Debug("spawning shrine !!");
                    
                    if (sendChatMessage.Value)
                    {
                        Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                        {
                            baseToken = chatMessage.Value
                        });
                    }
                    
                    return true;
                }
                
                Log.Debug("not spawning ethereal shrine !! ");
                return false;
            }
            
            [HarmonyPatch(typeof(EtherealBehavior), "Start")]
            [HarmonyPrefix]
            public static void StartPostFix(EtherealBehavior __instance)
            {
                EtherealBehavior.alwaysReplaceNewts = false;
            }
            
            [HarmonyPatch(typeof(EtherealBehavior), "ReplaceRandomNewtStatue")]
            [HarmonyPrefix]
            public static bool ReplaceRandomNewtStatuePostFix(EtherealBehavior __instance)
            {
                PortalStatueBehavior[] statues = FindObjectsOfType<PortalStatueBehavior>(true).Where(p => p.portalType == PortalStatueBehavior.PortalType.Shop).ToArray();
                PortalStatueBehavior[] disabledStatues = statues.Where(p => !p.gameObject.activeInHierarchy).ToArray();
               
                if (disabledStatues.Length > 0)
                {
                    Transform newt = disabledStatues[Random.Range(0, disabledStatues.Length)].transform;
                    GameObject term = Instantiate(EtherealBehavior.shrinePrefab, newt.position + Vector3.up * -1.2f, newt.rotation);
                    NetworkServer.Spawn(term);     
                }
                else
                {
                    Log.Debug("idk something bad happened ,..,,.");
                }
                
                return false;
            }
        }
    }
}
