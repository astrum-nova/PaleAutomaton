using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PaleAutomaton;

public static class Pools
{
    private static GameObject pooledObjectsParent = new("Pooled Objects Parent");
    private static List<GameObject> windSlashes = [];
    private static List<GameObject> tpEffects = [];
    private static List<GameObject> crossSlashes = [];
    private static List<GameObject> crossSlashAntics = [];

    public static GameObject GetWindSlash() => GetPooledObject(ref windSlashes, CustomBehaviour.skProjectileSetup);
    public static GameObject GetTpEffect() => GetPooledObject(ref tpEffects, CustomBehaviour.tpEffectSetup);
    public static GameObject GetCrossSlash() => GetPooledObject(ref crossSlashes, CustomBehaviour.crossSlashSetup);
    public static GameObject GetCrossSlashAntic() => GetPooledObject(ref crossSlashAntics, CustomBehaviour.crossSlashAnticSetup);

    private static GameObject GetPooledObject(ref List<GameObject> pool, GameObject setup)
    {
        GameObject clone = null!;
        var found = false;
        foreach (var obj in pool.Where(obj => !obj.activeSelf))
        {
            clone = obj;
            found = true;
            break;
        }
        return found ? clone! : AddToPool(ref pool, setup);
    }
    private static GameObject AddToPool(ref List<GameObject> pool, GameObject setup)
    {
        var clone = Object.Instantiate(setup, pooledObjectsParent.transform, true)!;
        clone.name += "_POOLED";
        pool.Add(clone);
        return clone;
    }
    public static void Clear()
    {
        pooledObjectsParent = new GameObject("Pooled Objects Parent");
        Object.DontDestroyOnLoad(pooledObjectsParent);
        windSlashes.Clear();
        tpEffects.Clear();
        crossSlashes.Clear();
        crossSlashAntics.Clear();
    }
    public static void DisableAll()
    {
        for (var i = 0; i < pooledObjectsParent.transform.childCount; i++) pooledObjectsParent.transform.GetChild(i).gameObject.SetActive(false);
    }
}