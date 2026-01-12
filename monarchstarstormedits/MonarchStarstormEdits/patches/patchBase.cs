using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace MonarchStarstormEdits.patches
{
    public abstract class PatchBase<T> : PatchBase where T : PatchBase<T>
    {
        public static T instance { get; private set; }

        public PatchBase()
        {
            if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting ItemBase was instantiated twice");
            instance = this as T;
        }
    }
    public abstract class PatchBase
    {
        public abstract void Init(Harmony harmony);

        public abstract void Config(ConfigFile config);
        
        public virtual void Hooks() { }
    }
}