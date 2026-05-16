using System;
using GenericVariableExtension;
using GlobalEnums;
using HarmonyLib;
using HutongGames.PlayMaker.Actions;
using Silksong.FsmUtil;
using TeamCherry.Localization;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PaleAutomaton;

[HarmonyPatch]
public static class Patches
{
    //? Redirect the vanilla boss room 
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
            spawned.name = "CrossSlashSetup";
            if (!CustomBehaviour.crossSlashSetup)
            {
                CustomBehaviour.crossSlashSetup = Object.Instantiate(go);
                CustomBehaviour.crossSlashSetup.SetActive(false);
                CustomBehaviour.crossSlashSetup.name = "CrossSlashSetup";
                var csFsm = CustomBehaviour.crossSlashSetup.GetComponent<PlayMakerFSM>();
                csFsm.GetState("Recycle")!.RemoveActionsOfType<RecycleSelf>();
            }
        } else if (spawned.name.StartsWith("Song Knight Projectile"))
        {
            PaleAutomatonPlugin.Instance.StartCoroutine(CustomBehaviour.SpawnWindSlash());
            Object.Destroy(go);
        } else if (go.name.StartsWith("bind_bell_appear"))
        {
            if (!tookBellBind)
            {
                tookBellBind = true;
                CustomBehaviour.bellBindEffect = Object.Instantiate(go, PaleAutomatonPlugin.songKnight.transform, true);
                Object.Destroy(go);
                CustomBehaviour.bellBindEffect.GetComponent<FollowTransform>().enabled = false;
                CustomBehaviour.bellBindEffect.transform.localScale = new Vector3(1.7f, 1.7f, 1);
                CustomBehaviour.bellBindEffect.transform.localPosition = Vector3.zero;
                CustomBehaviour.bellBindEffect.SetActive(false);
                HeroController.instance.transform.Find("Tool Effects").Find("Bell Bind").gameObject.SetActive(false);
                HeroController.instance.bellBindFSM.Reset();
            }
        }
    }
    public static bool tookBellBind;
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.FreezeMoment), typeof(FreezeMomentTypes), typeof(Action))]
    private static bool GameManager_FreezeMoment(GameManager __instance, FreezeMomentTypes type, Action onFinish)
    {
        if (!PaleAutomatonPlugin.bossScene) return true;
        return type switch
        {
            FreezeMomentTypes.NailClashEffect when Settings.DISABLE_PARRY_FREEZE => false,
            FreezeMomentTypes.BossDeathSlow => false,
            _ => true
        };
    }
    [HarmonyPrefix]
    [HarmonyPatch(typeof(HealthManager), nameof(HealthManager.Hit))]
    private static void HealthManager_Hit(HealthManager __instance, ref HitInstance hitInstance)
    {
        if (!PaleAutomatonPlugin.bossScene) return;
        if (StateData.IsInParryableState())
        {
            hitInstance.DamageDealt = 0;
            PaleAutomatonPlugin.Instance.StartCoroutine(PaleAutomatonPlugin.damageHero.NailClash(0, "Nail Attack", PaleAutomatonPlugin.songKnight.transform.position));
            GameManager.instance.FreezeMoment(FreezeMomentTypes.NailClashEffect);
        }
        else if (PaleAutomatonPlugin.controlFsm.ActiveStateName.Contains("Antic") &&
                 PaleAutomatonPlugin.controlFsm.ActiveStateName is not "Dash Slash Antic" &&
                 !PaleAutomatonPlugin.customComboSequence &&
                 !PaleAutomatonPlugin.PHASE_3) PaleAutomatonPlugin.Instance.StartCoroutine(CustomBehaviour.AnticParry());
    }
    [HarmonyPrefix]
    [HarmonyPatch(typeof(DamageHero), nameof(DamageHero.NailClash))]
    private static void DamageHero_NailClash_Prefix(DamageHero __instance)
    {
        if (!CustomBehaviour.csSpam) return;
        if (!__instance.transform.parent.gameObject.name.StartsWith("CrossSlashSetup")) return;
        HeroController.instance.StartInvulnerable(0.15f);
        HeroController.instance.invulnerableDuration = 0.15f;
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(DamageHero), nameof(DamageHero.NailClash))]
    private static void DamageHero_NailClash_Postfix(DamageHero __instance)
    {
        if (!PaleAutomatonPlugin.bossScene) return;
        if (HeroController.instance.cState.downAttacking) HeroController.instance.DownspikeBounce(false);
        HeroController.instance.AddSilkParts(1, false);
        PaleAutomatonPlugin.healthManager.SpriteFlash.flashArmoured();
        var damage = PlayerData.instance.nailDamage / 2;
        if (PaleAutomatonPlugin.PHASE_4)
        {
            if (PaleAutomatonPlugin.healthManager.hp - damage > 1) PaleAutomatonPlugin.healthManager.hp -= damage;
            else PaleAutomatonPlugin.healthManager.hp = 1;
        }
        else if (PaleAutomatonPlugin.PHASE_3)
        {
            if (PaleAutomatonPlugin.healthManager.hp - damage > PaleAutomatonPlugin.PHASE_4_THRESHOLD) PaleAutomatonPlugin.healthManager.hp -= damage;
            else PaleAutomatonPlugin.healthManager.hp = PaleAutomatonPlugin.PHASE_4_THRESHOLD;
        }
        else if (PaleAutomatonPlugin.PHASE_2)
        {
            if (PaleAutomatonPlugin.healthManager.hp - damage > PaleAutomatonPlugin.PHASE_3_THRESHOLD) PaleAutomatonPlugin.healthManager.hp -= damage;
            else PaleAutomatonPlugin.healthManager.hp = PaleAutomatonPlugin.PHASE_3_THRESHOLD;
        }
        else
        {
            if (PaleAutomatonPlugin.healthManager.hp - damage > PaleAutomatonPlugin.PHASE_2_THRESHOLD) PaleAutomatonPlugin.healthManager.hp -= damage;
            else PaleAutomatonPlugin.healthManager.hp = PaleAutomatonPlugin.PHASE_2_THRESHOLD;
        }
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(DamageHero), nameof(DamageHero.OnEnable))]
    private static void DamageHero_OnEnable(DamageHero __instance)
    {
        if (!PaleAutomatonPlugin.bossScene) return;
        if (__instance.name == "Song Knight") __instance.enabled = false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CameraController), nameof(CameraController.LockToArea))]
    public static bool CameraController_LockToArea(CameraController __instance, CameraLockArea lockArea)
    {
        if (!PaleAutomatonPlugin.bossScene || lockArea.gameObject.name != "CameraLockArea (1)") return true;
        if (!__instance.lockZoneList.Contains(lockArea) || lockArea == __instance.currentLockArea)
        {
            if (lockArea != __instance.currentLockArea) __instance.lockZoneList.Add(lockArea);
            if (__instance.currentLockArea != null && __instance.currentLockArea.priority > lockArea.priority) return false;
            if (lockArea.IgnoreInSuperjump && __instance.hero_ctrl.cState.superDashing) return false;
            __instance.currentLockArea = lockArea;
            if (__instance.mode != CameraController.CameraMode.FROZEN) __instance.SetMode(CameraController.CameraMode.LOCKED);
            __instance.xLockMin = lockArea.cameraXMin;
            __instance.xLockMax = lockArea.cameraXMax;
            __instance.yLockMin = lockArea.cameraYMin < 8.3f ? 8.3f : lockArea.cameraYMin;
            __instance.yLockMax = lockArea.cameraYMax;
            if (__instance.startLockedTimer > 0f && (__instance.hero_ctrl.transitionState != HeroTransitionState.WAITING_TO_TRANSITION || __instance.instantLockedArea.Contains(lockArea)))
            {
                var position = __instance.hero_ctrl.transform.position;
                position.x += __instance.camTarget.xOffset;
                __instance.camTarget.transform.SetPosition2D(__instance.KeepWithinLockBounds(position));
                __instance.camTarget.destination = __instance.camTarget.transform.position;
                __instance.camTarget.EnterLockZoneInstant(__instance.xLockMin, __instance.xLockMax, __instance.yLockMin, __instance.yLockMax);
                __instance.gameObject.transform.SetPosition2D(__instance.KeepWithinLockBounds(position));
                __instance.destination = __instance.gameObject.transform.position;
                __instance.instantLockedArea.Add(lockArea);
                lockArea.OnDestroyEvent += __instance.OnLockAreaDestroyed;
                return false;
            }
        }
        var hcPos = HeroController.instance.transform.position.x;
        __instance.camTarget.EnterLockZone(hcPos - 500, hcPos + 500, __instance.yLockMin, __instance.yLockMax);
        return false;
    }
}