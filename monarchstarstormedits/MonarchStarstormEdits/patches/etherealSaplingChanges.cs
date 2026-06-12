using System;
using System.Linq;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using HarmonyLib;
using On.EntityStates.AffixVoid;
using RoR2;
using RoR2BepInExPack.GameAssetPaths;
using SS2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.TextCore.Text;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace MonarchStarstormEdits.patches;

public class etherealSaplingChanges : PatchBase<etherealSaplingChanges>
{
    [HarmonyPatch]
    public class Starstorm2EtherealChanges
    {
        [HarmonyPatch(typeof(EtherealBehavior), "SpawnShrine")]
        [HarmonyPrefix]
        public static bool SpawnShrinePrefix(EtherealBehavior __instance)
        {
            float chance = Run.instance.runRNG.RangeFloat(0, 1);
            if (chance <= spawnChance.Value)
            {
                //Log.Debug("spawning shrine !!");
                
                if (sendChatMessage.Value)
                {
                    Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                    {
                        baseToken = chatMessage.Value
                    });
                }
                
                return true;
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