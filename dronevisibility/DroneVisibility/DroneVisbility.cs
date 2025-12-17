using BepInEx;
using BepInEx.Configuration;
using DroneVisibility;
using HarmonyLib;
using R2API;
using RoR2;
using RoR2.Hologram;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static R2API.LanguageAPI;

#pragma warning disable CS0618 // Type or member is obsolete

namespace DroneVisibility;

[BepInPlugin("icebro.dronevisibility", "dronevisibility", "1.0.0")]
public class DroneVisbility : BaseUnityPlugin
{
    private ConfigEntry<bool> allowTurrets;
    private ConfigEntry<bool> useSillyMaterials;
    private ConfigEntry<Color> sillyMaterialColor;
    private ConfigEntry<Color> operatorIndicatorColor;
    private ConfigEntry<float> visibilityDistance;
    private Material operatorMat;
    private Material sillyMat;
    
    public void Awake()
    {
        Hook();
        
        useSillyMaterials = Config.Bind("Drone Visibility",
            "use silly materials for indicator !!",
            true,
            "");
        allowTurrets = Config.Bind("Drone Visibility",
            "allow turrets !!",
            false,
            "");
        
        sillyMaterialColor = Config.Bind("Drone Visibility",
            "colors for silly sprite hologram thing material !!",
            new Color(191/255f, 126/255f, 211/255f),
            "");
        
        operatorIndicatorColor = Config.Bind("Drone Visibility",
            "colors for operator vfx thing !!",
            new Color(252/255f, 142/255f, 249/255f),
            "");
        operatorIndicatorColor.SettingChanged += (sender, args) =>
        {
            operatorMat = Instantiate(Addressables.LoadAssetAsync<Material>("RoR2/DLC3/Drone Tech/matDroneTechCarryRings.mat").WaitForCompletion());
            operatorMat.SetColor("_TintColor", operatorIndicatorColor.Value);
        };
        
        visibilityDistance = Config.Bind("Drone Visibility",
            "set distance which hologram becomes visible (set like 99999 for always !!",
            99999f,
            "");
        
        operatorMat = Instantiate(Addressables.LoadAssetAsync<Material>("RoR2/DLC3/Drone Tech/matDroneTechCarryRings.mat").WaitForCompletion());
        operatorMat.SetColor("_TintColor", operatorIndicatorColor.Value);
        sillyMat = Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC2.matHalcyoniteShrineCrystalGlow_mat).WaitForCompletion();
    }

    private void Hook()
    {
        On.RoR2.Hologram.HologramProjector.BuildHologram += HologramProjectorOnBuildHologram;
        On.RoR2.SummonMasterBehavior.OnEnable += SummonMasterBehaviorOnOnEnable;
    }

    private void SummonMasterBehaviorOnOnEnable(On.RoR2.SummonMasterBehavior.orig_OnEnable orig, SummonMasterBehavior self)
    {
        orig(self);
        if (!self.GetComponent<DroneAvailability>() && !self.GetComponent<SummonMasterBehavior>())
        {
            return;
        }
            
        HologramProjector projector = self.GetComponent<HologramProjector>();
        if (projector)
        {
            if (allowTurrets.Value || !projector.gameObject.name.Contains("Turret"))
            {
                projector.displayDistance = visibilityDistance.Value;
            }
        }
        else
        {
            Log.Warning("failed to find projector on drone !!");
        }
    }

    private void HologramProjectorOnBuildHologram(On.RoR2.Hologram.HologramProjector.orig_BuildHologram orig, HologramProjector self)
    {
        orig(self);
        if ((!self.gameObject.GetComponent<DroneAvailability>() && !self.GetComponent<SummonMasterBehavior>()) || (!allowTurrets.Value && self.gameObject.name.Contains("Turret")))
        {
            return;
        }
        
        Texture icon = self.gameObject.GetComponent<SummonMasterBehavior>().masterPrefab.GetComponent<CharacterMaster>().bodyPrefab.GetComponent<CharacterBody>().portraitIcon;
        Transform hologram = self.gameObject.transform.Find("HologramPivot");
            
        GameObject sprite = new("HologramSprite");
        sprite.transform.SetParent(hologram.transform);
        sprite.AddComponent<HologramHelper>();
        SpriteRenderer renderer = sprite.AddComponent<SpriteRenderer>();
        renderer.sprite = Sprite.Create(icon as Texture2D, new Rect(0, 0, icon.width, icon.height), new Vector2(0.5f, 0.5f));
        sprite.transform.localScale = new Vector3(2f, 2f, 1f);
        
        if (useSillyMaterials.Value)
        {
            renderer.sharedMaterial = sillyMat;
            renderer.sharedMaterial.SetTexture("_Cloud2Tex", Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common.texCloudOrganic2_png).WaitForCompletion());
            renderer.sharedMaterial.SetTexture("_Cloud1Tex", Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC2_Child.texChildPrimaryStarCloud_png).WaitForCompletion());
            renderer.sharedMaterial.SetColor("_TintColor", sillyMaterialColor.Value);
            renderer.sharedMaterial.SetInt("_AlphaBias", 0);
        }
        
        var vfx = Object.Instantiate(Addressables.LoadAssetAsync<GameObject>("RoR2/DLC3/Drone Tech/CommandCarryTransportVFX.prefab").WaitForCompletion(), sprite.transform, true);
        for(int i = 0; i < vfx.transform.childCount; i++)
        {
            GameObject vfxChild = vfx.transform.GetChild(i).gameObject;
            if (vfxChild.name != "MainRings")
            {
                Object.Destroy(vfxChild);
            }
            else
            {
                vfxChild.GetComponent<ParticleSystem>().startSpeed = -10;
                //vfxChild.GetComponent<ParticleSystem>().simulationSpace = ParticleSystemSimulationSpace.World;
                vfxChild.GetComponent<ParticleSystemRenderer>().sharedMaterial = operatorMat;
            }
        }
        vfx.transform.position = vfx.transform.localPosition;
        vfx.transform.localPosition = new Vector3(0, 0, 0);
        vfx.transform.position = new Vector3(vfx.transform.position.x, vfx.transform.position.y - 3.2f, vfx.transform.position.z);
    }
}

public class HologramHelper : MonoBehaviour
{
    private Transform hologram;
    public Vector3 pos;
    public void OnEnable()
    {
        hologram = transform.parent.GetChild(0);
        transform.position = transform.localPosition;
        transform.localPosition = new Vector3(0, 0, 0);
        transform.position = new Vector3(transform.position.x, transform.position.y + 2f, transform.position.z);
        
        //Log.Debug("" + hologram.gameObject.name);
    }

    public void FixedUpdate()
    {
        if(!hologram) 
        {
            Destroy(this.gameObject); // might want to hook onto where the hologram gets killed instead 
            return;
        }
        Vector3 target;
        target.x = 0;
        target.y = hologram.eulerAngles.y;
        target.z = hologram.eulerAngles.z;
        transform.rotation = Quaternion.Euler(target);
    }
}