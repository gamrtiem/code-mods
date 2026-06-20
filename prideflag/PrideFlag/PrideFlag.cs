using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using Newtonsoft.Json.Utilities;
using R2API;
using RoR2;
using SS2;
using SS2.Equipments;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using Object = UnityEngine.Object;
using Path = RoR2.Path;

namespace PrideFlag;

[BepInDependency(SS2.SS2Main.GUID)]
[BepInDependency("iDeathHD.UnityHotReload", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(ItemAPI.PluginGUID)]
[BepInDependency(LanguageAPI.PluginGUID)]
[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
public class PrideFlag : BaseUnityPlugin
{
    private const string PluginGUID = PluginAuthor + "." + PluginName;
    private const string PluginAuthor = "kina";
    private const string PluginName = "PrideFlag";
    private const string PluginVersion = "1.0.0";
    public static AssetBundle prideBundle;
    public static ConfigEntry<bool> customFlagsEnabled; 
    public static ConfigEntry<string> customFlagColors; 
    public static ConfigEntry<string> steamidOverrides; 
    public static ConfigEntry<bool> logSteamid; 
    public static ConfigEntry<string> survivorOverrides; 
    private static bool UHRInstalled => Chainloader.PluginInfos.ContainsKey("iDeathHD.UnityHotReload");
        
    public void Awake()
    {
        Log.Init(Logger);

        RoR2Application.onLoad += OnLoad;

        if (DateTime.Now.Month == 6)
        {
            Log.Warning("its pride month nemesis commando ,.., you know what that means .,,. ");
        }
        
        Harmony harmony = new Harmony(PluginGUID);
        harmony.CreateClassProcessor(typeof(SS2WhiteFlagHooks)).Patch();
        
        prideBundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location)!, "pridebundle"));
    }
    
    [HarmonyPatch]
    public class SS2WhiteFlagHooks
    {
        [HarmonyPatch(typeof(WhiteFlag), "Execute")]
        [HarmonyPrefix]
        public static bool WhiteFlagExecutePrefix(ref bool __result, WhiteFlag __instance, EquipmentSlot slot)
        {
            GameObject gameObject = Instantiate(__instance._flagObject, slot.characterBody.corePosition, Quaternion.identity);
            
            BuffWard buffWard = gameObject.GetComponent<BuffWard>();
            buffWard.expireDuration = WhiteFlag.flagDuration;
            buffWard.radius = WhiteFlag.flagRadius;
            gameObject.GetComponent<TeamFilter>().teamIndex = slot.teamComponent.teamIndex;
            
            WardPrider wardPrider = gameObject.transform.Find("Model").gameObject.GetComponent<WardPrider>();
            wardPrider.characterBody = slot.characterBody;
            wardPrider.Gayify();
            
            NetworkServer.Spawn(gameObject);
            __result = true;
            return false;
        }
    }
    
    private void OnLoad()
    {
        GameObject wardPrefab = SS2Assets.LoadAsset<GameObject>("WhiteFlagWard", SS2Bundle.Equipments);
        wardPrefab.transform.Find("Model").gameObject.AddComponent<WardPrider>();
        
        GameObject displayPrefab = SS2Assets.LoadAsset<GameObject>("DisplayWhiteFlag", SS2Bundle.Equipments);
        displayPrefab.AddComponent<DisplayPrider>();
        
        PrideHelper.Init();

        customFlagsEnabled = Config.Bind("pride flag",
            "enable custom flags .,.,",
            true,
            "enable custom flags !! put texture pngs inside the \"PrideFlags\" folder in your config ,..,");
            
        customFlagColors = Config.Bind("pride flag",
            "custom flag colors .,,.",
            "asexual,#000000,#A3A3A3,#FFFFFF,#800080;",
            "colors for custom pride flags,.,. use formatting (png name),(color);(png name),(color) !!!");
        
        steamidOverrides = Config.Bind("pride flag",
            "steam id overrides .,,.",
            "STEAM_1:1:174533492,trans",
            "steam id overrides !! will override survivor specific .,,. base mod options \"nonbinary\",\"lesbian\",\"gay\",\"trans\",\"bi\" ,..,");
        
        logSteamid = Config.Bind("pride flag",
            "log steam id.,.,.",
            false,
            "log steam ids when picking up white flag !! useful when setting stea mid overrides .,.,");
        
        survivorOverrides = Config.Bind("pride flag",
            "survivor overrides .,,.",
            "RailgunnerBody,lesbian",
            "survivor overrides !! uses body names ,.,. base mod options \"nonbinary\",\"lesbian\",\"gay\",\"trans\",\"bi\" ,..,");

        LanguageAPI.Add("SS2_EQUIP_WHITEFLAG_NAME", "Pride Flag");
        LanguageAPI.Add("SS2_EQUIP_WHITEFLAG_PICKUP", "Place a pride flag that disables skill usage in an area.");
        LanguageAPI.Add("SS2_EQUIP_WHITEFLAG_DESC", $"Place a pride flag that <style=cIsUtility>disables skill usage</style> within <style=cIsUtility>{WhiteFlag.flagRadius}m</style>. Lasts <style=cIsUtility>{WhiteFlag.flagDuration} seconds</style>.");
        
        if (customFlagsEnabled.Value)
        {
            AddCustomFlags();
        }
        
        GameObject pickupPrefab = SS2Assets.LoadAsset<GameObject>("PickupWhiteFlag", SS2Bundle.Equipments);
        MeshRenderer pickupRenderer = pickupPrefab.transform.Find("mdlWhiteFlag").Find("PickupFlag").gameObject.GetComponent<MeshRenderer>();
        pickupRenderer.material.SetTexture(WardPrider.MainTex, PrideHelper.flagTextures.Keys.ToArray()[PrideHelper.GetFlagIndexFromName("gay")]);
    }

    private void AddCustomFlags()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Paths.ConfigPath)!, "config", "PrideFlags");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        List<string> flagColors = customFlagColors.Value.Split(";").ToList(); 
        string[] fileEntries = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
        foreach (string fileName in fileEntries)
        {
            string file = System.IO.Path.Combine(dir, fileName.Trim());
            if (!file.EndsWith(".png")) continue;
            Log.Debug($"loading flag {fileName.Replace(".png", "").Split("\\").Last()}");
                
            Texture loadedTexture = PrideHelper.LoadTextureFromFile(file);
            if (loadedTexture == null) continue;
            
            loadedTexture.name = fileName.Replace(".png", "").Split("\\").Last();
            
            Color[] colors = [];
            foreach (string flag in flagColors.Where(flag => flag.StartsWith(loadedTexture.name)))
            {
                Log.Debug($"found colors for flag {loadedTexture.name} !!");
                
                string[] hexCodes = flag.Replace($"{loadedTexture.name},", "").Split(",");
                colors = new Color[hexCodes.Length];
                for (int i = 0; i < hexCodes.Length; i++)
                {
                    colors[i] = PrideHelper.GetHex(hexCodes[i]);
                }
                
                break;
            }
            
            PrideHelper.AddFlag(loadedTexture, colors);
        }
    }
        
    private void Update()
    {
#if DEBUG
        if (Input.GetKeyUp(KeyCode.F8))
        {
            if (UHRInstalled)
            {
                UHRSupport.hotReload(typeof(PrideFlag).Assembly, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), "GayFPrideFlag"));
            }
            else
            {
                Log.Debug("couldnt finds unity hot reload !!");
            }
        }
