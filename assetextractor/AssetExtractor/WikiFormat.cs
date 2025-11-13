namespace AssetExtractor;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;
using Path = System.IO.Path;

public static partial class WikiFormat
    {
        private const string WIKI_OUTPUT_FOLDER = "wiki";
        private const string WIKI_OUTPUT_ITEM = "Items.txt";
        private const string WIKI_OUTPUT_EQUIPMENT = "Equipments.txt";
        private const string WIKI_OUTPUT_SURVIVORS = "Survivors.txt";
        private const string WIKI_OUTPUT_SKILLS = "Skills.txt";
        private const string WIKI_OUTPUT_CHALLENGES = "Challenges.txt";
        private const string WIKI_OUTPUT_BODIES = "Bodies.txt";
        private const string WIKI_OUTPUT_BUFFS = "Buffs.txt";
        private const string WIKI_OUTPUT_STAGES = "Stages.txt";
        private const string WIKI_OUTPUT_LORE = "Lore.txt";
        
        public static string WikiOutputPath = Path.Combine(Path.GetDirectoryName(AssetExtractor.Instance.Info.Location) ?? throw new InvalidOperationException(), WIKI_OUTPUT_FOLDER);
        public static string WikiModname = "";
        public static bool WikiTryGetProcs = false;
        public static bool WikiAppend = false;
        public static List<string> loredefs = [];
        public static bool loadedScene = false;

        private static readonly Dictionary<string, string> FormatR2ToWiki = new Dictionary<string, string>()
        {
            { "</style>", "}}"},
            { "<style=cStack>", "{{Stack|" },
            { "<style=cIsDamage>", "{{Color|d|" },
            { "<style=cIsHealing>", "{{Color|h|" },
            { "<style=cIsUtility>", "{{Color|u|" },
            { "<style=cIsHealth>", "{{Color|hp|" },
            { "<style=cDeath>", "{{Color|hp|" },
            { "<style=cIsVoid>", "{{Color|v|" },
            { "<style=cIsLunar>", "{{Color|lunar|" },
            { "<style=cHumanObjective>", "{{Color|human|"},
            { "<style=cShrine>", "{{Color|boss|" }, // idk about this one
        };

        public static void exportTexture(Texture texture, String path)
        {
            //RenderTexture tmp = new RenderTexture(texture.width, texture.height, 32);
            var tmp = RenderTexture.GetTemporary(texture.width, texture.height, 32);
            tmp.name = "Whatever";
            tmp.enableRandomWrite = true;
            tmp.Create();

            // Blit the pixels on texture to the RenderTexture
            Graphics.Blit(texture, tmp);
                
            // Backup the currently set RenderTexture
            RenderTexture previous = RenderTexture.active;
                
            // Set the current RenderTexture to the temporary one we created
            RenderTexture.active = tmp;
                
            // Create a new readable Texture2D to copy the pixels to it
            Texture2D myTexture2D = new Texture2D(texture.width, texture.height);
                
            // Copy the pixels from the RenderTexture to the new Texture
            myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            myTexture2D.Apply();
                
            // Reset the active RenderTexture
            RenderTexture.active = previous;
                
            // Release the temporary RenderTexture
            RenderTexture.ReleaseTemporary(tmp);
            File.WriteAllBytes(path, myTexture2D.EncodeToPNG());
            
            Object.Destroy(myTexture2D);
        }
        
        public static void exportTexture(Sprite sprite, String path)
        {
            //RenderTexture tmp = new RenderTexture(sprite.texture.width, sprite.texture.height, 32);
            var tmp = RenderTexture.GetTemporary(sprite.texture.width, sprite.texture.height, 32);
            tmp.name = "Whatever";
            tmp.enableRandomWrite = true;
            tmp.Create();

            // Blit the pixels on texture to the RenderTexture
            Graphics.Blit(sprite.texture, tmp);
                
            // Backup the currently set RenderTexture
            RenderTexture previous = RenderTexture.active;
                
            // Set the current RenderTexture to the temporary one we created
            RenderTexture.active = tmp;
                
            // Create a new readable Texture2D to copy the pixels to it
            Texture2D myTexture2D = new Texture2D(sprite.texture.width, sprite.texture.height);
                
            // Copy the pixels from the RenderTexture to the new Texture
            myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            myTexture2D.Apply();
                
            // Reset the active RenderTexture
            RenderTexture.active = previous;
                
            // Release the temporary RenderTexture
            RenderTexture.ReleaseTemporary(tmp);
            
            //convert sprite to texture
            var croppedTexture = new Texture2D( sprite.texture.width, sprite.texture.height );
            var pixels = myTexture2D.GetPixels(  (int)sprite.textureRect.x, 
                (int)sprite.textureRect.y, 
                (int)sprite.textureRect.width, 
                (int)sprite.textureRect.height );
            
            croppedTexture.SetPixels( pixels );
            croppedTexture.Apply();
            
            File.WriteAllBytes(path, croppedTexture.EncodeToPNG());
            
            Object.Destroy(myTexture2D);
        }

        public static string acronymHelper(string expansion)
        {
            string[] expansionName = expansion.Split(" ");
            string acronym = "";
            foreach (var word in expansionName)
            {
                acronym += word.ToUpper()[0];
            }
            return acronym;
        }
    }