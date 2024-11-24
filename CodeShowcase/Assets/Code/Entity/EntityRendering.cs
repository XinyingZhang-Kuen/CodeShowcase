using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class EntityRendering
{
    public GameObject gameObject;
    public readonly List<RendererWrapper> rendererWrappers = new();

    public void Init(GameObject gameObject)
    {
        this.gameObject = gameObject;

        using (GetComponentsInChildrenScope<Renderer> scope = new GetComponentsInChildrenScope<Renderer>(gameObject, true))
        {
            foreach (Renderer renderer in scope.components)
            {
                RendererWrapper rendererWrapper = GenericPool<RendererWrapper>.Get();
                rendererWrapper.Init(renderer);
                rendererWrappers.Add(rendererWrapper);
            }
        }
    }

    public void Clear()
    {
        gameObject = default;
        foreach (RendererWrapper rendererWrapper in rendererWrappers)
        {
            rendererWrapper.Clear();
            GenericPool<RendererWrapper>.Release(rendererWrapper);
        }
        rendererWrappers.Clear();
    }
}