using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using R2API;
using RoR2;
using UnityEngine;

namespace weirdOnError
{
    [BepInDependency(SoundAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class weirdOnError : BaseUnityPlugin
    {
        private const string PluginGUID = PluginAuthor + "." + PluginName;
        private const string PluginAuthor = "kina";
        private const string PluginName = "weirdOnError";
        private const string PluginVersion = "1.0.0";
        
        private static List<float> weirdStack = []; 
        
        private static ConfigEntry<bool> useOnLog;
        private static ConfigEntry<bool> useOnWarning;
        private static ConfigEntry<bool> useOnError;
        private static ConfigEntry<bool> useOnException;
        private static ConfigEntry<int> weirdHell;
        private static ConfigEntry<int> weirdHellSuper;
        private static ConfigEntry<int> weirdHellSeconds;
        private static GameObject weirdSoundPlayer;
        
        /*
         * 	1026216972	weird
         *  2889195838	weird_hell
         *  1445867670	weird_hell_super
         */
        
        public void Awake()
        {
            useOnLog = Config.Bind("weirdOnError", "jingle on log", false, "");
            useOnWarning = Config.Bind("weirdOnError", "jingle on warning", false, "");
            useOnError = Config.Bind("weirdOnError", "jingle on error", false, "");
            useOnException = Config.Bind("weirdOnError", "jingle on exception", true, "");
            
            weirdHellSeconds = Config.Bind("weirdOnError", "amount of time multiple errors have to be within to start playing weird_hell and weird_hell_super ,..", 3, "");
            weirdHell = Config.Bind("weirdOnError", "amount of errors to start playing weird_hell in ,..", 15, "");
            weirdHellSuper = Config.Bind("weirdOnError", "amount of errors to start playing weird_hell_super in ,..", 50, "");

            RoR2Application.onLoadFinished += () =>
            {
                Application.logMessageReceived += ApplicationOnlogMessageReceived;
            };
            
            weirdSoundPlayer = new GameObject("weirdSoundPlayer");
            DontDestroyOnLoad(weirdSoundPlayer);
        }

        private static void ApplicationOnlogMessageReceived(string condition, string stacktrace, LogType type)
        {
            if (type == LogType.Log && useOnLog.Value || type == LogType.Warning && useOnWarning.Value || type == LogType.Error && useOnError.Value || type == LogType.Exception && useOnException.Value)
            {
                weirdStack = weirdStack.Where(weird => Time.time - weird < weirdHellSeconds.Value).ToList();
                if (weirdStack.Count >= weirdHellSuper.Value)
                {
                    AkSoundEngine.PostEvent(1445867670, weirdSoundPlayer);
                }
                else if (weirdStack.Count >= weirdHell.Value)
                {
                    AkSoundEngine.PostEvent(2889195838, weirdSoundPlayer);
                }
                else
                {
                    AkSoundEngine.PostEvent(1026216972, weirdSoundPlayer);
                }
                weirdStack.Add(Time.time);
            }
        }
    }
}
