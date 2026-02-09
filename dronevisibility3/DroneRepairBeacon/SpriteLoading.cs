using System.IO;
using System.Linq;
using BepInEx;
using UnityEngine;
using static DroneRepairBeacon.DroneRepairBeacon;

namespace DroneRepairBeacon;

public class SpriteLoading
{
    public static void ChangeSprites()
    {
        if (droneSprites.Value != "")
        {
            foreach (string spriteName in droneSprites.Value.Split(","))
            {
                if (assetbundle.Contains(spriteName.Trim()))
                {
                    droneIndicatorSprites.Add(assetbundle.LoadAsset<Sprite>(spriteName.Trim()));
                }
                else
                {
                    Log.Error($"couldnt find sprite {spriteName.Trim()} in asset bundle !!");
                }
            }
        }
        

        if (customDroneSprites.Value)
        {
            string dir = Path.Combine(Path.GetDirectoryName(Paths.ConfigPath)!, "config", "DroneBeacon");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            
            string specificDir = Path.Combine(dir, "Specific");
            if (!Directory.Exists(specificDir))
            {
                Directory.CreateDirectory(specificDir);
            }

            string [] fileEntries = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
            foreach (string fileName in fileEntries)
            {
                string file = Path.Combine(dir, fileName.Trim());
                
                if (!file.EndsWith(".png"))
                {
                    Log.Debug($"file {file} does not end in png ,..,,. skipping !!");
                    continue;
                }
                
                Sprite loadedSprite = LoadSpriteFromFile(file);
                if(loadedSprite != null)
                {
                    loadedSprite.name = fileName.Replace(".png", "").Split("\\").Last();
                    if (file.Contains(specificDir))
                    {
                        Log.Debug($"specific sprite {loadedSprite}!!!!!!");
                        specificDroneIndicatorSprites.Add(loadedSprite);
                    }
                    else
                    {
                        Log.Debug($"regualr sprite {loadedSprite}!!!!!!");
                        droneIndicatorSprites.Add(loadedSprite);
                    }
                }
                else
                {
                    Log.Error($"couldnt find sprite {file.Trim()} in files !!");
                }
            }
        }
        
        droneIndicatorMaterials = new Material[droneIndicatorSprites.Count];
        for (int i = 0; i < droneIndicatorMaterials.Length; i++)
        {
            droneIndicatorMaterials[i] = Object.Instantiate(baseMat);
            droneIndicatorMaterials[i].name = droneIndicatorSprites[i].name;
            droneIndicatorMaterials[i].SetTexture("_EmTex", droneIndicatorSprites[i].texture);
        }
        
        specificDroneIndicatorMaterials = new Material[specificDroneIndicatorSprites.Count];
        for (int i = 0; i < specificDroneIndicatorMaterials.Length; i++)
        {
            specificDroneIndicatorMaterials[i] = Object.Instantiate(baseMat);
            specificDroneIndicatorMaterials[i].name = specificDroneIndicatorSprites[i].name;
            specificDroneIndicatorMaterials[i].SetTexture("_EmTex", specificDroneIndicatorSprites[i].texture);
        }
    }
    
    private static Sprite LoadSpriteFromFile(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
        
        int sprite_width = 100;
        int sprite_height = 100;
        byte[] bytes = File.ReadAllBytes(path);
        Texture2D texture = new(sprite_width, sprite_height, TextureFormat.RGB24, false);
        texture.LoadImage(bytes);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        return sprite;
    }
}