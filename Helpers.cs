using System;
using System.Collections;
using System.Linq;
using Silksong.FsmUtil;
using UnityEngine;

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