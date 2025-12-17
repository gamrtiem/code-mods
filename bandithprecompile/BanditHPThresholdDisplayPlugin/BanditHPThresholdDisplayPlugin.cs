

// BanditHPThresholdDisplay.BanditHPThresholdDisplayPlugin

using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.UI;
using RiskOfOptions;
using Path = System.IO.Path;

namespace BanditHPThresholdDisplayPlugin
{
	[BepInPlugin("com.themysticsword.bandithpthresholddisplay", "BanditHPThresholdDisplay", "1.0.3")]
	public class BanditHPThresholdDisplayPlugin : BaseUnityPlugin
	{
		public class BanditHPThresholdDisplayHealthBarHelper : MonoBehaviour
		{
			public HealthBar.BarInfo hpThresholdBarInfo;

			public HealthBar.BarInfo hpThresholdCritBarInfo;

			public HealthBarStyle.BarStyle hpThresholdBarStyle;

			public HealthBarStyle.BarStyle hpThresholdCritBarStyle;

			public static BanditHPThresholdDisplayHealthBarHelper Get(HealthBar self)
			{
				BanditHPThresholdDisplayHealthBarHelper banditHPThresholdDisplayHealthBarHelper = self.GetComponent<BanditHPThresholdDisplayHealthBarHelper>();
				if (!banditHPThresholdDisplayHealthBarHelper)
				{
					banditHPThresholdDisplayHealthBarHelper = self.gameObject.AddComponent<BanditHPThresholdDisplayHealthBarHelper>();
				}
				return banditHPThresholdDisplayHealthBarHelper;
			}

			public void Awake()
			{
				hpThresholdBarStyle = hpThresholdBarStyleDefault;
				hpThresholdCritBarStyle = hpThresholdCritBarStyleDefault;
			}
		}

		public class BanditHPThresholdDisplayModelHelper : MonoBehaviour
		{
			public CharacterModel characterModel;

			public GameObject enemyKillableIcon;

			public GameObject enemyCritKillableIcon;

			public static BanditHPThresholdDisplayModelHelper Get(CharacterModel self)
			{
				BanditHPThresholdDisplayModelHelper banditHPThresholdDisplayModelHelper = self.GetComponent<BanditHPThresholdDisplayModelHelper>();
				if (!banditHPThresholdDisplayModelHelper)
				{
					banditHPThresholdDisplayModelHelper = self.gameObject.AddComponent<BanditHPThresholdDisplayModelHelper>();
				}
				return banditHPThresholdDisplayModelHelper;
			}

			public void Awake()
			{
				characterModel = GetComponent<CharacterModel>();
			}

			public void OnDestroy()
			{
				if (enemyKillableIcon)
				{
					Destroy(enemyKillableIcon);
				}
				if (enemyCritKillableIcon)
				{
					Destroy(enemyCritKillableIcon);
				}
			}

			public void UpdateForCamera(CameraRigController cameraRigController)
			{
				if (characterModel.body && characterModel.body.GetVisibilityLevel(cameraRigController.targetTeamIndex) != 0 && characterModel.body.healthComponent && characterModel.body.healthComponent.alive && cameraRigController.targetBody && requiredBodyIndices.Contains(cameraRigController.targetBody.bodyIndex) && TeamManager.IsTeamEnemy(characterModel.body.teamComponent.teamIndex, cameraRigController.targetTeamIndex))
				{
					float num = CalculateThresholdFraction(characterModel.body, cameraRigController.targetBody);
					bool flag = enemyKillableIcon != null;
					bool flag2 = markKillableEnemies.Value && characterModel.body.healthComponent.combinedHealthFraction <= num;
					bool flag3 = flag2;
					if (flag != flag2)
					{
						if (!flag)
						{
							enemyKillableIcon = Instantiate(enemyKillableOverlayPrefab);
						}
						else
						{
							Destroy(enemyKillableIcon);
						}
					}
					else if (flag)
					{
						enemyKillableIcon.transform.position = characterModel.body.corePosition;
					}
					flag = enemyCritKillableIcon != null;
					flag2 = markCritKillableEnemies.Value && !flag3 && characterModel.body.healthComponent.combinedHealthFraction <= num * cameraRigController.targetBody.critMultiplier;
					if (flag != flag2)
					{
						if (!flag)
						{
							enemyCritKillableIcon = Instantiate(enemyCritKillableOverlayPrefab);
						}
						else
						{
							Destroy(enemyCritKillableIcon);
						}
					}
					else if (flag)
					{
						enemyCritKillableIcon.transform.position = characterModel.body.corePosition;
					}
				}
				else
				{
					if (enemyKillableIcon)
					{
						Destroy(enemyKillableIcon);
					}
					if (enemyCritKillableIcon)
					{
						Destroy(enemyCritKillableIcon);
					}
				}
			}
		}

