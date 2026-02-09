using System;
using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace DroneRepairBeacon;

public class deadDroneTracker : MonoBehaviour, IHologramContentProvider
{
    public void Start()
    {
        gameObject.transform.localScale = new Vector3(DroneRepairBeacon.helpScale.Value,
            DroneRepairBeacon.helpScale.Value, DroneRepairBeacon.helpScale.Value);

        //doping this during initialization would change the local position and it just wasnt makes me go insane !! 
        Vector3 newPos = this.gameObject.transform.position;
        newPos.y += 4f * DroneRepairBeacon.helpScale.Value;
        gameObject.transform.position = newPos;
        
        if (DroneRepairBeacon.droneIndicatorSprites.Count != 0)
        {
            messageID = Random.RandomRangeInt(0, DroneRepairBeacon.droneIndicatorSprites.Count);
        }
        Log.Debug($"own netid = {GetComponent<NetworkIdentity>().netId}");
        
        
    }

    GameObject IHologramContentProvider.GetHologramContentPrefab()
    {
        return hologramContentPrefab;
    }

    public int messageID = -1;

    private GameObject hologramContentPrefab
    {
        get
        {
            if (!field)
            {
                field = DroneRepairBeacon.DroneIndicatorVFX.InstantiateClone("Drone Repair Beacon Hologram", false);

                Log.Debug("spawning new !" + messageID);
                if (messageID != -1)
                {
                    SpriteRenderer spriteRenderer = field.GetComponent<SpriteRenderer>();
                    spriteRenderer.sprite = DroneRepairBeacon.droneIndicatorSprites[messageID];
                    spriteRenderer.SetMaterial(DroneRepairBeacon.droneIndicatorMaterials[messageID]);
                }
            }

            Log.Debug("getting prefab !" + messageID);

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

    public class recieveMessageID : INetMessage
    {
        NetworkInstanceId trackerNetID;
        int messageID;

        public recieveMessageID(NetworkInstanceId netId, int num)
        {
            trackerNetID = netId;
            messageID = num;
        }

        public recieveMessageID()
        {

        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(trackerNetID);
            writer.Write(messageID);
        }

        public void Deserialize(NetworkReader reader)
        {
            trackerNetID = reader.ReadNetworkId();
            messageID = reader.ReadInt32();
        }

        public void OnReceived()
        {
            if (NetworkServer.active)
            {
                return;
            }

            GameObject trackerObject = Util.FindNetworkObject(networkInstanceId: trackerNetID);
            if (!trackerObject)
            {
                Log.Warning(
                    $"{typeof(recieveMessageID).FullName}: Could not retrieve GameObject with network ID {trackerNetID}");
            }

            Log.Debug($"{trackerObject}");
            deadDroneTracker droneTracker = trackerObject.GetComponent<deadDroneTracker>();
            if (!droneTracker)
            {
                Log.Warning($"{typeof(recieveMessageID).FullName}: Retrieved GameObject {trackerObject} but the GameObject does not have a deadDroneTracker");
                return;
            }

            droneTracker.messageID = messageID;
        }
    }
}