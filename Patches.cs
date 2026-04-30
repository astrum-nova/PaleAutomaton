using System;
using GlobalEnums;
using HarmonyLib;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using Object = UnityEngine.Object;

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
        if (!PaleAutomatonPlugin.bossScene) return;
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
            Object.Destroy(go);
            PaleAutomatonPlugin.Instance.StartCoroutine(CustomBehaviour.SpawnWindSlash());
        }
    }
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.FreezeMoment), typeof(FreezeMomentTypes), typeof(Action))]
    private static bool GameManager_FreezeMoment(GameManager __instance, FreezeMomentTypes type, Action onFinish)
    {
        return type != FreezeMomentTypes.NailClashEffect;
    }
    [HarmonyPrefix]
    [HarmonyPatch(typeof(HealthManager), nameof(HealthManager.Hit))]
    private static void HealthManager_Hit(HealthManager __instance, ref HitInstance hitInstance)
    {
        if (!PaleAutomatonPlugin.bossScene) return;
        if (!StateData.IsInParryableState()) return;
        hitInstance.DamageDealt = 0;
        PaleAutomatonPlugin.Instance.StartCoroutine(PaleAutomatonPlugin.damageHero.NailClash(0, "Nail Attack", PaleAutomatonPlugin.songKnight.transform.position));
        GameManager.instance.FreezeMoment(FreezeMomentTypes.NailClashEffect);
    }
}