using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using On.RoR2.Orbs;
using On.RoR2.UI;
using R2API;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using RoR2.UI.MainMenu;
using UnityEngine.UIElements;
using LanguageTextMeshController = IL.RoR2.UI.LanguageTextMeshController;
using MainMenuController = On.RoR2.UI.MainMenu.MainMenuController;
using Object = System.Object;
using RoR2Application = On.RoR2.RoR2Application;
using SkinDef = On.RoR2.SkinDef;
using SteamworksClientManager = On.RoR2.SteamworksClientManager;
using TitleMenuController = On.RoR2.UI.MainMenu.TitleMenuController;

namespace ExamplePlugin
{
    // This is an example plugin that can be put in
    // BepInEx/plugins/ExamplePlugin/ExamplePlugin.dll to test out.
    // It's a small plugin that adds a relatively simple item to the game,
    // and gives you that item whenever you press F2.

    // This attribute specifies that we have a dependency on a given BepInEx Plugin,
    // We need the R2API ItemAPI dependency because we are using for adding our item to the game.
    // You don't need this if you're not using R2API in your plugin,
    // it's just to tell BepInEx to initialize R2API before this plugin so it's safe to use R2API.
    [BepInDependency(ItemAPI.PluginGUID)]

    // This one is because we use a .language file for language tokens
    // More info in https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Localization/
    [BepInDependency(LanguageAPI.PluginGUID)]

    // This attribute is required, and lists metadata for your plugin.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    // This is the main declaration of our plugin class.
    // BepInEx searches for all classes inheriting from BaseUnityPlugin to initialize on startup.
    // BaseUnityPlugin itself inherits from MonoBehaviour,
    // so you can use this as a reference for what you can declare and use in your plugin class
    // More information in the Unity Docs: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    public class ExamplePlugin : BaseUnityPlugin
    {
        // The Plugin GUID should be a unique ID for this plugin,
        // which is human readable (as it is used in places like the config).
        // If we see this PluginGUID as it is on thunderstore,
        // we will deprecate this mod.
        // Change the PluginAuthor and the PluginName !
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "AuthorName";
        public const string PluginName = "ExamplePlugin";
        public const string PluginVersion = "1.0.0";

        // The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            // Init our logging class so that we can properly log for debugging
            Log.Init(Logger);

            On.RoR2.Orbs.HuntressArrowOrb.GetOrbEffect += HuntressArrowOrbOnGetOrbEffect;

            On.RoR2.SkinDef.ApplyAsync += SkinDefOnApplyAsync;
        }

        private IEnumerator SkinDefOnApplyAsync(SkinDef.orig_ApplyAsync orig, RoR2.SkinDef self, GameObject modelObject, List<AssetReferenceT<Material>> loadedMaterials, List<AssetReferenceT<Mesh>> loadedMeshes, AsyncReferenceHandleUnloadType unloadType)
        {
            var model = modelObject.GetComponent<CharacterModel>();

            //ignore if the model isnt huntress
            if (modelObject.name != "mdlHuntress") return orig(self, modelObject, loadedMaterials, loadedMeshes, unloadType);

            //automatically remove greentress in case it was applied previously
            Destroy(model.body.gameObject.GetComponent<greentress>());
            Logger.LogDebug(self.name);

            if (self.name.Contains("AltColossus")) // green huntress is skinHuntressAltColossus
            {
                model.body.gameObject.AddComponent<greentress>();
            }

            return orig(self, modelObject, loadedMaterials, loadedMeshes, unloadType);
        }

        public void Start()
        {
            //load the base orbs
            var basetrailmat = Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Huntress.matHuntressArrowTrail_mat).WaitForCompletion();
            var basearrowmat = Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Huntress.matHuntressArrow_mat).WaitForCompletion();
            var newramp = Addressables.LoadAssetAsync<Texture2D>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common_ColorRamps.texRampLoaderSwing_png).WaitForCompletion();

            //create clones of the vanilla materials but replace their color ramps with something else (eg. loaders swing which looks nice green !!
            newtrailmat = Instantiate(basetrailmat);
            newtrailmat.SetTexture("_RemapTex", newramp);
            Logger.LogDebug(newtrailmat.GetTexture("_RemapTex").name);

            newarrowmat = Instantiate(basearrowmat);
            newarrowmat.SetTexture("_RemapTex", newramp);
            Logger.LogDebug(newarrowmat.GetTexture("_RemapTex").name);

            //clone the old orb prefab and edit it to use the new materials
            neworbreturn = Addressables
            .LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Huntress
            .ArrowOrbEffect_prefab).WaitForCompletion().InstantiateClone("greentressorb", false);

            //orb prefab -> traileffect -> trail -> trailrenderer
            var trailrenderer = neworbreturn.transform.GetChild(0).transform.GetChild(0).GetComponent<TrailRenderer>();

            //arrow head is made up of two planes which are in the second child
            var arrowhead1 = neworbreturn.transform.GetChild(1).GetComponent<MeshRenderer>();
            var arrowhead2 = neworbreturn.transform.GetChild(1).transform.GetChild(0).GetComponent<MeshRenderer>();

            trailrenderer.material = newtrailmat;
            arrowhead1.material = newarrowmat;
            arrowhead2.material = newarrowmat;

            // make sure to content addition add effect !!!! will be invisible otherwise
            ContentAddition.AddEffect(neworbreturn);
        }

        public static GameObject neworbreturn;
        public static Material newtrailmat;
        public static Material newarrowmat;

        private GameObject HuntressArrowOrbOnGetOrbEffect(HuntressArrowOrb.orig_GetOrbEffect orig, RoR2.Orbs.HuntressArrowOrb self)
        {
            var green = self.attacker.GetComponent<greentress>();

            //if no greentress just return what it would in vanilla (base game orb
            if (!green) return RoR2.Orbs.OrbStorageUtility.Get("Prefabs/Effects/OrbEffects/ArrowOrbEffect");

            Logger.LogDebug("has greentress !!");

            //return new and awesome orb if we re green and awesome
            return neworbreturn;
        }
    }
}

public class greentress : MonoBehaviour // we just need this as a marker so no real point in it doing anything :/3
{
}
