using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Architect.Behaviour.Utility;
using GlobalEnums;
using Silksong.AssetHelper.ManagedAssets;
using Silksong.FsmUtil;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    public static void SetupGroundSpikeHitbox(GameObject groundSpikesCollider)
    {
        groundSpikesCollider.layer = LayerMask.NameToLayer("Enemies");
        var damageHero = groundSpikesCollider.GetComponent<DamageHero>();
        damageHero.SetDamageAmount(2);
        damageHero.hazardType = HazardType.NON_HAZARD;
        var collider = groundSpikesCollider.GetComponent<PolygonCollider2D>();
        collider.name = "GroundSpikeColliderComponent";
        collider.SetPath(0, new List<Vector2>()
        {
            new(0, 0),
            new(0, 1),
            new(1, 1),
            new(1, 0),
        });
        groundSpikesCollider.transform.position = groundSpikesCollider.transform.position with { y = 12.5f };
        groundSpikesCollider.transform.localScale = groundSpikesCollider.transform.localScale with { y = 180 };
        groundSpikesCollider.transform.localScale = groundSpikesCollider.transform.localScale with { x = 16.4f };
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
        PaleAutomatonPlugin.songKnight.transform.rotation = Quaternion.Euler(0, 0, PaleAutomatonPlugin.controlFsm.ActiveStateName.Equals("Dive Antic") ? 0 : angle);
    }
    public static float GetAdaptedSpeed(float speed, float min, float max) => Mathf.Clamp(Math.Abs(HeroController.instance.transform.position.x - PaleAutomatonPlugin.songKnight.transform.position.x) * speed, min, max);
    public static Vector2[] originalDownSlash = null!;
    public static Vector2[] originalDownSlashAlt = null!;
    private static readonly Vector2[] expandedDownSlash = [
        new(3.524622f, 0.000000f),
        new(3.256326f, 1.211478f),
        new(2.492284f, 2.238519f),
        new(1.348815f, 2.924766f),
        new(0.000000f, 3.165744f),
        new(-1.348815f, 2.924766f),
        new(-2.492284f, 2.238519f),
        new(-3.256326f, 1.211478f),
        new(-3.524622f, 0.000000f),
        new(-3.256326f, -1.211478f),
        new(-2.492284f, -2.238519f),
        new(-1.348815f, -2.924766f),
        new(-0.000000f, -3.165744f),
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
    public static void DisableChargingEffects()
    {
        PaleAutomatonPlugin.songKnight.transform.GetChild(17).gameObject.SetActive(false);
        PaleAutomatonPlugin.songKnight.transform.GetChild(18).gameObject.SetActive(false);
    }
    private static readonly HashSet<string> arenaWhitelist = [
        "Black Thread States",
        "strut_bg_song_bridge_example",
        "song_city_default (1)",
        "GameObject (11)",
        "wind_tiled_set",
        "BlurPlane",
        "Spike Collider",
        "CameraLockArea (1)",
        "Camera Wind Region",
        "terrain collider",
        "Boss Scene - To Additive Load(Clone)",
    ];
    private static readonly HashSet<string> arenaBlacklist = [
        "dust_root_bg_set (1)",
        "dust_root_bg_set",
        "black_fader_moon (4)",
    ];
    public static GameObject mainTerrainArt = null!;
    public static GameObject clonedTerrainArt = null!;
    public static GameObject clonedGroundSpikes = null!;
    public static GameObject clonedGroundSpikesCollider = null!;
    public static GameObject fallKiller = null!;
    public static GameObject cameraLockArea = null!;
    public static void SetupArena()
    {
        // Camera
        GameCameras.instance.cameraController.SetAllowExitingSceneBounds(true);
        var sceneBorderRemover = new GameObject("Scene Border Remover");
        Object.DontDestroyOnLoad(sceneBorderRemover);
        sceneBorderRemover.SetActive(false);
        SceneBorderRemover.Init();
        sceneBorderRemover.AddComponent<SceneBorderRemover>();
        sceneBorderRemover.transform.position = new Vector3(0, 0, 0.1f);
        // Hitboxes
        PaleAutomatonPlugin.terrainCollider.transform.localScale = PaleAutomatonPlugin.terrainCollider.transform.localScale with { x = 1.1f };
        PaleAutomatonPlugin.terrainCollider.transform.position = PaleAutomatonPlugin.terrainCollider.transform.position with { x = 33.0313f };
        
        CustomBehaviour.groundSpikesCollider = Object.Instantiate(GameObject.Find("Spike Collider"))!;
        CustomBehaviour.groundSpikesCollider.name = "GroundSpikesCollider";
        SetupGroundSpikeHitbox(CustomBehaviour.groundSpikesCollider);
        fallKiller = Object.Instantiate(GameObject.Find("Spike Collider"))!;
        SetupGroundSpikeHitbox(fallKiller);
        fallKiller.name = "FallKiller";
        fallKiller.transform.position = fallKiller.transform.position with { y = -2 };
        fallKiller.GetComponent<DamageHero>().SetDamageAmount(999);
        fallKiller.transform.localScale = fallKiller.transform.localScale with {y = 500};
        fallKiller.transform.localScale = fallKiller.transform.localScale with {x = 100};
        fallKiller.AddComponent<NonBouncer>();
        foreach (var gameObject in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (gameObject.name.Contains("Audio")) continue;
            if (arenaWhitelist.Contains(gameObject.name))
            {
                switch (gameObject.name)
                {
                    case "CameraLockArea (1)":
                        gameObject.transform.position = gameObject.transform.position with { x = 150.7405f };
                        gameObject.transform.localScale = gameObject.transform.localScale with { x = gameObject.transform.localScale.x * 5 };
                        gameObject.transform.localScale = gameObject.transform.localScale with { y = gameObject.transform.localScale.y * 5 };
                        cameraLockArea = gameObject;
                        InfiniteTerrainMover.cameraLockArea = gameObject.GetComponent<CameraLockArea>();
                        break;
                    case "Spike Collider":
                        gameObject.transform.position = gameObject.transform.position with { y = -1000 };
                        break;
                    case "wind_tiled_set":
                        gameObject.transform.Find("plane").localScale *= 200;
                        gameObject.transform.Find("plane (1)").localScale *= 200;
                        gameObject.transform.Find("plane (2)").localScale *= 200;
                        gameObject.transform.Find("plane (3)").localScale *= 200;
                        break;
                    case "strut_bg_song_bridge_example":
                        foreach (var objName in (string[])[
                                     //? Left side parts sticking up
                                     "bridge_under_strut_plat_45_angle (8)",
                                     "dock_arch_small_strut_0001_1 (328)",
                                     "dock_arch_small_strut_0001_1 (373)",
                                     "dock_arch_small_strut_0001_1 (354)",
                                     //? Right section separated from the structure
                                     "dock_arch_small_strut_0001_1 (557)",
                                     "dock_arch_small_strut_0001_1 (554)",
                                     "dock_arch_small_strut_0001_1 (549)",
                                     "dock_arch_small_strut_0001_1 (547)",
                                     "dock_arch_small_strut_0001_1 (546)",
                                     "dock_arch_small_strut_0001_1 (537)",
                                     "dock_arch_small_strut_0001_1 (532)",
                                     "dock_arch_small_strut_0001_1 (527)",
                                     "dock_arch_small_strut_0001_1 (525)",
                                     "dock_arch_small_strut_0001_1 (522)",
                                     "dock_arch_small_strut_0001_1 (520)",
                                     "dock_arch_small_strut_0001_1 (515)",
                                     "dock_arch_small_strut_0001_1 (512)",
                                     "dock_arch_small_strut_0001_1 (511)",
                                     "dock_arch_small_strut_0001_1 (509)",
                                     "dock_arch_small_strut_0001_1 (508)",
                                     "dock_arch_small_strut_0001_1 (507)",
                                     "dock_arch_small_strut_0001_1 (499)",
                                     "dock_arch_small_strut_0001_1 (495)",
                                     "dock_arch_small_strut_0001_1 (491)",
                                     "dock_arch_small_strut_0001_1 (466)",
                                     "dock_arch_small_strut_0001_1 (489)",
                                     "dock_arch_small_strut_0001_1 (488)",
                                     "dock_arch_small_strut_0001_1 (484)",
                                     "dock_arch_small_strut_0001_1 (480)",
                                     "dock_arch_small_strut_0001_1 (479)",
                                     "dock_arch_small_strut_0001_1 (530)",
                                     "dock_arch_small_strut_0001_1 (510)",
                                     "dock_arch_small_strut_0001_1 (470)",
                                     "dock_arch_small_strut_0001_1 (468)",
                                     "dock_arch_small_strut_0001_1 (427)",
                                     "dock_arch_small_strut_0001_1 (408)",
                                     "dock_arch_small_strut_0001_1 (406)",
                                     "dock_arch_small_strut_0001_1 (28)",
                                     "sc_metal_strut_back (40)",
                                     "sc_metal_strut_back (33)",
                                     "sc_metal_strut_back (32)",
                                 ]) gameObject.transform.Find(objName).gameObject.SetActive(false);
                        mainTerrainArt = gameObject;
                        PaleAutomatonPlugin.terrainCollider.name = "Terrain Collider";
                        PaleAutomatonPlugin.terrainCollider.transform.SetParent(mainTerrainArt.transform);
                        CustomBehaviour.groundSpikesCollider.transform.SetParent(mainTerrainArt.transform);
                        var infiniteTerrainMoverLeft = new GameObject("Infinite Terrain Mover Left");
                        var colliderLeft = infiniteTerrainMoverLeft.AddComponent<BoxCollider2D>();
                        colliderLeft.isTrigger = true;
                        infiniteTerrainMoverLeft.transform.localScale = new Vector3(1, 100000, 1);
                        infiniteTerrainMoverLeft.transform.position = infiniteTerrainMoverLeft.transform.position with { x = 75 };
                        infiniteTerrainMoverLeft.transform.SetParent(gameObject.transform);
                        var infiniteTerrainMoverRight = new GameObject("Infinite Terrain Mover Right");
                        var colliderRight = infiniteTerrainMoverRight.AddComponent<BoxCollider2D>();
                        colliderRight.isTrigger = true;
                        infiniteTerrainMoverRight.transform.localScale = new Vector3(1, 100000, 1);
                        infiniteTerrainMoverRight.transform.position = infiniteTerrainMoverRight.transform.position with { x = 185 };
                        infiniteTerrainMoverRight.transform.SetParent(gameObject.transform);
                        var itmLeft = infiniteTerrainMoverLeft.AddComponent<InfiniteTerrainMover>();
                        var itmRight = infiniteTerrainMoverRight.AddComponent<InfiniteTerrainMover>();
                        itmLeft.other = infiniteTerrainMoverRight;
                        itmRight.other = infiniteTerrainMoverLeft;
                        //? The first set is almost working right, but its overlapping with the last spike set of the cloned ground
                        var firstSet = PaleAutomatonPlugin.groundSpikesParent.transform.GetChild(0)!;
                        firstSet.GetChild(0).gameObject.SetActive(false);
                        firstSet.GetChild(2).gameObject.SetActive(false);
                        firstSet.GetChild(3).gameObject.SetActive(false);
                        firstSet.GetChild(1).position = new Vector3(45.5544f, 10.9078f, -1.4537f);
                        firstSet.GetChild(6).position = new Vector3(49.7621f, 10.9079f, -1.4537f);
                        firstSet.GetChild(7).position = new Vector3(48.4338f, 10.9079f, -1.4537f);
                        PaleAutomatonPlugin.groundSpikesParent.transform.SetParent(mainTerrainArt.transform);
                        clonedTerrainArt = Object.Instantiate(mainTerrainArt);
                        clonedGroundSpikes = clonedTerrainArt.transform.Find("GroundSpikesParent").gameObject;
                        clonedGroundSpikesCollider = clonedTerrainArt.transform.Find("GroundSpikeColliderComponent").gameObject;
                        clonedGroundSpikesCollider.SetActive(false);
                        clonedTerrainArt.transform.position = new Vector3(-98.7366f, -34.5f, -5.1624f);
                        clonedTerrainArt.transform.Find("Infinite Terrain Mover Right").gameObject.SetActive(false);
                        clonedTerrainArt.transform.Find("Infinite Terrain Mover Left").gameObject.SetActive(true);
                        mainTerrainArt.transform.Find("Infinite Terrain Mover Right").gameObject.SetActive(true);
                        mainTerrainArt.transform.Find("Infinite Terrain Mover Left").gameObject.SetActive(true);
                        mainTerrainArt.name = "Main Terrain Art";
                        clonedTerrainArt.name = "Cloned Terrain Art";
                        break;
                    case "BlurPlane":
                        gameObject.transform.SetParent(HeroController.instance.transform);
                        gameObject.transform.position = gameObject.transform.position with { y = gameObject.transform.position.y - 10 };
                        break;
                    case "Black Thread States":
                        var targetChild = gameObject.transform.GetChild(0)!;
                        foreach (var objName in (string[])[
                                     "hanging_garden__0013_fence_mid (9)",
                                     "song_city_pipes_0016_1 (16)",
                                     "song_fence_standard (8)",
                                     "song_city_pipes_0016_1 (12)",
                                     "song_city_pipes_0016_1 (11)",
                                     "song_city_pipes_0016_1 (15)",
                                     "hanging_garden__0013_fence_mid (10)",
                                     "song_city_pipes_0016_1 (9)",
                                     "hanging_garden__0017_arch_brace (6)",
                                     "hanging_garden__0013_fence_mid (16)",
                                     "hanging_garden__0013_fence_mid (17)",
                                     "break_lamp_slab_bridge",
                                     "arborium_tunnel_simple",
                                 ]) targetChild.Find(objName).gameObject.SetActive(false);
                        break;
                }
                continue;
            }
            if (gameObject.transform.position.z < 25 || arenaBlacklist.Contains(gameObject.name)) gameObject.SetActive(false);
            else
            {
                //todo: mirror some bg objects so it doesnt look scuffed asf
                
            }
            PaleAutomatonPlugin.terrainCollider.SetActive(true);
            fallKiller.SetActive(true);
        }
    }
}