using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "VFXMaterialConfig", menuName = "VFX/MaterialConfig")]
public class VFXMaterialConfig : VFXConfig
{
    public override VFXSystemFeatures Features => VFXSystemFeatures.Staging | VFXSystemFeatures.RequiresACaller;
    public override VFXSystemID SystemID => VFXSystemID.VFXMaterialSystem;
    public override bool IsValid => true;
    public int priority;
    public List<Shader> supportedShaders = new();
    [SerializeReference] public List<VFXMaterialModifier> modifiers = new();

    public virtual void OnValidate()
    {
        // Do NOT move this into the loop, or here will be more gc.
        string assetName = name;
        foreach (VFXMaterialModifier modifier in modifiers)
        {
            modifier.Init(assetName);
        }
    }
}