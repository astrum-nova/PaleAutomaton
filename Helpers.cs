using System;
using System.Collections;
using System.Linq;
using Silksong.AssetHelper.ManagedAssets;
using Silksong.FsmUtil;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PaleAutomaton;

public static class Helpers
{
    public static void MakeProjectileIgnoreEnvironment(GameObject projectile)
    {
        var colliders = projectile.GetComponentsInChildren<Collider2D>(true);
        if (colliders == null || colliders.Length == 0) return;
        foreach (var col in colliders)
        {
            var environmentLayer = LayerMask.NameToLayer("Terrain");
            if (environmentLayer >= 0) Physics2D.IgnoreLayerCollision(projectile.layer, environmentLayer, true);
            col.isTrigger = true;
        }
    }
    public static void MakeProjectileRenderAboveWalls(GameObject projectile)
    {
        var tk2dSprites = projectile.GetComponentsInChildren<tk2dSprite>(true);
        foreach (var s in tk2dSprites) s.SortingOrder = 1000;
    }
    public static void RemoveProjectileWallEvents(GameObject projectile)
    {
        var fsm = projectile.LocateMyFSM("Control");
        foreach (var state in fsm.FsmStates)
        {
            var newTransitions = state.Transitions.Where(t => !t.EventName.Equals("WALL", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (newTransitions.Length != state.Transitions.Length) state.Transitions = newTransitions;
        }
        foreach (var stateName in new[] { "Wall End", "Floor?" })
        {
            var s = fsm.GetState(stateName);
            if (s == null) continue;
            s.Transitions = [];
            s.Actions = [];
        }
    }
    public static void RemoveEventFromState(string stateName, string eventName)
    {
        var state = PaleAutomatonPlugin.controlFsm.FsmStates.FirstOrDefault(state => state.Name == stateName)!;
        state.Transitions = state.Transitions.Where(t => t.EventName != eventName).ToArray();
    }
    public static IEnumerator DelayedTurnAround(float delay)
    {
        yield return new WaitForSeconds(delay);
        PaleAutomatonPlugin.controlFsm.transform.FlipLocalScale(x:true);
    }
    public static IEnumerator ScheduleNextState(float delay, string state)
    {
        yield return new WaitForSeconds(delay);
        PaleAutomatonPlugin.controlFsm.SetState(state);
    }
    public static IEnumerator TpEffect()
    {
        if (!CustomBehaviour.tpEffectSetup)
        {
            yield return CustomBehaviour.SK_PROJECTILE_ASSET.Load();
            CustomBehaviour.tpEffectSetup = CustomBehaviour.SK_PROJECTILE_ASSET.InstantiateAsset();
            CustomBehaviour.tpEffectSetup.GetComponent<Collider2D>().isTrigger = true;
            MakeProjectileIgnoreEnvironment(CustomBehaviour.tpEffectSetup);
            RemoveProjectileWallEvents(CustomBehaviour.tpEffectSetup);
            MakeProjectileRenderAboveWalls(CustomBehaviour.tpEffectSetup);
            CustomBehaviour.tpEffectSetup.AddComponent<TeleportEffect>();
            Object.Destroy(CustomBehaviour.tpEffectSetup.GetComponent<DamageHero>());
            CustomBehaviour.tpEffectSetup.transform.localScale = new Vector3(0.75f, 1, 1);
            CustomBehaviour.tpEffectSetup.SetActive(false);
            CustomBehaviour.tpEffectSetup.transform.position = new Vector3(0, -1000, 0);
            CustomBehaviour.tpEffectSetup.name = "TeleportEffect";
        }
        var tpEffectTop = Pools.GetTpEffect();
        var tpEffectBottom = Object.Instantiate(tpEffectTop);
        tpEffectTop.SetActive(true);
        tpEffectBottom.SetActive(true);
        yield return new WaitForSeconds(1);
        tpEffectTop.SetActive(false);
        tpEffectBottom.SetActive(false);
    }
    public static IEnumerator DiveTurnaround()
    {
        yield return new WaitForSeconds(0.15f);
        if (PaleAutomatonPlugin.songKnight.transform.localScale.x > 0)
        {
            if (PaleAutomatonPlugin.songKnight.transform.position.x < HeroController.instance.transform.position.x) PaleAutomatonPlugin.songKnight.transform.FlipLocalScale(x: true);
        }
        else
        {
            if (PaleAutomatonPlugin.songKnight.transform.position.x > HeroController.instance.transform.position.x) PaleAutomatonPlugin.songKnight.transform.FlipLocalScale(x: true);
        }
    }
    public static float GetAdaptedSpeed(float speed, float min, float max) => Mathf.Clamp(Math.Abs(HeroController.instance.transform.position.x - PaleAutomatonPlugin.songKnight.transform.position.x) * speed, min, max);
    /*
    Debug.Log("DISTANCE: " + Math.Abs(HeroController.instance.transform.position.x - PaleAutomatonPlugin.songKnight.transform.position.x));
    Debug.Log("ADAPTED SPEED: " + Math.Abs(HeroController.instance.transform.position.x - PaleAutomatonPlugin.songKnight.transform.position.x) * speed);
    Debug.Log("MAX: " + max + ", MIN: " + min);
    */
}