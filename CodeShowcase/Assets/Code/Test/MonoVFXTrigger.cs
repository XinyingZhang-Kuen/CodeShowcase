using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class MonoVFXTrigger : MonoBehaviour, IVFXTrigger
{
    [OnValueChanged(nameof(CheckIfConfigEditedWhenItsRunning))]
    public List<VFXConfig> configs = new();
    private readonly List<VFXHandler> _handlers = new();
    
    private void OnEnable()
    {
        foreach (VFXConfig config in configs)
        {
            VFXSystemManager.Instance.Add(config, this, out VFXHandler handler);
            _handlers.Add(handler);
        }
    }

    private void OnDisable()
    {
        for (var index = 0; index < configs.Count; index++)
        {
            VFXConfig config = configs[index];
            VFXHandler handler = _handlers[index];
            VFXSystemManager.Instance.Remove(config, ref handler, gameObject);
        }
        _handlers.Clear();
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