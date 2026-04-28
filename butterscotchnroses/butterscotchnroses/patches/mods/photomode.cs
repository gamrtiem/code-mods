using System;
using BNR.patches;
using BepInEx.Configuration;
using HarmonyLib;
using PhotoMode;
using Rewired;
using RiskOfOptions;
using RiskOfOptions.Options;
using RoR2;
using RoR2.UI;
using RoR2.UI.SkinControllers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BNR;

public class photomode : PatchBase<photomode>
{
	public override string chainLoaderKey => "com.cwmlolzlz.photomode";
    
	[HarmonyPatch]
	public class PhotomodeChanges
	{
		private static bool timeStop;
		public static float prevTimeScale = -1;
		[HarmonyPatch(typeof(PhotoModeController), "Update")]
		[HarmonyPrefix]
		public static bool UpdatePrefix(PhotoModeController __instance)
		{
			if (!noConsoleCamMovement.Value) return true;
            UserProfile userProfile = __instance.cameraRigController.localUserViewer.userProfile;
			Player inputPlayer = __instance.cameraRigController.localUserViewer.inputPlayer;
			if (inputPlayer.GetButton(25))
			{
				__instance.ExitPhotoMode();
				return false;
			}

			if (timeStop)
			{
				prevTimeScale = Time.timeScale;
				Time.timeScale = 0;
				timeStop = false;
			}
			
			if (RoR2.UI.ConsoleWindow.instance?.gameObject)
			{
				return false;
			}
			float mouseLookSensitivity = userProfile.mouseLookSensitivity;
			float mouseLookScaleX = userProfile.mouseLookScaleX;
			float mouseLookScaleY = userProfile.mouseLookScaleY;
			float axis = inputPlayer.GetAxis(23);
			float axis2 = inputPlayer.GetAxis(24);
			float num = 1f;
			if (__instance.gamepad)
			{
				num = 10f;
			}
			if ((__instance.gamepad && inputPlayer.GetButton(9)) || Input.GetMouseButton(1))
			{
				__instance.cameraState.fov = Mathf.Clamp(__instance.Camera.fieldOfView + mouseLookSensitivity * Time.unscaledDeltaTime * axis2 * num, 4f, 120f);
			}
			if ((__instance.gamepad && inputPlayer.GetButton(10)) || Input.GetMouseButton(2))
			{
				__instance.cameraState.roll += (0f - mouseLookScaleX) * mouseLookSensitivity * Time.unscaledDeltaTime * axis * num;
			}
			else
			{
				float value = mouseLookScaleX * mouseLookSensitivity * Time.unscaledDeltaTime * axis * num;
				float value2 = mouseLookScaleY * mouseLookSensitivity * Time.unscaledDeltaTime * axis2 * num;
				__instance.ConditionalNegate(ref value, userProfile.mouseLookInvertX);
				__instance.ConditionalNegate(ref value2, userProfile.mouseLookInvertY);
				float f = __instance.cameraState.roll * (MathF.PI / 180f);
				__instance.cameraState.yaw += __instance.cameraState.fov * (value * Mathf.Cos(f) - value2 * Mathf.Sin(f));
				__instance.cameraState.pitch += __instance.cameraState.fov * ((0f - value2) * Mathf.Cos(f) - value * Mathf.Sin(f));
				__instance.cameraState.pitch = Mathf.Clamp(__instance.cameraState.pitch, -89f, 89f);
			}
			Vector3 vector = new Vector3(inputPlayer.GetAxis(0) * num, 0f, inputPlayer.GetAxis(1) * num);
			if ((__instance.gamepad && inputPlayer.GetButton(7)) || Input.GetKey(KeyCode.Q))
			{
				vector.y -= 1f;
			}
			if ((__instance.gamepad && inputPlayer.GetButton(8)) || Input.GetKey(KeyCode.E))
			{
				vector.y += 1f;
			}
			vector.Normalize();
			if (inputPlayer.GetButton("Sprint"))
			{
				vector *= PhotoModeController.cameraSprintMultiplier;
			}
			if (Input.GetKey(KeyCode.LeftControl))
			{
				vector *= PhotoModeController.cameraSlowMultiplier;
			}
			__instance.cameraState.position += __instance.cameraState.Rotation * vector * Time.unscaledDeltaTime * PhotoModeController.cameraSpeed;
			__instance.Camera.transform.position = __instance.cameraState.position;
			Quaternion quaternion = __instance.cameraState.Rotation;
			if ((double)Mathf.Abs(quaternion.eulerAngles.z) < 2.0)
			{
				quaternion = quaternion.WithEulerAngles(null, null, 0f);
			}
			__instance.Camera.transform.rotation = quaternion;
			__instance.Camera.fieldOfView = __instance.cameraState.fov;
			return false;
        }
		
