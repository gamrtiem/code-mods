using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BNR.patches
{

    // The directly below is entirely from TILER2 API (by ThinkInvis) specifically the Item module. Utilized to implement instancing for classes.
    // TILER2 API can be found at the following places:
    // https://github.com/ThinkInvis/RoR2-TILER2
    // https://thunderstore.io/package/ThinkInvis/TILER2/

    public abstract class PatchBase<T> : PatchBase where T : PatchBase<T>
    {
        //This, which you will see on all the -base classes, will allow both you and other modders to enter through any class with this to access internal fields/properties/etc as if they were a member inheriting this -Base too from this class.
        public static T instance { get; private set; }

        public PatchBase()
        {
            if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting ItemBase was instantiated twice");
            instance = this as T;
        }
    }
    public abstract class PatchBase
    {
        
        //public abstract ItemDef ItemDef { get; }
        public bool AIBlacklisted { get; set; }
        public string ItemName;

        /// <summary>
        /// This method structures your code execution of this class. An example implementation inside of it would be:
        /// <para>CreateConfig(config);</para>
        /// <para>CreateLang();</para>
        /// <para>CreateItem();</para>
        /// <para>Hooks();</para>
        /// <para>This ensures that these execute in this order, one after another, and is useful for having things available to be used in later methods.</para>
        /// <para>P.S. CreateItemDisplayRules(); does not have to be called in this, as it already gets called in CreateItem();</para>
        /// </summary>
        /// <param name="config">The config file that will be passed into this from the main class.</param>
        public abstract void Init(Harmony harmony);

        public abstract void Config(ConfigFile config);

        protected virtual void CreateLang()
        {
            // LanguageAPI.Add(ItemDef.nameToken, "bwa name");
            // LanguageAPI.Add(ItemDef.pickupToken, "bwah pickup");
            // LanguageAPI.Add(ItemDef.descriptionToken, "bwa desc");
            // LanguageAPI.Add(ItemDef.loreToken, "bwa lore");
        }

        //public abstract ItemDisplayRuleDict CreateItemDisplayRules();
        protected void CreateItem()
        {
            
        }
        
        public void RegItemName()
        {
            
        }

        public virtual void Hooks() { }
    }
}
