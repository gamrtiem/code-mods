using System.Collections;
using System.IO;
using System.Reflection;
using BepInEx;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Analytics;
using UnityEngine.SceneManagement;
using CharacterModel = On.RoR2.CharacterModel;

namespace tetoify
{
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    
    public class tetoify : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "icebro";
        public const string PluginName = "tetoify";
        public const string PluginVersion = "1.0.0";

        private static Texture2D teto;

        public void Awake()
        {
            Log.Init(Logger);
            
            teto = new Texture2D(2, 2);
            teto.LoadImage(File.ReadAllBytes(Assembly.GetExecutingAssembly().Location.Replace("tetoify.dll", "teto.jpg")));
            
            On.RoR2.CharacterModel.UpdateMaterials += CharacterModelOnUpdateMaterials;
            SceneManager.activeSceneChanged += SceneManagerOnactiveSceneChanged;
        }

        private void SceneManagerOnactiveSceneChanged(Scene arg0, Scene arg1)
        {
            this.StartCoroutine(MyOats());
        }

        private void CharacterModelOnUpdateMaterials(CharacterModel.orig_UpdateMaterials orig, RoR2.CharacterModel self)
        {
            orig(self);
            OatifyObject(self.gameObject);
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.F2)) return;
            GameObject[] allObjects = FindObjectsOfType<GameObject>() ;
            foreach (var obj in allObjects)
            {
                try
                {
                    Logger.LogDebug(obj + " tetoifying ");
                    if (obj == null) continue;
                    if (obj.GetComponentsInChildren<Renderer>(true) == null) continue;
                    Renderer[] componentsInChildren = obj.GetComponentsInChildren<Renderer>(true);
                    foreach (var t in componentsInChildren)
                    {
                        if (t == null) continue;
                        bool flag;
                        if (t)
                        {
                            Renderer renderer = t;
                            flag = (renderer is MeshRenderer || renderer is SkinnedMeshRenderer);
                        }
                        else
                        {
                            flag = false;
                        }

                        if (!flag) continue;
                        if (t.sharedMaterials == null) continue;
                        foreach (Material material in t.sharedMaterials)
                        {
                            if (material == null) continue;
                            if (material.GetTexturePropertyNames() == null) continue;
                            foreach (string text in material.GetTexturePropertyNames())
                            {
                                if (text == null) continue;
                                string text2 = text.ToLower();
                                bool flag3 = text2.Contains("emtex") || text2.Contains("fres") ||
                                             text2.Contains("norm");
                                if (flag3) continue;
                                bool flag4 = !material.GetTexture(text);
                                if (flag4) continue;
                                try
                                {
                                    material.SetTexture(text, teto);
                                }
                                catch
                                {
                                    Logger.LogDebug("ohg no !!");
                                }
                            }
                        }
                    }
                }
                catch
                {
                    
                }
                
            }
        }
        private void OatifyMaterial(Material mat)
        {
            string[] texturePropertyNames = mat.GetTexturePropertyNames();
            foreach (string text in texturePropertyNames)
            {
                try
                {
                    mat.SetTexture(text, teto);
                }
                catch
                {
                    Logger.LogDebug("failed to teto skybox");
                }
            }
        }

        private IEnumerator MyOats()
        {
            yield return (object)new WaitForEndOfFrame();
            yield return (object)new WaitForEndOfFrame();
            yield return (object)new WaitForEndOfFrame();
            Scene activeScene = SceneManager.GetActiveScene();
            GameObject[] rootGameObjects = activeScene.GetRootGameObjects();
            foreach (GameObject obj in rootGameObjects)
            {
                OatifyObject(obj);
            }
            OatifyMaterial(RenderSettings.skybox);
        }
        private void OatifyObject(GameObject obj)
        {
            try
            {
                Logger.LogDebug(obj + " tetoifying ");
                if (obj == null) return;
                if (obj.GetComponentsInChildren<Renderer>(true) == null) return;
                Renderer[] componentsInChildren = obj.GetComponentsInChildren<Renderer>(true);
                foreach (var t in componentsInChildren)
                {
                    if (t == null) continue;
                    bool flag;
                    if (t)
                    {
                        Renderer renderer = t;
                        flag = (renderer is MeshRenderer || renderer is SkinnedMeshRenderer);
                    }
                    else
                    {
                        flag = false;
                    }

                    if (!flag) continue;
                    if (t.sharedMaterials == null) continue;
                    foreach (Material material in t.sharedMaterials)
                    {
                        if (material == null) continue;
                        if (material.GetTexturePropertyNames() == null) continue;
                        foreach (string text in material.GetTexturePropertyNames())
                        {
                            if (text == null) continue;
                            string text2 = text.ToLower();
                            bool flag3 = text2.Contains("emtex") || text2.Contains("fres") ||
                                         text2.Contains("norm");
                            if (flag3) continue;
                            bool flag4 = !material.GetTexture(text);
                            if (flag4) continue;
                            try
                            {
                                material.SetTexture(text, teto);
                                material.color = Color.red;
                                material.mainTexture = teto;
                                //material.
                            }
                            catch
                            {
                                Logger.LogDebug("ohg no !!");
                            }
                        }
                    }
                }
                
            }
            catch
            {
                Logger.LogError(obj.name + " stupoud");
            }
        }
    }
}
