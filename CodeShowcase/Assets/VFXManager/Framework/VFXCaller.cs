using System.Collections.Generic;
using UnityEngine;

public class VFXCaller : MonoBehaviour
{
    // [OnValueChanged("CheckIfConfigEditedWhenItsRunning")]
    public List<VFXConfig> configs = new List<VFXConfig>();
    private List<VFXHandler> handlers = new List<VFXHandler>();
    
    private void OnEnable()
    {
        foreach (VFXConfig config in configs)
        {
            VFXSystemManager.Instance.Add(config, this, out VFXHandler handler);
        }
    }

    private void OnDisable()
    {
        for (var index = 0; index < configs.Count; index++)
        {
            VFXConfig config = configs[index];
            VFXHandler handler = handlers[index];
            VFXSystemManager.Instance.Remove(config, handler, this);
        }
    }
    
#if UNITY_EDITOR
    private void CheckIfConfigEditedWhenItsRunning()
    {
        if (Application.isPlaying && enabled)
        {
            Debug.LogError("Trying to edit the config list while VFXCaller is running!");
        }
    }
#endif
}