		public const string PluginGUID = "com.themysticsword.bandithpthresholddisplay";

		public const string PluginName = "BanditHPThresholdDisplay";

		public const string PluginVersion = "1.0.3";

		public static AssetBundle assetBundle;

		public static ConfigFile config = new ConfigFile(Paths.ConfigPath + "\\TheMysticSword-BanditHPThresholdDisplay.cfg", true);

		public static List<BodyIndex> requiredBodyIndices = [];

		public static ConfigEntry<float> banditSpecialDamage = config.Bind("Options", "Bandit Special Damage", 600f, "Damage of Bandit's specials (in %). Change this only if one of your mods changes the damage");

		public static ConfigEntry<float> desperadoTokenDamage = config.Bind("Options", "Desperado Token Damage", 10f, "Bonus damage for each Desperado kill (in %). Change this only if one of your mods changes the bonus damage");

		public static ConfigEntry<bool> showBar = config.Bind("Options", "Show Bar", true, "Show a pink bar to indicate the kill threshold");

		public static ConfigEntry<bool> showCritBar = config.Bind("Options", "Show Crit Bar", true, "Show a smaller red bar that is twice as long as the pink bar to indicate the kill threshold on crits");

		public static ConfigEntry<bool> markKillableEnemies = config.Bind("Options", "Mark Killable Enemies", true, "Enemies below the HP threshold get a visual mark");

		public static ConfigEntry<bool> markCritKillableEnemies = config.Bind("Options", "Mark Crit Killable Enemies", true, "Enemies below the crit HP threshold get a smaller red visual mark");
		
		public static ConfigEntry<float> markOpacity = config.Bind("Options", "Mark Opacity", 0.15f, "Change the mark transparency between 0-1");
		public static ConfigEntry<float> markSize = config.Bind("Options", "Mark Scale", 0.75f, "Change the mark size");

		public static HealthBarStyle.BarStyle hpThresholdBarStyleDefault;

		public static HealthBarStyle.BarStyle hpThresholdCritBarStyleDefault;

		public static GameObject enemyKillableOverlayPrefab;

		public static GameObject enemyCritKillableOverlayPrefab;