#endif  
    }
}

public static class PrideHelper
{
    public static Dictionary<Texture, Color[]> flagTextures = [];

    public static void Init()
    {
        flagTextures.Add(SS2Assets.LoadAsset<Texture>("texWhiteFlagDiffuse", SS2Bundle.Equipments), [Color.white]);
        flagTextures.Add(PrideFlag.prideBundle.LoadAsset<Texture>("nonbinary"), [GetHex("#FCF434"), GetHex("#FFFFFF"), GetHex("#9C59D1"), GetHex("#2C2C2C")]);
        flagTextures.Add(PrideFlag.prideBundle.LoadAsset<Texture>("trans"), [GetHex("#20bbf8"), GetHex("#ec5f7b"), GetHex("#FFFFFF"), GetHex("#ec5f7b"), GetHex("#20bbf8")]);
        flagTextures.Add(PrideFlag.prideBundle.LoadAsset<Texture>("lesbian"), [GetHex("#D52D00"), GetHex("#EF7627"), GetHex("#FF9A56"), GetHex("#FFFFFF"), GetHex("#D162A4"), GetHex("#B55690"), GetHex("#A30262")]);
        flagTextures.Add(PrideFlag.prideBundle.LoadAsset<Texture>("bi"), [GetHex("#D60270"), GetHex("#9B4F96"), GetHex("#0038A8")]);
        flagTextures.Add(PrideFlag.prideBundle.LoadAsset<Texture>("gay"), [GetHex("#E40303"), GetHex("#FF8C00"), GetHex("#FFED00"), GetHex("#008026"), GetHex("#004CFF"), GetHex("#732982")]);
    }

