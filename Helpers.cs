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
        var tpEffect = Pools.GetTpEffect();
        tpEffect.transform.position = PaleAutomatonPlugin.songKnight.transform.position;
        tpEffect.SetActive(true);
        PaleAutomatonPlugin.healthManager.SpriteFlash.flashArmoured();
        yield return new WaitForSeconds(1);
        tpEffect.SetActive(false);
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
    public static Vector2 GetNormalizedDirection()
    {
        var res = (HeroController.instance.transform.position - PaleAutomatonPlugin.songKnight.transform.position).normalized;
        res.x = Math.Abs(res.x);
        return res;
    }
    public static void LookAtHornet() 
    {
        var diff = HeroController.instance.transform.position - PaleAutomatonPlugin.songKnight.transform.position;
        var angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        if (HeroController.instance.transform.position.x < PaleAutomatonPlugin.songKnight.transform.position.x) angle -= 180;
        PaleAutomatonPlugin.songKnight.transform.rotation = Quaternion.Euler(0, 0, angle);
    }
    public static float GetAdaptedSpeed(float speed, float min, float max) => Mathf.Clamp(Math.Abs(HeroController.instance.transform.position.x - PaleAutomatonPlugin.songKnight.transform.position.x) * speed, min, max);
    public static Vector2[] originalDownSlash = null!;
    public static Vector2[] originalDownSlashAlt = null!;
    private static readonly Vector2[] expandedDownSlash =
    [
        new(3.524622f, 0.000000f),    // Right Center
        new(3.256326f, 1.211478f),
        new(2.492284f, 2.238519f),
        new(1.348815f, 2.924766f),
        new(0.000000f, 3.165744f),    // Top Reference (Unchanged)
        new(-1.348815f, 2.924766f),
        new(-2.492284f, 2.238519f),
        new(-3.256326f, 1.211478f),
        new(-3.524622f, 0.000000f),   // Left Center
        new(-3.256326f, -1.211478f),
        new(-2.492284f, -2.238519f),
        new(-1.348815f, -2.924766f),
        new(-0.000000f, -3.165744f),  // Bottom Reference (Unchanged)
        new(1.348815f, -2.924766f),
        new(2.492284f, -2.238519f),
        new(3.256326f, -1.211478f),
    ];
    public static void ToggleDownSlashHitbox(bool useExpanded)
    {
        if (originalDownSlash == null || originalDownSlashAlt == null) return;
        HeroController.instance.transform.Find("Attacks").Find("Wanderer").Find("DownSlash").gameObject.GetComponent<PolygonCollider2D>().SetPath(0, useExpanded ? expandedDownSlash : originalDownSlash);
        HeroController.instance.transform.Find("Attacks").Find("Wanderer").Find("DownSlashAlt").gameObject.GetComponent<PolygonCollider2D>().SetPath(0, useExpanded ? expandedDownSlash : originalDownSlashAlt);
    }
}