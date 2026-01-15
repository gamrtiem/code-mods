using R2API;
using RoR2;
using UnityEngine;

namespace DroneRepairBeacon;

public class deadDroneTracker : MonoBehaviour, IHologramContentProvider
{
    public void Start()
    {
        gameObject.transform.localScale = new Vector3(DroneRepairBeacon.helpScale.Value, DroneRepairBeacon.helpScale.Value, DroneRepairBeacon.helpScale.Value);

        //doping this during initialization would change the local position and it just wasnt makes me go insane !! 
        Vector3 newPos = this.gameObject.transform.position; 
        newPos.y += 4f * DroneRepairBeacon.helpScale.Value; 
        gameObject.transform.position = newPos; 
    }

    GameObject IHologramContentProvider.GetHologramContentPrefab()
    {
        return hologramContentPrefab;
    }

    private static GameObject hologramContentPrefab 
    {
        get
        {
            if (!field)
            {
                field = DroneRepairBeacon.DroneIndicatorVFX.InstantiateClone("Drone Repair Beacon Hologram", false);
            }
            
            int rng = Run.instance.runRNG.RangeInt(0, DroneRepairBeacon.droneIndicatorSprites.Count);
            SpriteRenderer spriteRenderer = field.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = DroneRepairBeacon.droneIndicatorSprites[rng];
            spriteRenderer.SetMaterial(DroneRepairBeacon.droneIndicatorMaterials[rng]);
            
            return field;
        }
    }
    
    public void UpdateHologramContent(GameObject hologramContentObject, Transform viewerBody)
    {
        //Log.Debug("not importwant !");
    }

    bool IHologramContentProvider.ShouldDisplayHologram(GameObject viewer)
    {
        if (!viewer)
        {
            return false; 
        }

        float distance = Vector3.Distance(viewer.transform.position, gameObject.transform.position);
        return distance <= DroneRepairBeacon.displayDistance.Value;
    }
}