using System;
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

public enum StretchMode
{
    Stretch,
    AlignHeight,
    AlignWidth,
}

public enum FollowMode
{
    UnderCamera,
    /// <summary>
    /// FollowTransform is slower than UnderCamera, because we need to calculate the transform vectors.
    /// </summary>
    FollowTransform,
}

public class VFXScreenEffectState : VFXState<VFXScreenEffectConfig>
{
    public GameObject gameObject;
}

public class VFXScreenEffectSystem : VFXSystem<VFXScreenEffectSystem, VFXScreenEffectConfig, VFXScreenEffectState>
{
    private CachedCameraData cameraData;

    public override void Init()
    {
        Camera camera = trigger.gameObject.GetComponent<Camera>();
        cameraData = CameraManager.Instance.GetCameraData(camera);
    }

    protected override void OnStateInit(VFXScreenEffectState state)
    {
        base.OnStateInit(state);

        UnityObjectManager.Spawn(state.config.prefab, out state.gameObject);
        
        if (state.config.followMode == FollowMode.UnderCamera)
        {
            Transform effectTransform = state.gameObject.transform;
            Transform cameraTransform = cameraData.transform;
            effectTransform.SetParent(cameraTransform, false);
        }
    }

    protected override void OnPerform(VFXScreenEffectState state)
    {
        base.OnPerform(state);

        const CameraDirtyState dependency = CameraDirtyState.Position 
                                            | CameraDirtyState.Rotation
                                            | CameraDirtyState.FieldOfView
                                            | CameraDirtyState.Aspect
                                            | CameraDirtyState.LocalScale;

        const CameraDirtyState localScaleDependency = CameraDirtyState.FieldOfView
                                                      | CameraDirtyState.Aspect
                                                      | CameraDirtyState.LocalScale;

        if (state.config.followMode == FollowMode.FollowTransform)
        {
            if ((cameraData.dirtyState & dependency) != 0)
            {
                Transform effectTransform = state.gameObject.transform;
                Transform cameraTransform = cameraData.transform;
                if ((cameraData.dirtyState & CameraDirtyState.Position) != 0)
                {
                    effectTransform.position = cameraTransform.position;
                }

                if ((cameraData.dirtyState & CameraDirtyState.Rotation) != 0)
                {
                    effectTransform.rotation = cameraTransform.rotation;
                }
                
                if ((cameraData.dirtyState & localScaleDependency) != 0)
                {
                    // TODO: Calculate local scale.

                    switch (state.config.stretchMode)
                    {
                        case StretchMode.Stretch:
                            break;
                        case StretchMode.AlignHeight:
                            break;
                        case StretchMode.AlignWidth:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }
    }

    protected override void OnClear(VFXScreenEffectState state)
    {
        state.gameObject = null;
        base.OnClear(state);
    }
}