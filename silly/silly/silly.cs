using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using BepInEx;
using BepInEx.Bootstrap;
using Newtonsoft.Json;
using R2API.Utils;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using Object = UnityEngine.Object;

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

        private static List<Edit> editList;
        private static Dictionary<string, string> assetPathsToNames;
        private static bool UHRInstalled => Chainloader.PluginInfos.ContainsKey("iDeathHD.UnityHotReload");

        public static List<GameObject> GetInstances<T>()
        {
        }

        public void Awake()
        {
            Log.Init(Logger);

            #region assetPathNameLoading
            //https://gist.github.com/xiaoxiao921/499361341751761f12514caaec8afb7b
            //stealings this function sorry !! 
            static bool IsLoadableAsset(IResourceLocation key)
            {
                return key.ResourceType != typeof(SceneInstance) &&
                       key.ResourceType != typeof(IAssetBundleResource) &&
                       key.ProviderId != "UnityEngine.ResourceManagement.ResourceProviders.LegacyResourcesProvider" &&
                       typeof(Object).IsAssignableFrom(key.ResourceType);
            }
            
            foreach (IResourceLocator resource in Addressables.ResourceLocators)
            {
                if (resource != typeof(ResourceLocationMap))
                {
                    Log.Debug(resource + " not a rlm !! continue ,..,");
                    continue;
                };
                
                HashSet<IResourceLocation> assetLocationsHash = [];
                ResourceLocationMap rlm = (ResourceLocationMap)resource;
                foreach (var resourceLocations in rlm.Locations)
                {
                    foreach (var location in resourceLocations.Value)
                    {
                        if (location.ResourceType != typeof(IAssetBundleResource))
                        {
                            assetLocationsHash.Add(location);
                        }
                    }
                }

                IResourceLocation[] assetLocationsArray = assetLocationsHash.ToArray();
                foreach (var assetPath in assetLocationsArray)
                {
                    try
                    {
                        if (IsLoadableAsset(assetPath))
                        {
                            var asset = Addressables.LoadAssetAsync<Object>(assetPath).WaitForCompletion();
                            
                            Log.Debug($"yay loaded asset {asset.name} !!");
                            assetPathsToNames.Add(asset.name, assetPath.PrimaryKey);
                        }
                    }
                    catch(Exception e)
                    {
                        Log.Debug($"failed to get asset path for {assetPath} !!! printing error ,.,,. ");
                        Log.Debug(e);
                    }
                }
            }
            #endregion
            
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
                    Addressables.LoadAssetAsync<Object>(edit.prefabName).Completed += delegate(AsyncOperationHandle<Object> handle)
                    {
                        switch (edit.editType)
                        {
                            case "AddComponent":
                                GameObject addComponentGameObject = handle.Result as GameObject;
                                if (addComponentGameObject == null)
                                {
                                    Log.Error($"tried to addcomponent to something not a gameobject ,.,. {edit.prefabName}");
                                    return;
                                }
                                
                                string addComponent = edit.editParameters[1];
                                addComponentGameObject.AddComponent(Type.GetType(addComponent));
                                break;
                            case "GetComponent":
                                GameObject getComponentGameObject = handle.Result as GameObject;
                                if (getComponentGameObject == null)
                                {
                                    Log.Error($"tried to addcomponent to something not a gameobject ,.,. {edit.prefabName}");
                                    return;
                                }
                                
                                string getComponent = edit.editParameters[1];
                                string operation = edit.editParameters[2];
                                string fieldName = edit.editParameters[3];
                                string operationArgument = edit.editParameters[4];
                                Log.Debug(getComponent);
                                
                                var obtainedComponent = getComponentGameObject.GetComponent(Type.GetType(getComponent));
                                switch (operation)
                                {
                                    case "Replace":
                                        string operationType = operationArgument.Split("::")[0];
                                        string operationValue = operationArgument.Split("::")[1];
                                        switch (operationType)
                                        {
                                            case("Load"):
                                                obtainedComponent.GetType().SetFieldValue(fieldName, Addressables.LoadAssetAsync<Object>(operationValue).WaitForCompletion());
                                                break;
                                            case("int"):
                                                obtainedComponent.GetType().SetFieldValue(fieldName, int.Parse(operationValue));
                                                break;
                                        }
                                        break;
                                }
                                break;
                            default:
                                Log.Error($"unknown edit type {edit.editType}!!");
                                break;
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

        public static bool isGameObject()
        {
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
