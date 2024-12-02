using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class VFXMaterialShowcaseTrigger
{
    [SerializeField] private GameObject _gameObject;
    public List<VFXConfig> configs = new();
    private readonly List<VFXHandler> _handlers = new();

    public GameObject gameObject
    {
        get => _gameObject;
        set => _gameObject = value;
    }

    public void Play(GameObject target)
    {
        if (gameObject)
        {
            foreach (VFXConfig config in configs)
            {
                VFXSystemManager.Instance.Add(config, target, out VFXHandler vfxID);
                _handlers.Add(vfxID);
            }
        }
    }

    public void Stop()
    {
        for (var index = 0; index < _handlers.Count; index++)
        {
            VFXHandler vfxID = _handlers[index];
            VFXSystemManager.Instance.Remove(vfxID);
        }

        _handlers.Clear();
    }
}

public class MonoVFXMaterialShowcase : MonoBehaviour
{
    public Button playButton;
    public Button stopButton;
    public VFXMaterialShowcaseTrigger trigger = new VFXMaterialShowcaseTrigger();

    void Start()
    {
        playButton.onClick.AddListener(OnPlayButtonClicked);
        stopButton.onClick.AddListener(OnStopButtonClicked);
    }

    private void OnDestroy()
    {
        playButton.onClick.RemoveListener(OnPlayButtonClicked);
        stopButton.onClick.RemoveListener(OnStopButtonClicked);
    }

    private void OnPlayButtonClicked()
    {
        trigger.Play(trigger.gameObject);
    }

    private void OnStopButtonClicked()
    {
        trigger.Stop();
    }
}