		[HarmonyPatch(typeof(PhotoModeController), "SetupPhotoModeHUD")]
		[HarmonyPostfix]
		public static void PhotomodePrefix(PhotoModeController __instance)
		{
			timeStop = true;
			Log.Debug($"enter photo mode !2 {prevTimeScale}");
		}
		
		[HarmonyPatch(typeof(PhotoModeController), "ExitPhotoMode")]
		[HarmonyPostfix]
		public static void PhotomodePost(PhotoModeController __instance)
		{
			if (!keepPreviousTimescale.Value) return;
			Time.timeScale = prevTimeScale;
			Log.Debug($"leave mode !2 {Time.timeScale}");
		}

		[HarmonyPatch(typeof(PhotoMode.PhotoModePlugin), "SetupPhotoModeButton")]
		[HarmonyPostfix]
		public static bool PhotomodePostButton(PhotoModePlugin __instance, PauseScreenController pauseScreenController)
		{
			GameObject gameObject = pauseScreenController.GetComponentInChildren<ButtonSkinController>()?.gameObject;
			if (gameObject == null) return false;
			GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject, gameObject.transform.parent);
			if (gameObject2 == null) return false;
			gameObject2.name = "GenericMenuButton (Photo mode)";
			gameObject2.SetActive(value: true);
			ButtonSkinController component = gameObject2.GetComponent<ButtonSkinController>();
			if (component == null) return false;
			component.GetComponent<LanguageTextMeshController>().token = "Photo mode";
			HGButton component2 = gameObject2.GetComponent<HGButton>();
			if (component2 == null) return false;
			component2.interactable = __instance.cameraRigController.localUserViewer != null;
			component2.onClick.AddListener(delegate
			{
				GameObject gameObject3 = new GameObject("PhotoModeController");
				PhotoModeController photoModeController = gameObject3.AddComponent<PhotoModeController>();
				photoModeController.EnterPhotoMode(pauseScreenController, __instance.cameraRigController);
			});
			gameObject2.transform.SetSiblingIndex(PhotoModePlugin.buttonPlacement.Value);
			return false;
		}

	}

    public override void Init()
    {
        if (!enabled.Value) return;
        
        butterscotchnroses.harmony.CreateClassProcessor(typeof(PhotomodeChanges)).Patch();
    }
    
    public override void Config(ConfigFile config)
    {
	    enabled = config.Bind("Mods - photomode",
		    "enable patches for photomode",
		    true,
		    "");
	    Utils.CheckboxConfig(enabled);
	    
	    noConsoleCamMovement = config.Bind("Mods - photomode",
		    "disable cam movement on console open",
		    true,
		    "doesnt move the camera around when you have the console open ,,. helpful when testing material edits with runtime material inspector @!!!!");
	    Utils.CheckboxConfig(noConsoleCamMovement);
	    
	    keepPreviousTimescale = config.Bind("Mods - photomode",
		    "keep previous timescale exiting photomode",
		    true,
		    "like !! if your time_scale was 0.3 going in ,.,. 0.3 going out !! instead of resetting back to 1 ,,.");
	    Utils.CheckboxConfig(keepPreviousTimescale);
    }
    
    private static ConfigEntry<bool> enabled;
    private static ConfigEntry<bool> noConsoleCamMovement;
    private static ConfigEntry<bool> keepPreviousTimescale;
}