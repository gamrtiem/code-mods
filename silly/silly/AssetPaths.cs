using System;
using System.Collections.Generic;
using System.Linq;
using silly;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace silly;

public class AssetPaths
{
    public static Dictionary<string, string> assetPathsToNames;

    //https://gist.github.com/xiaoxiao921/499361341751761f12514caaec8afb7b
    //stealings this function sorry !! 
    private static bool IsLoadableAsset(IResourceLocation key)
    {
        return key.ResourceType != typeof(SceneInstance) &&
               key.ResourceType != typeof(IAssetBundleResource) &&
               key.ProviderId != "UnityEngine.ResourceManagement.ResourceProviders.LegacyResourcesProvider" &&
               typeof(Object).IsAssignableFrom(key.ResourceType);
    }
    
    public static void UpdateAssetPathsToNames()
    {
        foreach (IResourceLocator resource in Addressables.ResourceLocators)
        {
            try
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
                        if (!IsLoadableAsset(assetPath)) continue;
                        
                        UnityEngine.Object asset = Addressables.LoadAssetAsync<UnityEngine.Object>(assetPath).WaitForCompletion();
                        Log.Debug($"yay loaded asset {asset.name} !!");
                        assetPathsToNames.Add(asset.name, assetPath.PrimaryKey);
                    }
                    catch(Exception e)
                    {
                        Log.Debug($"failed to get asset path for {assetPath} !!! printing error ,.,,. ");
                        Log.Debug(e);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("error when rebuilding asset paths !! " + e.Message);
            }
        }
    }
}