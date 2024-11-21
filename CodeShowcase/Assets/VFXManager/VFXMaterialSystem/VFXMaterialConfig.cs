using System;
using System.Collections.Generic;

public class VFXMaterialConfig : VFXConfig
{
    public override VFXSystemFeatures Features => features;
    public override VFXSystemID SystemID => VFXSystemID.VFXMaterialSystem;
    public VFXSystemFeatures features = VFXSystemFeatures.Staging;
    public List<VFXMaterialFunction> fadeInFunctions = new List<VFXMaterialFunction>();
    public List<VFXMaterialFunction> loopFunctions = new List<VFXMaterialFunction>();
    public List<VFXMaterialFunction> fadeOutFunctions = new List<VFXMaterialFunction>();

    public List<VFXMaterialFunction> GetCurrentStageFunctions(VFXStage stage)
    {
        switch (stage)
        {
            case VFXStage.Inactive:
                throw new ArgumentOutOfRangeException(nameof(stage), stage, null);
            case VFXStage.FadingIn:
                return fadeInFunctions;
            case VFXStage.Looping:
                return loopFunctions;
            case VFXStage.FadingOut:
                return fadeOutFunctions;
            default:
                throw new ArgumentOutOfRangeException(nameof(stage), stage, null);
        }
    }
}