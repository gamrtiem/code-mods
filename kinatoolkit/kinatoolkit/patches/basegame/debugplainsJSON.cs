using System.Collections.Generic;
using System.IO;
using BepInEx;
using Newtonsoft.Json;

namespace kinatoolkit.patches.basegame;

public class debugplainsJSON
{
    public static JSONedit loadJSON()
    {
        string dir = Path.Combine(Path.GetDirectoryName(Paths.ConfigPath)!, "config", "kinaToolkit");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        string jsonPath = Path.Combine(dir, "debugPlains.json");
        
        using StreamReader r = new StreamReader(jsonPath);
        string json = r.ReadToEnd();
        JSONedit items = JsonConvert.DeserializeObject<JSONedit>(json);
        return items;
    }

    public class JSONedit
    {
        public spawnPos spawnPos { get; set; }
        public IList<Dummy> dummies;
        public IList<Interactables> interactables;
        public IList<commandPickup> commandPickups;
    }
    
    public class spawnPos
    {
        public string tier { get; set; }
        public Position position { get; set; }
        public Rotation rotation { get; set; }
    }

    public class commandPickup
    {
        public string tier { get; set; }
        public Position position { get; set; }
    }

    public class Interactables
    {
        public string interactableCard { get; set; }
        public Position position { get; set; }
        public Rotation rotation { get; set; }
    }

    public class Dummy
    {
        public string masterName { get; set; }
        public Position position { get; set; }
        public Rotation rotation { get; set; }
    }

    public class Position
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
    }

    public class Rotation
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
    }
}