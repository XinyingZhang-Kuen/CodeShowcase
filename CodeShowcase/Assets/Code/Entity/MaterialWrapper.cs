using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public partial class MaterialWrapper
{
    private Material _originalMaterial;
    private Material _instancedMaterial;
    
    public RendererWrapper rendererWrapper { get; private set; }
    public int materialIndex { get; private set; }
    
    /// <summary>
    /// Note that this getter is the same as Renderer.material, which means that it will automatically copy the original material if it hasn't.
    /// </summary>
    public Material instancedMaterial
    {
        get
        {
            if (_instancedMaterial == null && _originalMaterial != null)
            {
                _instancedMaterial = new Material(_originalMaterial);
                List<Material> sharedMaterials = ListPool<Material>.Get();
                rendererWrapper.renderer.GetSharedMaterials(sharedMaterials);
                sharedMaterials[materialIndex] = _instancedMaterial;
                rendererWrapper.renderer.SetSharedMaterials(sharedMaterials);
            }

            return _instancedMaterial;
        }
    }

    public Material sharedMaterial => _instancedMaterial == null ? _originalMaterial : _instancedMaterial;

    public void Init(Material material, RendererWrapper rendererWrapper, int materialIndex)
    {
        this.rendererWrapper = rendererWrapper;
        _originalMaterial = material;
        this.materialIndex = materialIndex;
    }

    public void BeforeRendering()
    {
        if (_originalMaterial)
        {
            ApplyOverriders();
        }
    }

    public void Clear()
    {
        if (_instancedMaterial)
        {
            _instancedMaterial.Destroy();
        }
        
        _originalMaterial = default;
        _instancedMaterial = default;
        rendererWrapper = default;
        materialIndex = default;
        _originalMaterial = null;
        
        ClearOverriders();
    }
}