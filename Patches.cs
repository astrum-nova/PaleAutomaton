using HarmonyLib;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

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
    [HarmonyPatch(typeof(SpawnObjectFromGlobalPool), nameof(SpawnObjectFromGlobalPool.OnEnter))]
    [HarmonyPostfix]
    private static void SpawnObjectFromGlobalPool_OnEnter_Postfix(SpawnObjectFromGlobalPool __instance)
    {
        if (!PaleAutomatonPlugin.controlFsm) return;
        if (!__instance.storeObject.Value) return;
        var go = __instance.storeObject.Value;
        if (go == null) return;
        var spawned = go.transform;
        if (spawned.name.StartsWith("Song Knight CrossSlash"))
        {
            spawned.localScale = new Vector3(2.50f, 2.50f, 1.00f);
            Helpers.MakeProjectileRenderAboveWalls(__instance.storeObject.Value);
        } else if (spawned.name.StartsWith("Song Knight Projectile"))
        {
            spawned.localScale = new Vector3(2.25f, 2.20f, 1.00f);
            __instance.storeObject.Value.GetComponent<Collider2D>().isTrigger = true;
            Helpers.MakeProjectileIgnoreEnvironment(__instance.storeObject.Value);
            Helpers.RemoveProjectileWallEvents(__instance.storeObject.Value);
            Helpers.MakeProjectileRenderAboveWalls(__instance.storeObject.Value);
        }
    }
}