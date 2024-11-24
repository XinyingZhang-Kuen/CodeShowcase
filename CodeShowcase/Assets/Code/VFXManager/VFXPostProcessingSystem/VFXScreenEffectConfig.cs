using UnityEngine;

[CreateAssetMenu(fileName = "VFXScreenEffectConfig", menuName = "VFX/ScreenEffectConfig")]
public class VFXScreenEffectConfig : VFXConfig
{
    public override VFXSystemFeatures Features => VFXSystemFeatures.Staging;
    public override VFXSystemID SystemID => VFXSystemID.VFXScreenEffectSystem;
    public override bool IsValid => prefab;
    
    public GameObject prefab; // Does not support async loading yet, because this is just a showcase.
    public StretchMode stretchMode;
    public FollowMode followMode;
}