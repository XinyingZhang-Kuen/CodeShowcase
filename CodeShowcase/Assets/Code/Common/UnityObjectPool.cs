using UnityEngine;

public class UnityObjectManager
{
    public static bool Spawn(GameObject prefab, out GameObject instance)
    {
        if (!prefab)
        {
            Debug.LogError("Trying to spawn a null or destroyed prefab!");
            instance = null;
            return false;
        }

        // TODO: pooling.
        instance = Object.Instantiate(prefab);
        return true;
    }
}