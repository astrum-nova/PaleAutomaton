using UnityEngine;

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
        cameraLockArea.enabled = false;
        Helpers.cameraLockArea.transform.position = Helpers.cameraLockArea.transform.position with { x = HeroController.instance.transform.position.x };
        var parentTransform = (other.transform.parent.gameObject.name == "Main Terrain Art" ? Helpers.clonedTerrainArt : Helpers.mainTerrainArt).transform;
        switch (gameObject.name)
        {
            case "Infinite Terrain Mover Left":
                parentTransform.Find("Infinite Terrain Mover Right").gameObject.SetActive(false);
                parentTransform.Find("Infinite Terrain Mover Left").gameObject.SetActive(true);
                parentTransform.position = parentTransform.position with { x = gameObject.transform.position.x - TERRAIN_DISTANCE_LEFT };
                break;
            case "Infinite Terrain Mover Right":
                parentTransform.Find("Infinite Terrain Mover Left").gameObject.SetActive(false);
                parentTransform.Find("Infinite Terrain Mover Right").gameObject.SetActive(true);
                parentTransform.position = parentTransform.position with { x = gameObject.transform.position.x + TERRAIN_DISTANCE_RIGHT };
                break;
        }
        cameraLockArea.enabled = true;
        cameraLockArea.OnInsideStateChanged(true);
    }
}