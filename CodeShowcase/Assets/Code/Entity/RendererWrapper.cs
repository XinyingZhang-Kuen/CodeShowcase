using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class RendererWrapper
{
    public readonly List<MaterialWrapper> materialWrappers = new();
    public void Init(Renderer renderer)
    {
        using (GetMaterialsScope scope = new GetMaterialsScope(renderer, true))
        {
            foreach (Material material in scope.materials)
            {
                MaterialWrapper materialWrapper = GenericPool<MaterialWrapper>.Get();
                materialWrapper.Init(material);
                materialWrappers.Add(materialWrapper);
            }
        }
    }

    public void Clear()
    {
        foreach (MaterialWrapper materialWrapper in materialWrappers)
        {
            materialWrapper.Clear();
            GenericPool<MaterialWrapper>.Release(materialWrapper);
        }
        materialWrappers.Clear();
    }
}