using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using Newtonsoft.Json;
using R2API;
using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ExamplePlugin
{
    [BepInDependency("iDeathHD.UnityHotReload", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class silly : BaseUnityPlugin
    {
        private const string PluginGUID = PluginAuthor + "." + PluginName;
        private const string PluginAuthor = "icebro";
        private const string PluginName = "silly";
        private const string PluginVersion = "1.0.0";

        private static ConfigEntry<string> editPrefabs;
        private static List<Edit> editList;
        
        private static bool UHRInstalled => Chainloader.PluginInfos.ContainsKey("iDeathHD.UnityHotReload");
        
        public void Awake()
        {
            Log.Init(Logger);

            // this is going to be a bit complicated, but ideally data should be stored like this:
            // (path1);(edit1)::(edit parameter1);(edit2)::(edit parameter1)::(edit parameter2),(path2);(edit1)::(edit parameter1)
            // edit parameter will have to be from a set list (eg. AddComponent, GetComponent
            editPrefabs = Config.Bind("silly", 
                "Edited Prefabs", 
                "RoR2/DLC3/Drifter/DrifterBody.prefab;AddComponent:", 
                "test");
            
            using (StreamReader r = new StreamReader("file.json"))
            {
                string json = r.ReadToEnd();
                editList = JsonConvert.DeserializeObject<List<Edit>>(json);
            }

            // these can also be GUIDs ideally, but for nows lets just work with addressable paths ,..,
            foreach (Edit edit in editList)
            {
                try
                {
                    Addressables.LoadAssetAsync<GameObject>(edit.prefabName).Completed += delegate(AsyncOperationHandle<GameObject> handle)
                    {
                        switch (edit.editType)
                        {
                            case "AddComponent":
                                string addComponent = edit.editParameters[1];
                                handle.Result.AddComponent(Type.GetType(addComponent));
                                break;
                            case "GetComponent":
                                string getComponent = edit.editParameters[1];
                                string operation = edit.editParameters[2];
                                string fieldName = edit.editParameters[3];
                                string operationArgument = edit.editParameters[4];
                                Log.Debug(getComponent);
                                var obtainedComponent = handle.Result.AddComponent(Type.GetType(getComponent));
                                switch (operation)
                                {
                                    case "Replace":
                                        obtainedComponent.GetType().SetFieldValue(fieldName, Addressables.LoadAssetAsync<Type.GetType(getComponent)>(operationArgument).WaitForCompletion());
                                        break;
                                }
                                
                                // switch (operation)
                                // {
                                //     case "Replace":
                                //         
                                //         field.SetValueDirect();
                                // }
                                break;
                            default:
                                throw new Exception($"unknown edit type {edit.editType}!!");
                        }
                        Log.Debug(handle.Result.name);
                    };
                }
                catch (Exception e)
                {
                    Log.Error($"failed to edit prefab of path {edit.prefabName} !! error below .,,. ");
                    Log.Error(e.Message);
                }
            }
        }

        // [
        //   {
        //     "prefabName": "RoR2/DLC3/Drifter/DrifterBody.prefab",
        //     "editType": "AddComponent",
        //     "editParameters": [
        //       "MeshRenderer"
        //     ]
        //   }
        //   {
        //     "prefabName": "RoR2/DLC3/Drifter/DrifterBody.prefab",
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
        private class Edit
        {
            public string prefabName;
            public string editType;
            public string[] editParameters;
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
    }
}
