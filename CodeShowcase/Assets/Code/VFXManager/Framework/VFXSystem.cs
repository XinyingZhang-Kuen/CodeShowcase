using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public abstract class VFXSystem
{
    public const int VFXStageCount = 3;
    protected GameObject target { get; private set; }
    public abstract void UpdateStates();
    public abstract bool Add(VFXConfig config, GameObject target, out VFXHandler vfxID);
    public abstract bool Remove(int vfxID);
    public abstract void Init();
    public void Bind(GameObject target)
    {
        this.target = target;
    }
}

[Flags]
public enum VFXSystemFeatures
{
    None = 0,
    Staging = 1 << 0,
    Binding = 1 << 1,
}

/// <summary>
/// VFX system framework.
/// - Support Time scale
/// - Support fading in, fading out and looping.
/// </summary>
public abstract class VFXSystem<TVFXConfig, TVFXState> : VFXSystem
    where TVFXConfig : VFXConfig
    where TVFXState : VFXState<TVFXConfig>, new()
{
    private static int _increasingID;
    // TODO: Consider implement a index-cached list to save performance of fetching and looping.
    private readonly Dictionary<int, TVFXState> _states = new();
    private static List<TVFXState> removeList = new();

    public sealed override bool Add(VFXConfig config, GameObject target, out VFXHandler handler)
    {
        TVFXState state = GenericPool<TVFXState>.Get();
        state.config = config as TVFXConfig;
        int vfxID = state.id = _increasingID++;
        _states.Add(state.id, state);
        int targetID = target ? target.GetInstanceID() : 0;
        handler = new VFXHandler(config.SystemID, vfxID, targetID);
        OnStateInit(state);
        OnPlayStart(state);
        state.stage = VFXStage.FadingIn;
        return true;
    }

    public sealed override bool Remove(int vfxID)
    {
        if (_states.TryGetValue(vfxID, out TVFXState state))
        {
            RemoveImpl(state);
            return true;
        }

        return false;
    }

    private void RemoveImpl(TVFXState state)
    {
        _states.Remove(state.id);
        OnClear(state);
        state.Clear();
        GenericPool<TVFXState>.Release(state);
    }

    public sealed override void UpdateStates()
    {
        float scaledDeltaTime = Time.deltaTime;
        float unscaledDeltaTime = Time.unscaledDeltaTime;

        foreach (TVFXState state in _states.Values)
        {
            state.timeInStage += state.config.timeScaled ? unscaledDeltaTime : scaledDeltaTime;
            
            if ((state.config.Features & VFXSystemFeatures.Staging) != 0)
            {
                if (state.stage == VFXStage.FadingIn)
                {
                    if (state.timeInStage >= state.config.fadeInDuration)
                    {
                        state.timeInStage -= state.config.fadeInDuration;
                        state.stage++;
                    }
                }
                if (state.stage == VFXStage.Looping)
                {
                    if (state.timeInStage >= state.config.loopDuration)
                    {
                        state.timeInStage -= state.config.loopDuration;
                        state.loopedTimes++;
                        if (state.loopedTimes >= state.config.loopTimes)
                        {
                            state.stage++;
                        }
                    }
                }
                if (state.stage == VFXStage.FadingOut)
                {
                    if (state.timeInStage >= state.config.fadeOutDuration)
                    {
                        state.timeInStage -= state.config.fadeOutDuration;
                        state.stage++;
                    }
                }
            }

            if (state.stage <= VFXStage.FadingOut)
            {
                OnPerform(state);
            }
            else
            {
                removeList.Add(state);
            }
        }

        foreach (TVFXState state in removeList)
        {
            RemoveImpl(state);
        }

        removeList.Clear();
    }

    protected virtual void OnStateInit(TVFXState state)
    {
        
    }

    protected virtual void OnPerform(TVFXState state)
    {
        // Implemented in sub classes.
    }

    protected virtual void OnPlayStart(TVFXState state)
    {
        
    }

    protected virtual void OnClear(TVFXState state)
    {
        
    }
}

/// <summary>
/// When the system does not support staging, use FadingIn when vfx is playing.
/// </summary>
public enum VFXStage
{
    Inactive = -1,
    FadingIn,
    Looping,
    FadingOut,
}

public abstract class VFXConfig : ScriptableObject
{
    public abstract VFXSystemFeatures Features { get; }
    public abstract VFXSystemID SystemID { get; }
    public abstract bool IsValid { get; }
    [Min(0)]
    public float fadeInDuration = 1;
    [Min(0)]
    public int loopTimes = 1;
    [Min(0)]
    public float loopDuration = 1;
    [Min(0)]
    public float fadeOutDuration = 1;
    public bool timeScaled = true;
}

public abstract class VFXState<TVFXConfig> where TVFXConfig : VFXConfig
{
    public int id;
    public TVFXConfig config;
    public VFXStage stage;
    public float timeInStage;
    public int loopedTimes;

    public float StageDuration
    {
        get
        {
            switch (stage)
            {
                case VFXStage.Inactive:
                    return -1;
                case VFXStage.FadingIn:
                    return config.fadeInDuration;
                case VFXStage.Looping:
                    return config.loopDuration;
                case VFXStage.FadingOut:
                    return config.fadeOutDuration;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
        
    public virtual void Clear()
    {
        id = -1;
        config = null;
        stage = VFXStage.Inactive;
        timeInStage = 0;
        loopedTimes = 0;
    }
}

public enum VFXSystemID
{
    VFXMaterialSystem,
    VFXScreenEffectSystem,
    Count,
}