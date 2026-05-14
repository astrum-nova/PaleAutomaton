using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PaleAutomaton;

public class InfiniteTerrainMover : MonoBehaviour
{
    private const float TERRAIN_DISTANCE_LEFT = 174f - 0.325f;
    private const float TERRAIN_DISTANCE_RIGHT = 70 + 2.9f;
    public GameObject other = null!;
    public static CameraLockArea cameraLockArea = null!;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.name.StartsWith("Hero_Hornet")) return;
        other.SetActive(true);
        gameObject.SetActive(false);
        Helpers.fallKiller.transform.position = Helpers.fallKiller.transform.position with { x = HeroController.instance.transform.position.x - 250 };
        var claBounds = cameraLockArea.box2d.bounds;
        claBounds.min = claBounds.min with { x = HeroController.instance.transform.position.x - 500 };
        claBounds.max = claBounds.max with { x = HeroController.instance.transform.position.x + 500 };
        cameraLockArea.cameraXMin = HeroController.instance.transform.position.x - 500;
        cameraLockArea.cameraXMax = HeroController.instance.transform.position.x + 500;
        cameraLockArea.cameraYMax = HeroController.instance.transform.position.y + 100000;
        cameraLockArea.enabled = false;
        Helpers.cameraLockArea.transform.position = Helpers.cameraLockArea.transform.position with { x = HeroController.instance.transform.position.x };
        var availableTerrain = (other.transform.parent.gameObject.name == "Main Terrain Art" ? Helpers.clonedTerrainArt : Helpers.mainTerrainArt).transform;
        switch (gameObject.name)
        {
            case "Infinite Terrain Mover Left":
                availableTerrain.Find("Infinite Terrain Mover Right").gameObject.SetActive(false);
                availableTerrain.Find("Infinite Terrain Mover Left").gameObject.SetActive(true);
                availableTerrain.position = availableTerrain.position with { x = gameObject.transform.position.x - TERRAIN_DISTANCE_LEFT };
                break;
            case "Infinite Terrain Mover Right":
                availableTerrain.Find("Infinite Terrain Mover Left").gameObject.SetActive(false);
                availableTerrain.Find("Infinite Terrain Mover Right").gameObject.SetActive(true);
                availableTerrain.position = availableTerrain.position with { x = gameObject.transform.position.x + TERRAIN_DISTANCE_RIGHT };
                break;
        }
        cameraLockArea.enabled = true;
        cameraLockArea.OnInsideStateChanged(true);
        if (!PaleAutomatonPlugin.PHASE_3 && Math.Abs(PaleAutomatonPlugin.songKnight.transform.position.x - HeroController.instance.transform.position.x) > 60) PaleAutomatonPlugin.Instance.StartCoroutine(CustomBehaviour.Teleport(HeroController.instance.transform.position.x + (Random.value > 0.5f ? 10 : -10), 12.9413f, "First Idle"));
    }
}