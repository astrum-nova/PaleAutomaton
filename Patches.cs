using System;
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
                //todo: replace this with the asset like projectile
                CustomBehaviour.crossSlashSetup = Object.Instantiate(go);
                CustomBehaviour.crossSlashSetup.SetActive(false);
                CustomBehaviour.crossSlashSetup.name = "CrossSlashSetup";
                var csFsm = CustomBehaviour.crossSlashSetup.GetComponent<PlayMakerFSM>();
                csFsm.GetState("Recycle")!.RemoveActionsOfType<RecycleSelf>();
            }
        } else if (spawned.name.StartsWith("Song Knight Projectile"))
        {
            go.SetActive(false);
            PaleAutomatonPlugin.Instance.StartCoroutine(CustomBehaviour.SpawnWindSlash());
        }
    }
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.FreezeMoment), typeof(FreezeMomentTypes), typeof(Action))]
    private static bool GameManager_FreezeMoment(GameManager __instance, FreezeMomentTypes type, Action onFinish)
    {
        if (!PaleAutomatonPlugin.bossScene) return true;
        return type != FreezeMomentTypes.NailClashEffect && type != FreezeMomentTypes.BossDeathSlow;
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
}