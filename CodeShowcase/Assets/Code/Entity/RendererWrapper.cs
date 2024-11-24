using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class RendererWrapper
{
    public readonly List<MaterialWrapper> materialWrappers = new();
    public Renderer renderer { get; private set; }

    public void Init(Renderer renderer)
    {
        this.renderer = renderer;
        using (GetMaterialsScope scope = new GetMaterialsScope(renderer, true))
        {
            for (int index = 0; index < scope.materials.Count; index++)
            {
                Material material = scope.materials[index];
                MaterialWrapper materialWrapperOverrider = GenericPool<MaterialWrapper>.Get();
                materialWrapperOverrider.Init(material, this, index);
                materialWrappers.Add(materialWrapperOverrider);
            }
        }
    }

    public void BeforeRendering()
    {
        if (renderer)
        {
            foreach (MaterialWrapper materialWrapper in materialWrappers)
            {
                materialWrapper.BeforeRendering();
            }
        }
    }

    public void Clear()
    {
        renderer = null;
        
        foreach (MaterialWrapper materialWrapper in materialWrappers)
        {
            materialWrapper.Clear();
            GenericPool<MaterialWrapper>.Release(materialWrapper);
        }

        materialWrappers.Clear();
    }
}