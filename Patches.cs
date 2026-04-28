using HarmonyLib;

namespace PaleAutomaton;

[HarmonyPatch]
public static class Patches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.BeginSceneTransition))]
    public static void GameManager_BeginSceneTransition_Prefix(GameManager __instance, ref GameManager.SceneLoadInfo info)
    {
        if (info.SceneName != "Hang_17b") return;
        info.SceneName = "Arborium_11";
        info.EntryGateName = "left1";
        info.PreventCameraFadeOut = true;
        GameCameras.instance.cameraFadeFSM.SetState("Scene Fade Out Instant");
    }
}