		public void Awake()
		{
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_0063: Expected O, but got Unknown
			//IL_006b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0075: Expected O, but got Unknown
			assetBundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Info.Location), "bandithpthresholddisplayassetbundle"));
			// if (RiskOfOptionsDependency.enabled)
			// {
			// tried fixing ,.,. realized have no idea what this is trying to do ,..,,.
			// 	RiskOfOptionsDependency.RegisterModInfo("com.themysticsword.bandithpthresholddisplay", "BanditHPThresholdDisplay", "Shows the HP threshold to kill an enemy with the Bandit's special", assetBundle.LoadAsset<Sprite>("Assets/Mod Icons/BanditHPThresholdDisplay.png"));
			// }
			On.RoR2.UI.HealthBar.UpdateBarInfos += HealthBar_UpdateBarInfos;
			On.RoR2.UI.HealthBar.ApplyBars += HealthBar_ApplyBars;
			RoR2Application.onLoad = (Action)Delegate.Combine(RoR2Application.onLoad, new Action(OnGameLoad));
			SceneCamera.onSceneCameraPreRender += SceneCamera_onSceneCameraPreRender;
			HealthBarStyle.BarStyle barStyle = default(HealthBarStyle.BarStyle);
			barStyle.enabled = true;
			barStyle.baseColor = new Color32(232, 138, 235, byte.MaxValue);
			barStyle.sprite = assetBundle.LoadAsset<Sprite>("Assets/BanditHPThresholdDisplay/texHPThresholdBar.png");
			barStyle.imageType = Image.Type.Sliced;
			barStyle.sizeDelta = 10f;
			hpThresholdBarStyleDefault = barStyle;
			barStyle = default(HealthBarStyle.BarStyle);
			barStyle.enabled = true;
			barStyle.baseColor = new Color32(232, 53, 37, byte.MaxValue);
			barStyle.sprite = assetBundle.LoadAsset<Sprite>("Assets/BanditHPThresholdDisplay/texHPThresholdCritBar.png");
			barStyle.imageType = Image.Type.Sliced;
			barStyle.sizeDelta = 4f;
			hpThresholdCritBarStyleDefault = barStyle;
			markOpacity.SettingChanged += (sender, args) =>
			{
				ChangeMaterial(assetBundle.LoadAsset<Material>("Assets/BanditHPThresholdDisplay/matEnemyKillableOverlayShaky.mat"));
				ChangeMaterial(assetBundle.LoadAsset<Material>("Assets/BanditHPThresholdDisplay/matEnemyKillableOverlayShakyCrit.mat"));
				ChangeMaterial(assetBundle.LoadAsset<Material>("Assets/BanditHPThresholdDisplay/matEnemyKillableOverlayShakyBackdrop.mat"));

				void ChangeMaterial(Material material)
				{
					Color color = material.GetColor("_Color");
					color.a = markOpacity.Value;
					material.SetColor("_Color", color);
				}
			};
			markSize.SettingChanged += (sender, args) =>
			{
				ChangeGameObject(enemyKillableOverlayPrefab, 1f);
				ChangeGameObject(enemyCritKillableOverlayPrefab, 0.666f);
				foreach (CharacterModel instances in InstanceTracker.GetInstancesList<CharacterModel>())
				{
					BanditHPThresholdDisplayModelHelper banditHPThresholdDisplayModelHelper = BanditHPThresholdDisplayModelHelper.Get(instances);
					ChangeGameObject(banditHPThresholdDisplayModelHelper.enemyKillableIcon, 1f);
					ChangeGameObject(banditHPThresholdDisplayModelHelper.enemyCritKillableIcon, 0.666f);
				}
				void ChangeGameObject(GameObject gameObject, float relativeScale)
				{
					if ((bool)gameObject)
					{
						Transform transform = gameObject.transform.Find("Holder");
						if ((bool)transform)
						{
							transform.localScale = Vector3.one * markSize.Value * relativeScale;
						}
					}
				}
			};
			enemyKillableOverlayPrefab = assetBundle.LoadAsset<GameObject>("Assets/BanditHPThresholdDisplay/EnemyKillableOverlay.prefab").InstantiateClone("BanditHPThresholdDisplay_EnemyKillableIcon", false);
			enemyCritKillableOverlayPrefab = assetBundle.LoadAsset<GameObject>("Assets/BanditHPThresholdDisplay/EnemyKillableOverlayCrit.prefab").InstantiateClone("BanditHPThresholdDisplay_EnemyCritKillableIcon", false);
		}

		private void SceneCamera_onSceneCameraPreRender(SceneCamera sceneCamera)
		{
			if (!sceneCamera.cameraRigController)
			{
				return;
			}
			foreach (CharacterModel instances in InstanceTracker.GetInstancesList<CharacterModel>())
			{
				BanditHPThresholdDisplayModelHelper.Get(instances).UpdateForCamera(sceneCamera.cameraRigController);
			}
		}

		public void OnGameLoad()
		{
			requiredBodyIndices.Add(BodyCatalog.FindBodyIndex("Bandit2Body"));
		}

		public static float CalculateThresholdFraction(CharacterBody victim, CharacterBody attacker)
		{
			float num = 1f + desperadoTokenDamage.Value / 100f * (float)attacker.GetBuffCount(RoR2Content.Buffs.BanditSkull);
			float num2 = Mathf.Lerp(3f, 1f, victim.healthComponent.combinedHealthFraction);
			float num3 = victim.armor + victim.healthComponent.adaptiveArmorValue;
			float num4 = ((num3 >= 0f) ? (1f - num3 / (num3 + 100f)) : (2f - 100f / (100f - num3)));
			return attacker.damage * (banditSpecialDamage.Value / 100f) * num2 * num * num4 / victim.healthComponent.fullCombinedHealth;
		}

		private void HealthBar_UpdateBarInfos(On.RoR2.UI.HealthBar.orig_UpdateBarInfos orig, HealthBar self)
		{
			orig.Invoke(self);
			BanditHPThresholdDisplayHealthBarHelper banditHPThresholdDisplayHealthBarHelper = BanditHPThresholdDisplayHealthBarHelper.Get(self);
			if ((bool)(UnityEngine.Object)(object)self.source && (bool)(UnityEngine.Object)(object)self.source.body && (bool)(UnityEngine.Object)(object)self.viewerBody && requiredBodyIndices.Contains(self.viewerBody.bodyIndex) && TeamManager.IsTeamEnemy(self.source.body.teamComponent.teamIndex, self.viewerBody.teamComponent.teamIndex))
			{
				float num = CalculateThresholdFraction(self.source.body, self.viewerBody);
				banditHPThresholdDisplayHealthBarHelper.hpThresholdBarInfo.enabled = showBar.Value && num > 0f;
				banditHPThresholdDisplayHealthBarHelper.hpThresholdBarInfo.normalizedXMin = 0f;
				banditHPThresholdDisplayHealthBarHelper.hpThresholdBarInfo.normalizedXMax = Mathf.Min(num, 1f);
				bool flag = num >= 1f;
				ApplyStyle(ref banditHPThresholdDisplayHealthBarHelper.hpThresholdBarInfo, banditHPThresholdDisplayHealthBarHelper.hpThresholdBarStyle);
				banditHPThresholdDisplayHealthBarHelper.hpThresholdCritBarInfo.enabled = showCritBar.Value && !flag && num > 0f;
				banditHPThresholdDisplayHealthBarHelper.hpThresholdCritBarInfo.normalizedXMin = 0f;
				banditHPThresholdDisplayHealthBarHelper.hpThresholdCritBarInfo.normalizedXMax = Mathf.Min(num * self.viewerBody.critMultiplier, 1f);
				ApplyStyle(ref banditHPThresholdDisplayHealthBarHelper.hpThresholdCritBarInfo, banditHPThresholdDisplayHealthBarHelper.hpThresholdCritBarStyle);
			}
			else
			{
				banditHPThresholdDisplayHealthBarHelper.hpThresholdBarInfo.enabled = false;
				ApplyStyle(ref banditHPThresholdDisplayHealthBarHelper.hpThresholdBarInfo, banditHPThresholdDisplayHealthBarHelper.hpThresholdBarStyle);
				banditHPThresholdDisplayHealthBarHelper.hpThresholdCritBarInfo.enabled = false;
				ApplyStyle(ref banditHPThresholdDisplayHealthBarHelper.hpThresholdCritBarInfo, banditHPThresholdDisplayHealthBarHelper.hpThresholdCritBarStyle);
			}
			static void ApplyStyle(ref HealthBar.BarInfo barInfo, HealthBarStyle.BarStyle barStyle)
			{
				barInfo.enabled &= barStyle.enabled;
				barInfo.color = barStyle.baseColor;
				barInfo.sprite = barStyle.sprite;
				barInfo.imageType = barStyle.imageType;
				barInfo.sizeDelta = barStyle.sizeDelta;
			}
		}

		private void HealthBar_ApplyBars(On.RoR2.UI.HealthBar.orig_ApplyBars orig, HealthBar self)
		{
			orig.Invoke(self);
			BanditHPThresholdDisplayHealthBarHelper banditHPThresholdDisplayHealthBarHelper = BanditHPThresholdDisplayHealthBarHelper.Get(self);
			int num = 0;
			if (banditHPThresholdDisplayHealthBarHelper.hpThresholdBarInfo.enabled)
			{
				num++;
			}
			if (banditHPThresholdDisplayHealthBarHelper.hpThresholdCritBarInfo.enabled)
			{
				num++;
			}
			int activeCount = self.barInfoCollection.GetActiveCount();
			self.barAllocator.AllocateElements(activeCount + num);
			int i = activeCount;
			HandleBar(ref banditHPThresholdDisplayHealthBarHelper.hpThresholdCritBarInfo);
			HandleBar(ref banditHPThresholdDisplayHealthBarHelper.hpThresholdBarInfo);
			void HandleBar(ref HealthBar.BarInfo barInfo)
			{
				if (barInfo.enabled)
				{
					Image image = self.barAllocator.elements[i];
					image.type = barInfo.imageType;
					image.sprite = barInfo.sprite;
					image.color = barInfo.color;
					SetRectPosition((RectTransform)image.transform, barInfo.normalizedXMin, barInfo.normalizedXMax, barInfo.sizeDelta);
					i++;
				}
			}
			static void SetRectPosition(RectTransform rectTransform, float xMin, float xMax, float sizeDelta)
			{
				rectTransform.anchorMin = new Vector2(xMin, 0f);
				rectTransform.anchorMax = new Vector2(xMax, 1f);
				rectTransform.anchoredPosition = Vector2.zero;
				rectTransform.sizeDelta = new Vector2(sizeDelta * 0.5f + 1f, sizeDelta + 1f);
			}
		}
	}
}