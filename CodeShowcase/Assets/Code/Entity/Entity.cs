using UnityEngine;
using UnityEngine.Pool;

public class Entity
{
    public GameObject gameObject { get; private set; }
    public EntityRendering rendering { get; private set; }

    public void Init(GameObject gameObject)
    {
        this.gameObject = gameObject;
        if (gameObject)
        {
            rendering = GenericPool<EntityRendering>.Get();
            rendering.Init(gameObject);
        }
    }

    public void Clear()
    {
        gameObject = null;
        if (rendering != null)
        {
            rendering.Clear();
            GenericPool<EntityRendering>.Release(rendering);
            rendering = null;
        }
    }

    public void BeforeRendering()
    {
        rendering.BeforeRendering();
    }
}