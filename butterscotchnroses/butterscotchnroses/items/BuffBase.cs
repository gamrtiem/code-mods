using System;
using R2API;
using RoR2;
using UnityEngine;

namespace ExamplePlugin.items
{
    public abstract class BuffBase<T> : BuffBase where T : BuffBase<T>
    {
        //This, which you will see on all the -base classes, will allow both you and other modders to enter through any class with this to access internal fields/properties/etc as if they were a member inheriting this -Base too from this class.
        public static T instance { get; private set; }

        public BuffBase()
        {
            if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting BuffBase was instantiated twice");
            instance = this as T;
        }
    }

    public abstract class BuffBase
    {
        internal abstract string name { get; }
        internal abstract Sprite icon { get; }
        internal abstract Color color { get; }
        internal abstract bool canStack { get; }
        internal abstract bool isDebuff { get; }
        internal abstract EliteDef eliteDef { get; }
        public BuffDef BuffDef { get; set; }


        internal BuffDef AddBuff()
        {
            BuffDef buffDef = ScriptableObject.CreateInstance<BuffDef>();
            buffDef.name = this.name;
            buffDef.iconSprite = this.icon;
            buffDef.buffColor = this.color;
            buffDef.canStack = this.canStack;
            buffDef.isDebuff = this.isDebuff;
            buffDef.eliteDef = this.eliteDef;
            ContentAddition.AddBuffDef(buffDef);
            BuffDef = buffDef;
            return buffDef;
        }
    }
}