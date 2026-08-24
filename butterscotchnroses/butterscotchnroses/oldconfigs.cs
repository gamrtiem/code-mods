using BepInEx.Configuration;
using BNR;

namespace butterscotchnroses;

public class oldconfigs
{
    private static ConfigEntry<bool> replaceOldConfigs;
    
    public static void fixOldConfigs()
    {
        replaceOldConfigs = BNR.butterscotchnroses.instance.Config.Bind("!!!Config Replacement!!!",
            "whether or not to replace/fix old default configs ,..,",
            true,
            "basically uhh replace default configs if enabled.,,.");

        if (!replaceOldConfigs.Value) return;

        string oldCoolerEclipse = "golemplains,golemplains2";
        if (coolereclipse.whitelistStages.Value == oldCoolerEclipse)
        {
            coolereclipse.whitelistStages.Value = (string)coolereclipse.whitelistStages.DefaultValue;
        }
        
        string oldSkinRecolor = "skinCommandoAlt,CommandoBody,100,0,0,Test Skin;;skinCommandoAlt,CommandoBody,200,0,0,Test Skin 2;;skinCommandoDefault,CommandoBody,290,-40,-10,Awesome Skin !!!!";
        if (skinrecolors.skinRecolors.Value == oldSkinRecolor)
        {
            skinrecolors.skinRecolors.Value = (string)skinrecolors.skinRecolors.DefaultValue;
        }

        string olderCoolerEclipseBlacklist = "goldshores,bazaar,solutionalhaunt,ss2_voidshop,goldshores";
        if (coolereclipse.blacklistStages.Value == olderCoolerEclipseBlacklist)
        {
            coolereclipse.blacklistStages.Value = (string)coolereclipse.blacklistStages.DefaultValue;
        }
        
        string oldAllyNames = "BombardmentDrone,Bombardment Drone,Default;Drones,Tsar Bomba;Fat Man;Little Boy|" +
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
                              "DroneBomber,Lt. Droneboy,Default,Lt. Beep Boop";
        if (allynames.bodyNames.Value == oldAllyNames)
        {
            allynames.bodyNames.Value = (string)allynames.bodyNames.DefaultValue;
        }
    }
}