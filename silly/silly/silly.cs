using System;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
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

            // these can also be GUIDs ideally, but for nows lets just work with addressable paths ,..,
            foreach (string prefabPath in editPrefabs.Value.Split(","))
            {
                try
                {
                    string[] edits = prefabPath.Split(";");
                    Addressables.LoadAssetAsync<GameObject>(edits[0]).Completed += delegate(AsyncOperationHandle<GameObject> handle)
                    {
                        for (int i = 1; i < edits.Length; i++)
                        {
                            string[] editParameters = edits[i].Split("::");
                            string editType = editParameters[0];

                            switch (editType)
                            {
                                case "AddComponent":
                                    string addComponent = editParameters[1];
                                    handle.Result.AddComponent(Type.GetType(addComponent));
                                    break;
                                case "GetComponent":
                                    string getComponent = editParameters[1];
                                    string operation = editParameters[2];
                                    string fieldName = editParameters[3];
                                    string operationArgument = editParameters[4];
                                    Log.Debug(getComponent);
                                    var obtainedComponent = handle.Result.AddComponent(Type.GetType(getComponent));
                                    var field = obtainedComponent.GetType().GetFieldCached(fieldName);

                                    // switch (operation)
                                    // {
                                    //     case "Replace":
                                    //         
                                    //         field.SetValueDirect();
                                    // }
                                    break;
                                default:
                                    throw new Exception($"unknown edit type {editType}!!");
                            }
                            
                            
                        }
                        Log.Debug(handle.Result.name);
                    };
                }
                catch (Exception e)
                {
                    Log.Error($"failed to edit prefab of path {prefabPath} !! error below .,,. ");
                    Log.Error(e.Message);
                }
            }
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
