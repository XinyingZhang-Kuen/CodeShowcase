using UnityEngine;
using UnityEngine.Pool;

public class Entity
{
    public GameObject gameObject { get; private set; }
    public EntityRendering entityRendering { get; private set; }

    public void Init(GameObject gameObject)
    {
        this.gameObject = gameObject;
        if (gameObject)
        {
            entityRendering = GenericPool<EntityRendering>.Get();
            entityRendering.Init(gameObject);
        }
    }

    public void Clear()
    {
        gameObject = null;
        if (entityRendering != null)
        {
            entityRendering.Clear();
            GenericPool<EntityRendering>.Release(entityRendering);
            entityRendering = null;
        }
    }
}