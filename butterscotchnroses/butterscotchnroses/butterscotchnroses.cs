using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using BepInEx;
using BNR.patches;
using ExamplePlugin.items;
using HarmonyLib;
using R2API;
using RoR2;
using UnityEngine;
using UnityHotReloadNS;
using RoR2.ContentManagement;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using RoR2.ExpansionManagement;


namespace BNR
{
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    public class butterscotchnroses : BaseUnityPlugin
    {
        public const string PluginGUID = "zzz" + PluginAuthor + "." + PluginName;

        public const string PluginAuthor = "icebro";
        public const string PluginName = "BNR";
        public const string PluginVersion = "1.0.0";

        public void Awake()
        {
            //TODO add making inferno + ESBM config not give them double jumps TT 
            //TODO add mod options button (uses something different i think idk( and highlighted text color change configfs 
            //TODO cleanesthud color force instead of survivor color 
            //TODO main menu pink color option like wolfo qol 
            Log.Init(Logger);
            Logger.LogDebug("loading mod !!");
            
            Harmony harmony = new(Info.Metadata.GUID);
            //look into items like how that works instead of individual
            var patches = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(PatchBase)));
            foreach (Type patch in patches)
            {
                PatchBase patchBase = (PatchBase)Activator.CreateInstance(patch);

                try
                {
                    patchBase.Config(Config);
                    patchBase.Init(harmony);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
            
            var buffTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(BuffBase)));

            foreach (var buffType in buffTypes)
            {
                BuffBase buff = (BuffBase)System.Activator.CreateInstance(buffType);
                buff.AddBuff();
            }
            
            //scan for all items
            var itemTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(ItemBase)));

            foreach (var itemType in itemTypes)
            {
                ItemBase item = (ItemBase)System.Activator.CreateInstance(itemType);
                item.Init(Config);
            }
        }
        
        void Update()
        {
            if (Input.GetKeyUp(KeyCode.F4))
            {
                UnityHotReload.LoadNewAssemblyVersion(
                    typeof(butterscotchnroses).Assembly, // The currently loaded assembly to replace.
                    "Z:\\run\\media\\icebrah\\buh\\gale\\riskofrain2\\profiles\\debug2 awese\\BepInEx\\plugins\\MarioVsLuigi-Linux-v1.7.1.0-beta\\butterscotchnroses.dll"  // The path to the newly compiled DLL.
                );
            }
            
            if (Input.GetKeyUp(KeyCode.F1))
            {
                Log.Debug("asdasdasdasd");
            }
        }
    }
}