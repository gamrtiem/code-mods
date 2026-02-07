using R2API;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

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
    }

    GameObject IHologramContentProvider.GetHologramContentPrefab()
    {
        /*Log.Debug("bwaa,.");
        SpriteRenderer spriteRenderer = hologramContentPrefab.GetComponent<SpriteRenderer>();
        if (messageID != -1)
        {
            Log.Debug("bwbwww");
            spriteRenderer.sprite = DroneRepairBeacon.droneIndicatorSprites[messageID];
            spriteRenderer.SetMaterial(DroneRepairBeacon.droneIndicatorMaterials[messageID]);
        }
        else
        {
            Log.Debug("message id -1 ");
            spriteRenderer.gameObject.SetActive(false);
        }*/

        return hologramContentPrefab;
    }

    public int messageID = -1;

    private GameObject hologramContentPrefab
    {
        get
        {
            if (!field)
            {
                field = Instantiate(DroneRepairBeacon.DroneIndicatorVFX);

                Log.Debug("spawning new !" + messageID);
                if (messageID != -1)
                {
                    SpriteRenderer spriteRenderer = field.GetComponent<SpriteRenderer>();
                    spriteRenderer.sprite = DroneRepairBeacon.droneIndicatorSprites[messageID];
                    spriteRenderer.SetMaterial(DroneRepairBeacon.droneIndicatorMaterials[messageID]);
                }


                // if (NetworkServer.active)
                // {
                //     NetworkServer.Spawn(field);
                // }
            }

            Log.Debug("getting prefab !");

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

    public class SyncBaseStats : INetMessage
    {
        NetworkInstanceId bodyNetId;
        int messageID;

        public SyncBaseStats(NetworkInstanceId netId, int num)
        {
            bodyNetId = netId;
            messageID = num;
        }

        public SyncBaseStats()
        {

        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(bodyNetId);
            writer.Write(messageID);
        }

        public void Deserialize(NetworkReader reader)
        {
            bodyNetId = reader.ReadNetworkId();
            messageID = reader.ReadInt32();
        }

        public void OnReceived()
        {
            if (NetworkServer.active)
            {
                return;
            }

            GameObject bodyObject = Util.FindNetworkObject(bodyNetId);
            if (!bodyObject)
            {
                Log.Warning(
                    $"{typeof(SyncBaseStats).FullName}: Could not retrieve GameObject with network ID {bodyNetId}");
            }

            Log.Debug($"{bodyObject}");
            deadDroneTracker droneTracker = bodyObject.GetComponent<deadDroneTracker>();
            if (!droneTracker)
            {
                Log.Warning(
                    $"{typeof(SyncBaseStats).FullName}: Retrieved GameObject {bodyObject} but the GameObject does not have a deadDroneTracker");
                return;
            }

            droneTracker.messageID = messageID;
        }
    }
}