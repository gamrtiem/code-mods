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
    public int messageID = -1;
    public bool usingSpecificSprite;

    public void Start()
    {
        gameObject.transform.localScale = new Vector3(DroneRepairBeacon.helpScale.Value, DroneRepairBeacon.helpScale.Value, DroneRepairBeacon.helpScale.Value);

        //doping this during initialization would change the local position and it just wasnt makes me go insane !! 
        Vector3 newPos = this.gameObject.transform.position;
        newPos.y += 4f * DroneRepairBeacon.helpScale.Value;
        gameObject.transform.position = newPos;

        if (!NetworkServer.active)
        {
            new sendMessageID(GetComponent<NetworkIdentity>().netId, usingSpecificSprite).Send(NetworkDestination.Server);
        }
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

    public class recieveMessageID : INetMessage
    {
        NetworkInstanceId trackerNetID;
        int messageID;
        bool useSpecificSprite;

        public recieveMessageID(NetworkInstanceId netId, int num, bool useSpecific)
        {
            trackerNetID = netId;
            messageID = num;
            useSpecificSprite = useSpecific;
        }

        public recieveMessageID()
        {

        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(trackerNetID);
            writer.Write(messageID);
            writer.Write(useSpecificSprite);
        }

        public void Deserialize(NetworkReader reader)
        {
            trackerNetID = reader.ReadNetworkId();
            messageID = reader.ReadInt32();
            useSpecificSprite = reader.ReadBoolean();
        }

        public void OnReceived()
        {
            if (NetworkServer.active || messageID != -1)
            {
                return;
            }

            GameObject trackerObject = Util.FindNetworkObject(networkInstanceId: trackerNetID);
            if (!trackerObject)
            {
                Log.Warning($"{typeof(recieveMessageID).FullName}: Could not retrieve GameObject with network ID {trackerNetID}");
                return;
            }

            Log.Debug($"{trackerObject}");
            deadDroneTracker droneTracker = trackerObject.GetComponent<deadDroneTracker>();
            if (!droneTracker)
            {
                Log.Warning($"{typeof(recieveMessageID).FullName}: Retrieved GameObject {trackerObject} but the GameObject does not have a deadDroneTracker");
                return;
            }

            droneTracker.messageID = messageID;
            droneTracker.usingSpecificSprite = useSpecificSprite;
            Log.Debug("recieved message id ! " + messageID);
            droneTracker.UpdateSprite();
        }
    }

    public class sendMessageID : INetMessage
    {
        NetworkInstanceId trackerNetID;
        bool useSpecificSprites;

        public sendMessageID(NetworkInstanceId netId, bool useSpecific)
        {
            trackerNetID = netId;
            useSpecificSprites = useSpecific;
        }

        public sendMessageID()
        {

        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(trackerNetID);
            writer.Write(useSpecificSprites);
        }

        public void Deserialize(NetworkReader reader)
        {
            trackerNetID = reader.ReadNetworkId();
            useSpecificSprites = reader.ReadBoolean();
        }

        public void OnReceived()
        {
            if (!NetworkServer.active)
            {
                return;
            }
            
            Log.Debug("sending back proper message id ,..,");
            
            GameObject trackerObject = Util.FindNetworkObject(networkInstanceId: trackerNetID);
            if (!trackerObject)
            {
                Log.Warning(
                    $"{typeof(recieveMessageID).FullName}: Could not retrieve GameObject with network ID {trackerNetID}");
            }

            deadDroneTracker droneTracker = trackerObject.GetComponent<deadDroneTracker>();
            if (!droneTracker)
            {
                Log.Warning($"{typeof(recieveMessageID).FullName}: Retrieved GameObject {trackerObject} but the GameObject does not have a deadDroneTracker");
                return;
            }
            
            new recieveMessageID(trackerNetID, droneTracker.messageID, droneTracker.usingSpecificSprite).Send(NetworkDestination.Clients);
        }
    }
}