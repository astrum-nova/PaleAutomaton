using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PaleAutomaton;

public static class Pools
{
    private static GameObject pooledObjectsParent = new("Pooled Objects Parent");
    private static readonly List<GameObject> windSlashes = [];

    public static GameObject GetWindSlash() => GetPooledObject(windSlashes, CustomBehaviour.skProjectileSetup);

    private static GameObject GetPooledObject(List<GameObject> pool, GameObject setup)
    {
        GameObject clone = null!;
        var found = false;
        foreach (var obj in pool.Where(obj => !setup.name.Equals("spear") ? !obj.activeInHierarchy : !obj.transform.GetChild(0).gameObject.activeInHierarchy))
        {
            clone = obj;
            found = true;
            break;
        }
        return found ? clone! : AddToPool(pool, setup);
    }
    private static GameObject AddToPool(List<GameObject> pool, GameObject setup)
    {
        var clone = Object.Instantiate(setup, pooledObjectsParent.transform);
        clone.name += "_POOLED";
        pool.Add(clone);
        return clone;
    }

    public static void Clear()
    {
        pooledObjectsParent = new GameObject("Pooled Objects Parent");
        Object.DontDestroyOnLoad(pooledObjectsParent);
        windSlashes.Clear();
    }
}