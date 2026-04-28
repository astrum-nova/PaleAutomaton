using System;
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
}