    public static Texture GetFlagTexture(CharacterMaster master)
    {
        if (!master)
        {
            Log.Warning("master was null when trying to get flag texture !! returning base flag color ,.,.");
            return flagTextures.Keys.ToArray()[0];
        }

        WardPrideIntStore store = master.gameObject.GetComponent<WardPrideIntStore>();
        if (store == null)
        {
            store = master.gameObject.AddComponent<WardPrideIntStore>();
        }

        return flagTextures.Keys.ToArray()[store.flagType];
    }
    
    public static Color[] GetFlagColors(CharacterMaster master)
    {
        if (!master)
        {
            Log.Warning("master was null when trying to get flag colors !! returning base flag color ,.,.");
            return flagTextures.Values.ToArray()[0];
        }

        WardPrideIntStore store = master.gameObject.GetComponent<WardPrideIntStore>();
        if (store == null)
        {
            store = master.gameObject.AddComponent<WardPrideIntStore>();
        }

        return flagTextures.Values.ToArray()[store.flagType];
    }

    public static void AddFlag(Texture texture, Color[] colors)
    {
        if (texture == null)
        {
            Log.Warning("tried to add a flag with a null texture .,,.");
            return;
        }

        if (colors.Length == 0)
        {
            Log.Warning($"flag {texture.name} had no colors assigned .,,. going with white !");
            colors = [Color.white];
        }
        
        flagTextures.Add(texture, colors);
    }

    public static int GetFlagIndexFromName(string name)
    {
        Texture[] textures = flagTextures.Keys.ToArray();
        for (int i = 0; i < textures.Length; i++)
        {
            if (textures[i].name != name) continue;
            return i;
        }

        return -1;
    }

    public static Color GetHex(string hex)
    {
        if (!hex.StartsWith("#"))
        {
            hex = "#" + hex;
        }
        
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }
    
    public static Texture LoadTextureFromFile(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
        
        byte[] bytes = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
        texture.LoadImage(bytes);
            
        return texture;
    }
}

public class WardPrideIntStore : NetworkBehaviour
{
    [SyncVar]
    public int flagType;

    public void OnEnable()
    {
        if (!NetworkServer.active) return;

        flagType = UnityEngine.Random.Range(1, PrideHelper.flagTextures.Count);

        CharacterMaster master = gameObject.GetComponent<CharacterMaster>();
        
        string[] characterOverrides = PrideFlag.survivorOverrides.Value.Split(',').ToArray();

        string bodyName = master?.GetBody()?.name.Replace("(Clone)", "");
        int characterIndex = -1;
        for (int i = 0; i < characterOverrides.Length; i++)
        {
            if (characterOverrides[i] == bodyName && characterOverrides.Length != i + 1)
            {
                characterIndex = i;
            }
        }
        if (characterIndex != -1)
        {
            int flagIndex = PrideHelper.GetFlagIndexFromName(characterOverrides[characterIndex + 1]);
            if (flagIndex != -1)
            {
                flagType = flagIndex;
                Log.Debug($"overriding flag to {characterOverrides[characterIndex + 1]} !!");
            }
        }
        
        string steamid = master?.playerCharacterMasterController?.networkUser?.id.steamId.ToSteamID();
        if (steamid != null)
        {
            if (PrideFlag.logSteamid.Value)
            {
                Log.Debug($"steamid {steamid} !!");
            }
            
            Log.Debug(PrideFlag.steamidOverrides.Value);
            string[] steamIds = PrideFlag.steamidOverrides.Value.Split(',').ToArray();
            int index = -1;
            for (int i = 0; i < steamIds.Length; i++)
            {
                if (steamIds[i] == steamid && steamIds.Length != i + 1)
                {
                    index = i;
                }
            }
            if (index != -1)
            {
                Log.Debug($"steamsdfsdfid {steamid} !!");
                int flagIndex = PrideHelper.GetFlagIndexFromName(steamIds[characterIndex + 1]);
                if (flagIndex != -1)
                {
                    flagType = flagIndex;
                    Log.Debug($"overriding flag to {steamIds[characterIndex + 1]} !!");
                }
            }
        }
        
        Log.Debug($"set master flag type {PrideHelper.flagTextures.Keys.ToArray()[flagType].name}");
    }
}

