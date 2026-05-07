using System;
using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace DroneRepairBeacon;

public class deadDroneTracker : NetworkBehaviour, IHologramContentProvider
{
    [SyncVar]
    public int messageID = -1;
    [SyncVar]
    public bool usingSpecificSprite;

    public void Start()
    {
        gameObject.transform.localScale = new Vector3(DroneRepairBeacon.helpScale.Value, DroneRepairBeacon.helpScale.Value, DroneRepairBeacon.helpScale.Value);

        //doping this during initialization would change the local position and it just wasnt makes me go insane !! 
        Vector3 newPos = this.gameObject.transform.position;
        newPos.y += 4f * DroneRepairBeacon.helpScale.Value;
        gameObject.transform.position = newPos;

        //if (!NetworkServer.active)
        //{
        //    new sendMessageID(GetComponent<NetworkIdentity>().netId, usingSpecificSprite).Send(NetworkDestination.Server);
        //}
    }

    GameObject IHologramContentProvider.GetHologramContentPrefab()
    {
        return messageID == -1 ? null : hologramContentPrefab;
    }

    public void UpdateSprite()
    {
        SpriteRenderer spriteRenderer = hologramContentPrefab.GetComponent<SpriteRenderer>();
        if (usingSpecificSprite)
        {
            spriteRenderer.sprite = DroneRepairBeacon.specificDroneIndicatorSprites[messageID];
            spriteRenderer.SetMaterial(DroneRepairBeacon.specificDroneIndicatorMaterials[messageID]);
        }
        else
        {
            spriteRenderer.sprite = DroneRepairBeacon.droneIndicatorSprites[messageID];
            spriteRenderer.SetMaterial(DroneRepairBeacon.droneIndicatorMaterials[messageID]);
        }
    }


    private GameObject hologramContentPrefab
    {
        get
        {
            if (field) return field;
            
            field = DroneRepairBeacon.DroneIndicatorVFX.InstantiateClone("Drone Repair Beacon Hologram", false);
            UpdateSprite();
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