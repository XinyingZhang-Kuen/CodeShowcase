using UnityEngine;

public class MonoGameController : MonoBehaviour
{
    private void Start()
    {
        VFXSystemManager.Instance.Init();
    }

    private void LateUpdate()
    {
        VFXSystemManager.Instance.UpdateAllSystems();
        
        // This is supposed to be called before material submitting of URP (culling, exactly).
        // But this is only a showcase, I am not going to modify the render pipeline for now.
        EntityManager.instance.BeforeRendering();
    }
}