public class WardPrider : MonoBehaviour
{
    public static readonly int MainTex = Shader.PropertyToID("_MainTex");
    private static readonly int Tint = Shader.PropertyToID("_TintColor");
    public CharacterBody characterBody;
    private Material wardFlagMat;
    private Material indicatorMat;
    private Color[] indicatorColors;
    private float colorIndex;

    public void Gayify()
    {
        Log.Debug("enabled ward");
        Log.Debug($"body null ? {characterBody == null}");
        
        
        SkinnedMeshRenderer flagRenderer = gameObject.transform.Find("mdlWhiteFlag").Find("FlagBendy").gameObject.GetComponent<SkinnedMeshRenderer>();
        wardFlagMat = Instantiate(flagRenderer.material);
        wardFlagMat.SetTexture(MainTex, PrideHelper.GetFlagTexture(characterBody.master));
        
        MeshRenderer indicatorRenderer = gameObject.transform.Find("Indicator").Find("Mesh").gameObject.GetComponent<MeshRenderer>();
        indicatorMat = Instantiate(indicatorRenderer.material);

        flagRenderer.material = wardFlagMat;
        indicatorRenderer.material = indicatorMat;

        indicatorColors = PrideHelper.GetFlagColors(characterBody.master);
    }

    private void Update()
    {
        if (!indicatorMat) return;
        if (indicatorColors.Length < (int)colorIndex)
        {
            colorIndex = 0; 
        }
        
        Log.Debug($"mod {colorIndex % 1} index {(int)colorIndex} length {indicatorColors.Length}");

        Color initalColor = (int)colorIndex > 1 ? indicatorColors[(int)colorIndex - 1] : indicatorColors[0];
        indicatorMat.SetColor(Tint, Color.Lerp(initalColor, indicatorColors.Length == (int)colorIndex ? indicatorColors[0] : indicatorColors[(int)colorIndex], colorIndex % 1));
           
        //lasts for 15s ,,. cycle through twice !!
        colorIndex += Time.deltaTime * (indicatorColors.Length/WhiteFlag.flagDuration) * 2.3f;
    }

    private void OnDestroy()
    {
        Log.Debug("killing mat ward .,,");
        Destroy(wardFlagMat);
    }
}

public class DisplayPrider : MonoBehaviour
{
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");
    private ItemDisplay display;
    
    public CharacterBody characterBody;
    private Material displayFlagMat;
    
    private void OnEnable()
    {
        Log.Debug("enabled display");
        
        display = gameObject.GetComponent<ItemDisplay>();
        characterBody = GetComponentInParent<CharacterModel>()?.body;
        displayFlagMat = Instantiate(gameObject.transform.Find("mdlWhiteFlag").Find("FlagBendy").gameObject.GetComponent<MeshRenderer>().material);
        
        displayFlagMat.SetTexture(MainTex, PrideHelper.GetFlagTexture(characterBody?.master));
        display.rendererInfos[0].defaultMaterial = displayFlagMat;
        gameObject.transform.Find("mdlWhiteFlag").Find("FlagBendy").gameObject.GetComponent<MeshRenderer>().material = displayFlagMat;
    }

    private void OnDestroy()
    {
        Log.Debug("killing mat disaply .,,");
        Destroy(displayFlagMat);
    }
}

