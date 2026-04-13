using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;
using Path = System.IO.Path;

namespace AssetEditor;

public class Edits
{
    private static List<GenericEdit> genericEditList;
    public static void addEdits()
    {
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
                    Log.Debug($"---------- {handle.Result.name} running edit ,.., ----------");
                    
                    //handle hierarchy if it exists .,,.
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

                    //handle component editing ,.,.
                    if (edit.editType.Contains("Component"))
                    {
                        if (hierarchy == null)
                        {
                            hierarchy = handle.Result as GameObject;
                                
                            if (hierarchy == null)
                            {
                                Log.Error($"tried to addcomponent to something not a gameobject ,.,. {edit.prefabName}");
                                return;
                            }
                        }
                    }
                     
                    switch (edit.editType)
                    {
                        case "AddComponent":
                            string addComponent = edit.editParameters[0];
                            hierarchy.AddComponent(Type.GetType(addComponent));
                            break;
                        case "RemoveComponent":
                            string removeComponent = edit.editParameters[0];
                            Object.Destroy(hierarchy.GetComponent(Type.GetType(removeComponent)));
                            break;
                        case "GetComponent":
                            string getComponent = edit.editParameters[0];
                            string operation = edit.editParameters[1];
                            string fieldName = edit.editParameters[2];
                            string operationArgument = edit.editParameters[3];
                            
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

                    Log.Debug($"---------- {handle.Result.name} edit finished ,.., ----------");
                }
                catch (Exception e)
                {
                    Log.Error($"---------- failed to edit prefab of path {edit.prefabName} !! error below .,,. ----------");
                    Log.Error(e.Message);
                    Log.Error($"----------");
                }
            };
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
}