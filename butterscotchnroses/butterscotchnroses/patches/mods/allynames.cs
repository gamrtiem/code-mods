using AllyNames;
using static BNR.butterscotchnroses;
using BNR.patches;
using BepInEx.Configuration;
using HarmonyLib;
using Mono.Cecil.Cil;
using RoR2;
using UnityEngine;
using MonoMod.Cil;

namespace BNR;

public class allynames : PatchBase<allynames>
{
    [HarmonyPatch]
    public class AllyNamesChanges
    {
        [HarmonyPatch(typeof(NamesList), "InitConfig")]
        [HarmonyPostfix]
        public static void OnEnterPostFix(NamesList __instance)
        {
            foreach (string body in bodyNames.Value.Split('|'))
            {
                if (string.IsNullOrEmpty(body)) continue;
                string[] split = body.Split(',');
                string customNames = "";
                if (split.Length >= 4)
                {
                    customNames = split[3].Trim().TrimStart('[').TrimEnd(']').Replace(";", ",");
                }

                HG.ArrayUtils.ArrayAppend(ref NamesList.configBodyIndexes, new ConfigBodyIndex(
                    split[0].Trim(),
                    split[1].Trim(),
                    split[2].Trim().TrimStart('[').TrimEnd(']').Replace(";", ","),
                    customNames));
            }
            
            foreach (var VARIABLE in NamesList.configBodyIndexes)
            {
                Log.Debug(VARIABLE.categories);
                Log.Debug(VARIABLE.names);
                Log.Debug(VARIABLE.bodyIndex);
                Log.Debug(VARIABLE.realName);
            }
        }
    }
    public override void Init(Harmony harmony)
    {     
        if(!enabled.Value) { return; }
        Log.Debug("applying allynames patches !!");
        harmony.CreateClassProcessor(typeof(AllyNamesChanges)).Patch();
        NamesList.InitConfig();
        NamesList.BuildNamesByBodyName();
    }

    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - allynames",
            "enable patches for allynames",
            true,
            "");
        BNRUtils.CheckboxConfig(enabled);
        
        bodyNames = config.Bind("BNR - allynames",
            "ally name replace !!",
            "BombardmentDrone,Bombardment Drone,Default;Drones,Tsar Bomba;Fat Man;Little Boy|" +
            "JunkDrone,Junk Drone,Default;Drones|" +
            "RechargeDrone,Barrier Drone,Default;Drones|" +
            "JailerDrone,Jailer Drone,Default;Drones|" +
            "CleanupDrone,Cleanup Drone,Default;Drones,Roomba|" +
            "CopycatDrone,Freeze Drone,Default;Drones,Frozone|" +
            "HaulerDrone,Transport Drone,Default;Drones|" +
            "DTGunnerDroneBody,CROSSHAIRS,Default;Drones,Ruxin|" +
            "DTHealingDroneBody,DOC,Default;Drones,Bunny|" +
            "DTHaulerDroneBody,CHIRP,Default;Drones,Moose|" +
            "FriendUnitBody,Best Buddy,Default,Friend Inside Me;Stupid Baby;Son|" +
            "DroneBomber,Lt. Droneboy,Default,Lt. Beep Boop",
            "add custom !! use bodynamewithoutBody,realname,category1;category2,customname1;customname2|seconditem format to add custom names !!");
        BNRUtils.StringConfig(bodyNames);
    }

    public static ConfigEntry<string> bodyNames;
    private ConfigEntry<bool> enabled;
}