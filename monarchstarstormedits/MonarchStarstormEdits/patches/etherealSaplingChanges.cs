using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using RoR2;
using SS2;
using UnityEngine;
using UnityEngine.Networking;

namespace MonarchStarstormEdits.patches;

public class etherealSaplingChanges : PatchBase<etherealSaplingChanges>
{
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
            PortalStatueBehavior[] statues = Object.FindObjectsOfType<PortalStatueBehavior>(true).Where(p => p.portalType == PortalStatueBehavior.PortalType.Shop).ToArray();
            PortalStatueBehavior[] disabledStatues = statues.Where(p => !p.gameObject.activeInHierarchy).ToArray();
           
            if (disabledStatues.Length > 0)
            {
                Transform newt = disabledStatues[Random.Range(0, disabledStatues.Length)].transform;
                GameObject term = Object.Instantiate(EtherealBehavior.shrinePrefab, newt.position + Vector3.up * -1.2f, newt.rotation);
                NetworkServer.Spawn(term);     
            }
            else
            {
                Log.Debug("idk something bad happened ,..,,.");
            }
            
            return false;
        }
    }

    public override void Init(Harmony harmony)
    {
        if (!etherealChanges.Value) return;
        harmony.CreateClassProcessor(typeof(Starstorm2EtherealChanges)).Patch();
    }

    public override void Config(ConfigFile config)
    {
        etherealChanges = config.Bind("ethereal shrine change s,s,.,.", 
            "apply any ethereal sapling changes at al l !!", 
            true,
            "byeah ,.,");
        spawnChance = config.Bind("ethereal shrine change s,s,.,.", 
            "chance in percent to spawn ethereal shrine !! 1 is 100% ,.,,.", 
            0.5f,
            "byeah ,.,");
        sendChatMessage = config.Bind("ethereal shrine change s,s,.,.", 
            "should if a shrine spawn send a chat message !!", 
            true,
            "byeah ,.,");
        chatMessage = config.Bind("ethereal shrine change s,s,.,.", 
            "said chat message if chat message enabled ,,.", 
            "<style=\"cIsHealing\">Unknown forces start to coalesce...</style>",
            "byeah ,.,");
    }
    
    private static ConfigEntry<bool> etherealChanges;
    private static ConfigEntry<float> spawnChance;
    private static ConfigEntry<bool> sendChatMessage;
    private static ConfigEntry<string> chatMessage;
}