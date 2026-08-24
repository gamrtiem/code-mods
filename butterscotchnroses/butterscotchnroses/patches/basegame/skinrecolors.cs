using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BepInEx;
using BNR.patches;
using BepInEx.Configuration;
using JetBrains.Annotations;
using On.RoR2.UI;
using R2API;
using Rewired;
using RoR2;
using RoR2.ContentManagement;
using TMPro;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;
using ColorCatalog = On.RoR2.ColorCatalog;
using Console = System.Console;
using Debug = UnityEngine.Debug;
using GameObjectFactory = IL.RoR2.GameObjectFactory;
using Object = UnityEngine.Object;
using Path = RoR2.Path;
using Stage = On.RoR2.Stage;

namespace BNR;

public class skinrecolors : PatchBase<skinrecolors>
{
    public static SkinDef baseSkinName;
    public static bool multiplySat;
    public static string newSkinName;
    public static CharacterBody currentBody;
    public static float[] hsv = [0, 0, 0];
    public static string textureDirs;
    public static SkinDef baseSkin;
    public static Dictionary<string, Texture2D> recoloredTextures = [];
    public static GameObject window;
    public override void Init()
    {
        textureDirs = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Paths.BepInExConfigPath)!, "skintextures");
        if (!Directory.Exists(textureDirs))
        {
            Directory.CreateDirectory(textureDirs);
        }

        baseSkin = Addressables.LoadAssetAsync<SkinDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Commando.skinCommandoDefault_asset).WaitForCompletion();
        applyHooks();
        
        GameObject skinLoader = PrefabAPI.CreateEmptyPrefab("skinloadercoroutines");
        skinLoader.AddComponent<monobehaviorskinloader>();
        Object.Instantiate(skinLoader);
    }

    public class monobehaviorskinloader : MonoBehaviour
    {
        public void OnEnable()
        {
            Log.Debug("starting custom skin loadings coroutine  !");
            StartCoroutine(LoadTextures());
        }
    }

    private static IEnumerator LoadTextures()
    {
        float totalTime = 0;
        string[] files = Directory.GetFiles(textureDirs, "*.*", SearchOption.AllDirectories);
        foreach (string texture in files)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            byte[] bytes = File.ReadAllBytes(texture);
            Texture2D returnTexture = new Texture2D(2, 2);
            returnTexture.LoadImage(bytes);
            
            Log.Debug($"loaded {texture.Split("\\")[^1]} in {stopwatch.ElapsedMilliseconds}ms !! adding to recoloredTextures ,.,.");
            recoloredTextures.Add(texture.Split("\\")[^1], returnTexture);
            totalTime += stopwatch.ElapsedMilliseconds;
            
            yield return null;
        }
        Log.Info($"total time loading recolored skins ,.., {totalTime}ms");
    }

    private void applyHooks()
    {
        if (enabled.Value)
        {
            On.RoR2.SkinCatalog.Init += SkinCatalogOnInit;
            RoR2.Run.onRunStartGlobal += RunOnonRunStartGlobal;
            On.RoR2.Stage.Start += StageOnStart;
        }
        else
        {
            On.RoR2.SkinCatalog.Init -= SkinCatalogOnInit;
            RoR2.Run.onRunStartGlobal -= RunOnonRunStartGlobal;
            On.RoR2.Stage.Start -= StageOnStart;
        }
    }
    
    private IEnumerator StageOnStart(Stage.orig_Start orig, RoR2.Stage self)
    {
        commands.bodyNameToPrev.Clear();
        yield return orig(self);
    }
    
    private static void RunOnonRunStartGlobal(Run obj)
    {
        commands.bodyNameToPrev.Clear();
    }

    private IEnumerator SkinCatalogOnInit(On.RoR2.SkinCatalog.orig_Init orig)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        stopwatch.Start();
        
        RecolorSkins();
        
        
        Log.Debug($"recolored {skinRecolors.Value.Split(";;").Length} skins in {stopwatch.ElapsedMilliseconds}ms !!!");
        yield return orig();
    }

    private static void RecolorSkins()
    {
        if (skinRecolors.Value.Length == 0) return;
        string[] newSkinRecolors = skinRecolors.Value.Split(";;");
        foreach (string skinRecolorParams in newSkinRecolors)
        {
            try
            {
                string[] skinArgs = skinRecolorParams.Split(",");

                skinRecolor(skinArgs[0],
                    skinArgs[1],
                    float.Parse(skinArgs[2]),
                    float.Parse(skinArgs[3]),
                    float.Parse(skinArgs[4]),
                    skinArgs[5],
                    skinArgs.Length == 7 ? skinArgs[6] : "",
                    skinArgs.Length == 8 && bool.Parse(skinArgs[7]));
            }
            catch(Exception e)
            {
                Log.Warning($"error while parsing config !!");
                Log.Warning(e);
            }
        }
    }

    public static SkinDef skinRecolor(string baseSkinDefName, string bodyName, float hue, float saturation, float value, string skinName, string prefix = "", bool dontAdd = false, bool multiplySaturation = false)
    {
        SkinDef recoloredSkinDef = baseSkin;
        
        try
        {
            ModelSkinController skinController = Utils.GetModelLocator(BodyCatalog.FindBodyPrefab(bodyName));
            if (skinController == null)
            {
                return recoloredSkinDef;
            }
            SkinDef originalSkin = null;
            foreach (var iterateSkinDef in skinController.skins)
            {
                if (iterateSkinDef.name != baseSkinDefName) continue;
                originalSkin = iterateSkinDef;
                break;
            }
            if(originalSkin == null) return recoloredSkinDef;

            SkinDef skinDef = UnityEngine.Object.Instantiate(originalSkin);
            
            Texture2D newTexture = Utils.hsvModifyTexture(skinDef.icon.texture, hue, saturation/100f, value/100f, dontAdd);
            Texture2D newIcon = new Texture2D(Mathf.FloorToInt(skinDef.icon.textureRect.width), Mathf.FloorToInt(skinDef.icon.textureRect.height));

            var pixels = newTexture.GetPixels(  
                Mathf.FloorToInt(skinDef.icon.textureRect.x), 
                Mathf.FloorToInt(skinDef.icon.textureRect.y), 
                Mathf.FloorToInt(skinDef.icon.textureRect.width), 
                Mathf.FloorToInt(skinDef.icon.textureRect.height) );
            newIcon.SetPixels(pixels);
            newIcon.Apply();
            
            Sprite newIconSprite = Sprite.Create(newIcon, new Rect(0, 0, newIcon.width, newIcon.height), new Vector2(newIcon.width / 2, newIcon.height / 2));
            skinDef.icon = newIconSprite;

            if (skinDef.skinDefParams == null && skinDef.skinDefParamsAddress == null)
            {
                //legacy skins use this i think .,.,
                CharacterModel.RendererInfo[] newRenderers = new CharacterModel.RendererInfo[skinDef.rendererInfos.Length];

                for (int i = 0; i < skinDef.rendererInfos.Length; i++)
                {
                    CharacterModel.RendererInfo renderer = skinDef.rendererInfos[i];
                    Material newMat = UnityEngine.Object.Instantiate(renderer.defaultMaterial);
                    renderer.defaultMaterial = Utils.RecolorMaterial(newMat, hue, saturation, value, dontAdd, multiplySaturation);
                    renderer.defaultMaterialAddress = new AssetReferenceT<Material>("");
                    newRenderers[i] = renderer;
                }

                skinDef.rendererInfos = newRenderers;
            }
            else
            {
                //Log.Debug($"skinDef.skinDefParamsAddress null ? {skinDef.skinDefParamsAddress == null}");
                //Log.Debug($"skinDef.skinDefParams null ? {skinDef.skinDefParams}");
                //Log.Debug($"SkinDefParams.FromSkinDef null ? {SkinDefParams.FromSkinDef(originalSkin)}");
                var newParams = UnityEngine.Object.Instantiate(skinDef.skinDefParamsAddress != null && skinDef.skinDefParamsAddress.ToString() != "[]" ? skinDef.skinDefParamsAddress.LoadAssetAsync().WaitForCompletion() : (skinDef.skinDefParams == null ? SkinDefParams.FromSkinDef(originalSkin) : skinDef.skinDefParams));

                for (int i = 0; i < newParams.rendererInfos.Length; i++)
                {
                    Material newMat = UnityEngine.Object.Instantiate(newParams.rendererInfos[i].defaultMaterial == null ? newParams.rendererInfos[i].defaultMaterialAddress.LoadAssetAsync().WaitForCompletion() : newParams.rendererInfos[i].defaultMaterial);
                    Log.Debug($"new mat name = {newMat.name}");
                    newParams.rendererInfos[i].defaultMaterial = Utils.RecolorMaterial(newMat, hue, saturation, value, dontAdd, multiplySaturation);
                    newParams.rendererInfos[i].defaultMaterialAddress = new AssetReferenceT<Material>("");
                }

                skinDef.optimizedSkinDefParams = newParams; 
                skinDef.skinDefParams = newParams;
                skinDef.skinDefParamsAddress = new AssetReferenceT<SkinDefParams>("");
            }

            string internalName = skinName.Replace(" ", "");
            skinDef.name = skinDef.name.Replace("(Clone)", "");
            skinDef.name += $"Recolored{internalName}";
            skinDef.name = prefix + skinDef.name; // if someone wants ot add like Red or something to check for wolfo ,.,.
            skinDef.nameToken += $"_BNR_{internalName.ToUpper()}";
            LanguageAPI.Add(skinDef.nameToken, skinName);

            if (!dontAdd)
            {
                Array.Resize(ref skinController.skins, skinController.skins.Length + 1);
                skinController.skins[^1] = skinDef;
            }
            Log.Debug($"added {skinName} to {bodyName} !!!!");

            recoloredSkinDef = skinDef;
        }
        catch (Exception e)
        {
            Log.Warning($"faileds to add {skinName} skin to {bodyName} ,.,.,.");
            Log.Error(e);
        }

        return recoloredSkinDef;
    }

    public override void Update()
    {
        CheckConsoleKey();

        if (window)
        {
            if (Input.GetKey(KeyCode.UpArrow))
            {
                skinrecolorui.cameraObject.transform.position = new Vector3(skinrecolorui.cameraObject.transform.position.x, skinrecolorui.cameraObject.transform.position.y + 0.04f, skinrecolorui.cameraObject.transform.position.z);
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                skinrecolorui.cameraObject.transform.position = new Vector3(skinrecolorui.cameraObject.transform.position.x, skinrecolorui.cameraObject.transform.position.y - 0.04f, skinrecolorui.cameraObject.transform.position.z);
            }
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                skinrecolorui.cameraObject.transform.position = new Vector3(skinrecolorui.cameraObject.transform.position.x - 0.04f, skinrecolorui.cameraObject.transform.position.y, skinrecolorui.cameraObject.transform.position.z);
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                skinrecolorui.cameraObject.transform.position = new Vector3(skinrecolorui.cameraObject.transform.position.x + 0.04f, skinrecolorui.cameraObject.transform.position.y, skinrecolorui.cameraObject.transform.position.z);
            }
        }
    }
    
    private static void CheckConsoleKey()
    {
        /*if (Input.GetKeyDown(KeyCode.F3))
        {
            if (window)
            {
                Object.Destroy(window.gameObject);
            }
            else
            {
                window = Object.Instantiate(butterscotchnroses.bnrBundle.LoadAsset<GameObject>("Menu"));
                skinrecolorui.BuildUI();
                
                window.GetComponent<RoR2.UI.MPEventSystemProvider>().eventSystem = MPEventSystemManager.kbmEventSystem;
                window.AddComponent<evilcomponnet>();
            }
        }
        if (Input.GetKeyDown(KeyCode.Escape) && window)
        {
            Object.Destroy(window.gameObject);
        }*/
    }

    public class skinrecolorui
    {
        public static Slider hueSlider;
        public static TMP_InputField hueInputField;
        public static Slider satSlider;
        public static TMP_InputField satInputField;
        public static Slider valSlider;
        public static TMP_InputField valInputField;
        public static Toggle useMulitplySat;
        public static TMP_InputField skinName;
        public static RoR2.UI.HGButton applySkin;
        public static RoR2.UI.HGButton createSkin;
        
        public static GameObject cameraObject;
        public static RoR2.UI.HGButton zoomIn;
        public static RoR2.UI.HGButton zoomOut;
        public static RoR2.UI.HGButton rotL;
        public static RoR2.UI.HGButton rotR;
        public static Toggle disableTurntable;
        public static tabletop turntable;

        public static void BuildUI()
        {
            GameObject mainPanel = window.transform.Find("MainPanel").gameObject;
            
            hueSlider = mainPanel.transform.Find("HueSlider").gameObject.GetComponent<Slider>();
            hueSlider.onValueChanged.AddListener(HueSliderListener);
            hueInputField = mainPanel.transform.Find("HueInputField").gameObject.GetComponent<TMP_InputField>();
            hueInputField.onValueChanged.AddListener(HueInputFieldListener);
            hueSlider.value = hsv[0];
            hueInputField.text = hsv[0].ToString(CultureInfo.InvariantCulture);

            satSlider = mainPanel.transform.Find("SatSlider").gameObject.GetComponent<Slider>();
            satSlider.onValueChanged.AddListener(SatSliderListener);
            satInputField = mainPanel.transform.Find("SatInputField").gameObject.GetComponent<TMP_InputField>();
            satInputField.onValueChanged.AddListener(SatInputFieldListener);
            satSlider.value = hsv[1];
            satInputField.text = hsv[1].ToString(CultureInfo.InvariantCulture);
            
            valSlider = mainPanel.transform.Find("ValSlider").gameObject.GetComponent<Slider>();
            valSlider.onValueChanged.AddListener(ValSliderListener);
            valInputField = mainPanel.transform.Find("ValInputField").gameObject.GetComponent<TMP_InputField>();
            valInputField.onValueChanged.AddListener(ValInputFieldListener);
            valSlider.value = hsv[2];
            valInputField.text = hsv[2].ToString(CultureInfo.InvariantCulture);
            
            useMulitplySat = mainPanel.transform.Find("SatMultiplyToggle").gameObject.GetComponent<Toggle>();
            useMulitplySat.onValueChanged.AddListener(UseMulitplySatToggleListener);
            useMulitplySat.isOn = multiplySat;

            skinName = mainPanel.transform.Find("SkinName").gameObject.GetComponent<TMP_InputField>();
            skinName.onValueChanged.AddListener(SkinNameInputFieldListener);
            skinName.text = newSkinName;

            applySkin = mainPanel.transform.Find("ApplySkin").gameObject.GetComponent<RoR2.UI.HGButton>();
            applySkin.onClick.AddListener(ApplySkinHGButtonListner);
            
            createSkin = mainPanel.transform.Find("CreateSkin").gameObject.GetComponent<RoR2.UI.HGButton>();
            createSkin.onClick.AddListener(CreateSkinHGButtonListner);
            
            currentBody = PlayerCharacterMasterController.instances[0].master.GetBody();

            cameraObject = new GameObject("Temp Camera");
            cameraObject.transform.SetParent(window.transform);
            Camera newCamera = cameraObject.AddComponent<Camera>();
           
            Log.Debug($"radius = {currentBody.radius}");
            
            //pull from modelpanelparameters 
            cameraObject.transform.position = new Vector3(0, currentBody.radius * 2, currentBody.radius * -4);
            cameraObject.layer = (int)LayerIndex.ragdoll;
            newCamera.clearFlags = CameraClearFlags.SolidColor;
            newCamera.backgroundColor = new Color(0, 0, 0, 0);
            newCamera.cullingMask = (int)LayerIndex.ragdoll;
            newCamera.targetTexture = butterscotchnroses.bnrBundle.LoadAsset<RenderTexture>("TestRenderTexture");
            
            GameObject survModel = Object.Instantiate(currentBody.modelLocator.modelTransform.gameObject);
            if (survModel.TryGetComponent(out AimAnimator aimAnimator))
            {
                aimAnimator.inputBank = null;
                aimAnimator.directionComponent = null;
            }
            survModel.transform.SetParent(window.transform);
            survModel.transform.position = new Vector3(0, 0, 0);
            survModel.transform.rotation = new Quaternion(0, 180, 0, 0);
            turntable = survModel.AddComponent<tabletop>();
            
            zoomIn = mainPanel.transform.Find("Preview").Find("ZoomIn").gameObject.GetComponent<RoR2.UI.HGButton>();
            zoomIn.onClick.AddListener(ZoomInHGButtonListner);
            
            zoomOut = mainPanel.transform.Find("Preview").Find("ZoomOut").gameObject.GetComponent<RoR2.UI.HGButton>();
            zoomOut.onClick.AddListener(ZoomOutHGButtonListner);
            
            rotL = mainPanel.transform.Find("Preview").Find("RotL").gameObject.GetComponent<RoR2.UI.HGButton>();
            rotL.onClick.AddListener(RotLHGButtonListner);
            
            rotR = mainPanel.transform.Find("Preview").Find("RotR").gameObject.GetComponent<RoR2.UI.HGButton>();
            rotR.onClick.AddListener(RotRHGButtonListner);
            
            disableTurntable = mainPanel.transform.Find("Preview").Find("DisableTurntable").gameObject.GetComponent<Toggle>();
            disableTurntable.onValueChanged.AddListener(DisableTurntableHGButtonListner);
        }
        
        private static void DisableTurntableHGButtonListner(bool isDisabled)
        {
            turntable.isDisabled = !isDisabled;
        }
            
        private static void RotLHGButtonListner()
        {
            turntable.Rotate(20);
        }
        
        private static void RotRHGButtonListner()
        {
            turntable.Rotate(-20);
        }
        
        private static void ZoomInHGButtonListner()
        {
            cameraObject.transform.position = new Vector3(cameraObject.transform.position.x, cameraObject.transform.position.y, cameraObject.transform.position.z + (1));
        }
        
        private static void ZoomOutHGButtonListner()
        {
            cameraObject.transform.position = new Vector3(cameraObject.transform.position.x, cameraObject.transform.position.y, cameraObject.transform.position.z - (1));
        }

        public class tabletop : MonoBehaviour
        {
            public bool isDisabled = true;
            public void FixedUpdate()
            {
                if (isDisabled)
                {
                    gameObject.transform.Rotate(0, 20*Time.deltaTime, 0); 
                }
            }

            public void Rotate(float rot)
            {
                gameObject.transform.Rotate(0, rot, 0); 
            }
        }
        
        private static void CreateSkinHGButtonListner()
        {
            string skinName = !string.IsNullOrEmpty(newSkinName) ? newSkinName : "Generated Skin";
            skinRecolors.Value += $";;{baseSkinName.name},{currentBody.name[..^7]},{hsv[0]},{hsv[1]},{hsv[2]},{skinName},{multiplySat}";
            Log.Debug($"added ;;{baseSkinName.name},{currentBody.name[..^7]},{hsv[0]},{hsv[1]},{hsv[2]},{skinName},{multiplySat} to the config !!! restart your game to see it in lobby .,,.");
        }

        private static void ApplySkinHGButtonListner()
        {
            ModelSkinController skinController = Utils.GetModelLocator(currentBody.gameObject);

            int currentIndex = skinController.currentSkinIndex;
            if (commands.bodyNameToPrev.TryGetValue(currentBody.name, out int value))
            {
                currentIndex = value;
            }
            else
            {
                commands.bodyNameToPrev.Add(currentBody.name, skinController.currentSkinIndex);
            }
            baseSkinName = skinController.skins[currentIndex];
            
            Log.Debug($"baseskin set to {skinController.currentSkinIndex}");
            Log.Debug($"currentBody set to {currentBody.name}");
            Log.Debug($"hsv[0] set to {hsv[0]}");
            Log.Debug($"hsv[1] set to {hsv[1]}");
            Log.Debug($"hsv[2] set to {hsv[2]}");
            Log.Debug($"newSkinName set to {newSkinName}");
            Log.Debug($"multiplySat set to {multiplySat}");
            
            SkinDef replacementSkin = skinRecolor(baseSkin.name, currentBody.name, hsv[0], hsv[1], hsv[2], newSkinName == "" ? "Generated Skin" : newSkinName, "", true, multiplySat);

            Array.Resize(ref skinController.skins, skinController.skins.Length + 1);
            skinController.skins[^1] = replacementSkin;
            skinController.currentSkinIndex = skinController.skins.Length - 1;
            currentBody.skinIndex = (uint)(skinController.skins.Length - 1);
        
#pragma warning disable CS0618 // Type or member is obsolete
            skinController.ApplySkin(skinController.currentSkinIndex);
#pragma warning restore CS0618 // Type or member is obsolete
        
            Array.Resize(ref skinController.skins, skinController.skins.Length - 1);
        }

        private static void UseMulitplySatToggleListener(bool isOn)
        {
            multiplySat = isOn;
        }
        
        private static void SkinNameInputFieldListener(string skinNameNew)
        {
            newSkinName = skinNameNew;
        }

        private static void HueInputFieldListener(string hueString)
        {
            Log.Debug($"hue input field  = {hueString}");
            if (int.TryParse(hueString, out int textField) && !Mathf.Approximately(textField, hueSlider.value))
            {
                hueSlider.value = textField;
                hsv[0] = textField;
            }
        }

        private static void HueSliderListener(float hueFloat)
        {
            Log.Debug($"hue = {hueFloat}");
            if (!int.TryParse(hueInputField.text, out int check) || int.TryParse(hueInputField.text, out int textField) && !Mathf.Approximately(textField, hueFloat))
            {
                hueInputField.text = hueFloat.ToString(CultureInfo.InvariantCulture);
                hsv[0] = hueFloat;
            }
        }
    
        private static void SatInputFieldListener(string satString)
        {
            Log.Debug($"sat input field  = {satString}");
            if (int.TryParse(satString, out int textField) && !Mathf.Approximately(textField, satSlider.value))
            {
                satSlider.value = textField;
                hsv[1] = textField;
            }
        }

        private static void SatSliderListener(float satFloat)
        {
            Log.Debug($"sat  = {satFloat}");
            if (!int.TryParse(satInputField.text, out int check) || int.TryParse(satInputField.text, out int textField) && !Mathf.Approximately(textField, satFloat))
            {
                satInputField.text = satFloat.ToString(CultureInfo.InvariantCulture);
                hsv[1] = satFloat;
            }
        }
    
        private static void ValInputFieldListener(string valString)
        {
            Log.Debug($"val input field  = {valString}");
            if (int.TryParse(valString, out int textField) && !Mathf.Approximately(textField, valSlider.value))
            {
                valSlider.value = textField;
                hsv[2] = textField;
            }
        }

        private static void ValSliderListener(float valFloat)
        {
            Log.Debug($"val  = {valFloat}");
            if (!int.TryParse(valInputField.text, out int check) || int.TryParse(valInputField.text, out int textField) && !Mathf.Approximately(textField, valFloat))
            {
                valInputField.text = valFloat.ToString(CultureInfo.InvariantCulture);
                hsv[2] = valFloat;
            }
        }
    }

    

    public class evilcomponnet : MonoBehaviour
    {
        public void Update()
        {
            EventSystem eventSystem = MPEventSystemManager.FindEventSystem(ReInput.players.GetPlayer(0));
            if (!eventSystem || eventSystem.currentSelectedGameObject != window.gameObject)
            {
                return;
            }
            eventSystem.SetSelectedGameObject(window.gameObject);
        }
    }

    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("BNR - skinrecolors",
            "enable patches for skinrecolors",
            true,
            "");
        Utils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) => { applyHooks(); };
        
        skinRecolors = config.Bind("BNR - skinrecolors",
            "skin recolors",
            "",
            "follows \"string baseSkinDefName, string bodyName, float hue, float saturation, float value, string skinName, string prefix\" where prefix is optional (used for like ,., Red on wolfo qol merc.,., use list_skins to get internal names or prodz debugging mod ,., split with ;; ..,,. you can temporarily try out recolors with recolor_skin hue saturation value ,.,,.");
        
        fileType = config.Bind("BNR - skinrecolors",
            "file type",
            fileTypes.png,
            "follows \"string baseSkinDefName, string bodyName, float hue, float saturation, float value, string skinName, string prefix\" where prefix is optional (used for like ,., Red on wolfo qol merc.,., use list_skins to get internal names or prodz debugging mod ,., split with ;; ..,,. you can temporarily try out recolors with recolor_skin hue saturation value ,.,,.");
    }

    public enum fileTypes
    {
        png,
        jpg,
        tga
    }
    public static ConfigEntry<fileTypes> fileType;
    public static ConfigEntry<string> skinRecolors;
    private static ConfigEntry<bool> enabled;
}