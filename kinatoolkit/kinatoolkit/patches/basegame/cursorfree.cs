using BepInEx.Configuration;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace kinatoolkit.patches.basegame;

public class cursorfree : PatchBase<cursorfree>
{
    private static GameObject cursorFree;
    
    public override void Init()
    {
        applyHooks();
    }

    private void applyHooks()
    {
        if (enabled.Value)
        {
            RoR2Application.onUpdate += OnUpdate;
        }
        else
        {
            RoR2Application.onUpdate -= OnUpdate;
        }
    }

    private void OnUpdate()
    {
        if (!enabled.Value) return;

        if (!Input.GetKeyDown(cursorFreeEnabled.Value.MainKey)) return; // backmouse button ,.
        
        if (cursorFree)
        {
            Object.Destroy(cursorFree);
        }
        else
        {
            cursorFree = new GameObject("CursorFree");
            MPEventSystemProvider eventProvider = cursorFree.AddComponent<MPEventSystemProvider>();
            cursorFree.AddComponent<MPEventSystemLocator>().eventSystemProvider = cursorFree.GetComponent<MPEventSystemProvider>();
            eventProvider.eventSystem = MPEventSystemManager.kbmEventSystem;
            cursorFree.AddComponent<CursorOpener>();
        }
    }
    
    public override void Config(ConfigFile config)
    {
        enabled = config.Bind("kinaToolkit - free cursor",
            "Enable hooks for free cursor.",
            true,
            "");
        Utils.CheckboxConfig(enabled);
        enabled.SettingChanged += (_, _) => { applyHooks(); };
        
        cursorFreeEnabled = config.Bind("kinaToolkit - free cursor",
            "Keybind to free the cursor.",
            new KeyboardShortcut(KeyCode.Mouse3),
            "Spawns a gameobject to free the cursor, hitting again kills gameobject and unfrees the cursor.");
        Utils.KeyboardConfig(cursorFreeEnabled);
    }

    private ConfigEntry<KeyboardShortcut> cursorFreeEnabled;
    private ConfigEntry<bool> enabled;
}