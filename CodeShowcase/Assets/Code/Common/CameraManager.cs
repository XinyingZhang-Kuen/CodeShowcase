using System;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum CameraDirtyState : uint
{
    Position = 1 << 0,
    Rotation = 1 << 1,
    LocalScale = 1 << 2,
    FieldOfView = 1 << 3,
    Aspect = 1 << 4,
    All = ~0u,
}

/// <summary>
/// *** Note that cached properties are updated after LateUpdate, this is used for rendering stage only.
/// *** It may be an performance issue if unity api are called for too many times, ensure that apis are called for once only.
/// TODO: Maybe it would be better to cache those data in urp. 
/// </summary>
public class CachedCameraData
{
    public CameraDirtyState dirtyState;
    public Camera camera;
    public Transform transform;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 localScale;
    public float fieldOfView;
    public float aspect;
}

public class CameraManager
{
    public static CameraManager Instance { get; } = new CameraManager();
    private List<CachedCameraData> allCameraData = new List<CachedCameraData>();

    // TODO: Invoke in AdditionalCameraData.OnEnable.
    public CachedCameraData AddCamera(Camera camera)
    {
        foreach (CachedCameraData cameraData in allCameraData)
        {
            if (cameraData.camera == camera)
            {
                Debug.LogError($"Camera already added!", camera);
                return cameraData;
            }
        }

        CachedCameraData newCameraData = new CachedCameraData();
        newCameraData.dirtyState = CameraDirtyState.All;
        newCameraData.transform = camera.transform;
        newCameraData.position = newCameraData.transform.position;
        newCameraData.rotation = newCameraData.transform.rotation;
        newCameraData.localScale = newCameraData.transform.localScale;
        newCameraData.fieldOfView = camera.fieldOfView;
        allCameraData.Add(newCameraData);
        return newCameraData;
    }

    public void RemoveCamera(Camera camera)
    {
        for (var index = 0; index < allCameraData.Count; index++)
        {
            var cameraData = allCameraData[index];
            if (cameraData.camera == camera)
            {
                allCameraData.RemoveAt(index);
                return;
            }
        }
    }

    public CachedCameraData GetCameraData(Camera camera)
    {
        foreach (CachedCameraData cachedCameraData in allCameraData)
        {
            if (cachedCameraData.camera == camera)
            {
                return cachedCameraData;
            }
        }

        return AddCamera(camera);
    }

    public void LateUpdate()
    {
        #region Update cached variables, and collect dirty state.

        foreach (CachedCameraData cameraData in allCameraData)
        {
            cameraData.dirtyState = default;

            Vector3 newPosition = cameraData.transform.position;
            if (newPosition != cameraData.position)
            {
                cameraData.position = newPosition;
                cameraData.dirtyState |= CameraDirtyState.Position;
            }

            Quaternion newRotation = cameraData.transform.rotation;
            if (newRotation != cameraData.rotation)
            {
                cameraData.rotation = newRotation;
                cameraData.dirtyState |= CameraDirtyState.Rotation;
            }

            Vector3 newLocalScale = cameraData.transform.localScale;
            if (newLocalScale != cameraData.localScale)
            {
                cameraData.localScale = newLocalScale;
                cameraData.dirtyState |= CameraDirtyState.LocalScale;
            }

            float newFieldOfView = cameraData.camera.fieldOfView;
            if (Math.Abs(newFieldOfView - cameraData.fieldOfView) > 1e-4f)
            {
                cameraData.fieldOfView = newFieldOfView;
                cameraData.dirtyState |= CameraDirtyState.FieldOfView;
            }

            float newAspect = cameraData.camera.aspect;
            if (Math.Abs(newAspect - cameraData.aspect) > 1e-4f)
            {
                cameraData.aspect = newAspect;
                cameraData.dirtyState |= CameraDirtyState.Aspect;
            }
        }

        #endregion
    }
}