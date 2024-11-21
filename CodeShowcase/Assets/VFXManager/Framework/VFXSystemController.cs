using UnityEngine;

public class VFXSystemController : MonoBehaviour
{
    private void Start()
    {
        VFXSystemManager.Instance.Init();
    }

    private void LateUpdate()
    {
        VFXSystemManager.Instance.UpdateAllSystems();
    }
}