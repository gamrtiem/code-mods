using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using MonoMod.Cil;
using Newtonsoft.Json;
using R2API.Utils;
using RoR2;
using silly;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.AddressableAssets.Utility;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using Object = UnityEngine.Object;
using Path = System.IO.Path;

namespace silly
{
    [BepInDependency("iDeathHD.UnityHotReload", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class silly : BaseUnityPlugin
    {
        private const string PluginGUID = PluginAuthor + "." + PluginName;
        private const string PluginAuthor = "icebro";
        private const string PluginName = "silly";
        private const string PluginVersion = "1.0.0";
        

        private static List<GenericEdit> genericEditList;
        private static List<ItemEdit> itemCatalogEditList;
        private static bool UHRInstalled => Chainloader.PluginInfos.ContainsKey("iDeathHD.UnityHotReload");

        public void Awake()
        {
            Log.Init(Logger);
           // AssetBundle testbundle = new AssetBundle();
            //testbundle.GetAllScenePaths();

            On.RoR2.RoR2Application.OnLoad += (orig, self) =>
            {
                AssetPaths.UpdateAssetPathsToNames();
                return orig(self);
            };
            
            using (StreamReader r = new StreamReader(Path.Combine(Paths.ConfigPath, "genericEdits.json")))
            {
                string json = r.ReadToEnd();
                genericEditList = JsonConvert.DeserializeObject<List<GenericEdit>>(json);
            }

            // these can also be GUIDs ideally, but for nows lets just work with addressable paths ,..,
            foreach (GenericEdit edit in genericEditList)
            {
                Addressables.LoadAssetAsync<Object>(edit.prefabName).Completed += delegate(AsyncOperationHandle<Object> handle)
                {
                    try
                    {
                        GameObject hierarchy = null;
                        if (edit.hierarchy != null)
                        {
                            hierarchy = handle.Result as GameObject;
                            foreach (string t in edit.hierarchy)
                            {
                                if (int.TryParse(t, out int hierarchyIndex))
                                {
                                    hierarchy = hierarchy.transform.GetChild(hierarchyIndex).gameObject;
                                }
                                else
                                {
                                    hierarchy = hierarchy.transform.Find(t).gameObject;
                                }
                            }
                        }
                         
                        switch (edit.editType)
                        {
                            case "AddComponent":
                                if (hierarchy == null)
                                {
                                    hierarchy = handle.Result as GameObject;
                                    
                                    if (hierarchy == null)
                                    {
                                        Log.Error($"tried to addcomponent to something not a gameobject ,.,. {edit.prefabName}");
                                        return;
                                    }
                                }

                                string addComponent = edit.editParameters[0];
                                hierarchy.AddComponent(Type.GetType(addComponent));
                                break;
                            case "GetComponent":
                                if (hierarchy == null)
                                {
                                    hierarchy = handle.Result as GameObject;
                                    
                                    if (hierarchy == null)
                                    {
                                        Log.Error($"tried to addcomponent to something not a gameobject ,.,. {edit.prefabName}");
                                        return;
                                    }
                                }

                                string getComponent = edit.editParameters[0];
                                string operation = edit.editParameters[1];
                                string fieldName = edit.editParameters[2];
                                string operationArgument = edit.editParameters[3];
                                Log.Debug(getComponent);
                                
                                Log.Debug($"getting component {Utils.GetType(getComponent)} in {hierarchy}");
                                var obtainedComponent = hierarchy.GetComponent(Utils.GetType(getComponent));
                                switch (operation)
                                {
                                    case "Replace":
                                        Utils.replaceField(obtainedComponent, fieldName, operationArgument);
                                        break;
                                }
                                
                                break;
                            default:
                                Log.Error($"unknown edit type {edit.editType}!!");
                                break;
                        }

                        Log.Debug(handle.Result.name + " finished ,..,");
                    }
                    catch (Exception e)
                    {
                        Log.Error($"failed to edit prefab of path {edit.prefabName} !! error below .,,. ");
                        Log.Error(e.Message);
                    }
                };
            }
            
            ItemCatalog.availability.onAvailable += AvailabilityOnAvailable;
        }

        private void AvailabilityOnAvailable()
        {
            using (StreamReader r = new StreamReader(Path.Combine(Paths.ConfigPath, "itemCatalog.json")))
            {
                string json = r.ReadToEnd();
                itemCatalogEditList = JsonConvert.DeserializeObject<List<ItemEdit>>(json);
            }

            foreach (ItemEdit edit in itemCatalogEditList)
            {
                try
                {
                    ItemDef editItem = ItemCatalog.GetItemDef(ItemCatalog.FindItemIndex(edit.internalName));
                    if (editItem == null) continue;
                    
                    for (int i = 0; i < edit.editParameters.Length; i += 2)
                    {
                        string editField = edit.editParameters[i];
                        string editReplacement = edit.editParameters[i + 1];
                        
                        Utils.replaceField(editItem, editField, editReplacement);
                        // switch (editField)
                        // {
                        //     case ("Load"):
                        //         editItem.SetFieldValue(editField, editReplacement);
                        //         break;
                        //     case ("File"):
                        //         editItem.SetFieldValue(editField, Utils.Load(Path.Combine(Paths.ConfigPath, editReplacement)));
                        //         break;
                        // }
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"failed to edit item with internal name {edit.internalName} !! error below .,,. ");
                    Log.Error(e.Message);
                }
            }
        }

        // eg. GenericEdits.json
        // [
        //   {
        //     "prefabName": "RoR2/DLC3/Drifter/DrifterBody.prefab",
        //     "editType": "AddComponent",
        //     "editParameters": [
        //       "MeshRenderer"
        //     ]
        //   },
        //   {
        //     "prefabName": "RoR2/DLC3/Drifter/DrifterBody.prefab",
        //     "hierarchy": "test::test2::test3"
        //     "editType": "GetComponent",
        //     "editParameters": [
        //       "MeshRenderer",
        //       "Replace",
        //       "material",
        //       "Load::efb87e4ca777db44da34e51807b9e3ee"
        //     ]
        //   }
        // ]
        // efb87e4ca777db44da34e51807b9e3ee is guid for matIsShocked
        private class GenericEdit
        {
            public string prefabName;
            public string editType;
            public string[] hierarchy;
            public string[] editParameters;
        }

        // eg. ItemCatalogEdits.json (uses itemcatalog, specific to modded stuff) (maybe hook onto RoR2.onload for modded prefabs if possible to get? look into later ,.,.
        // [
        //   {
        //     "internalName": "Bear",
        //     "editFields": [
        //       "pickupIconSprite",
        //       "Load::(guid for icon here)"
        //     ]
        //   }
        // ]
        // efb87e4ca777db44da34e51807b9e3ee is guid for matIsShocked
        private class ItemEdit : GenericEdit
        {
            public string internalName;
            public string[] editFields; // edit fields will always be even, one for what the param is and the replace value
        }
        
        private void Update()
        {
#if DEBUG
            if (Input.GetKeyUp(KeyCode.F8))
            {
                if (UHRInstalled)
                {
                    UHRSupport.hotReload(typeof(silly).Assembly, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), "silly.dll"));
                }
                else
                {
                    Log.Debug("couldnt finds unity hot reload !!");
                }
            }
#endif  
        }
        
        [ConCommand(commandName = "type_test", flags = ConVarFlags.None, helpText = "bwaa")]
        public static void typetest(ConCommandArgs args)
        {
            Log.Debug("args = " + args[0]);
            Log.Debug($"type !! = {Utils.GetType(args[0]).Name}");
        }
    }
}
