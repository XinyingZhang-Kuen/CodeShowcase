using System;
using System.Collections.Generic;
using UnityEngine;

public class VFXSystemManager
{
    private Dictionary<int, VFXSystem>[] allSystems = new Dictionary<int, VFXSystem>[(int)VFXSystemID.Count]
    {
        new Dictionary<int, VFXSystem>(),
        new Dictionary<int, VFXSystem>(),
    };

    // DO NOT delete the initial count to ensure all constructors of new systems are added.
    private static readonly Func<VFXSystem>[] Constructors = new Func<VFXSystem>[(int)VFXSystemID.Count]
    {
        () => new VFXMaterialSystem(),
        () => new VFXScreenEffectSystem(),
    };

    public static VFXSystemManager Instance { get; } = new VFXSystemManager();
    
    public void Init()
    {
        int count = (int)VFXSystemID.Count;
        for (int i = 0; i < count; i++)
        {
            allSystems[i] = new Dictionary<int, VFXSystem>();
        }
    }
    
    public void UpdateAllSystems()
    {
        foreach (Dictionary<int, VFXSystem> subSystems in allSystems)
        {
            foreach (VFXSystem system in subSystems.Values)
            {
                system.UpdateStates();
            }
        }
    }

    public bool Add(VFXConfig config, GameObject target, out VFXHandler vfxID)
    {
        if ((config.Features & VFXSystemFeatures.Binding) != 0
            && target == null || !target.gameObject)
        {
            Debug.LogError("Callable is invalid while config requires a caller.");
            vfxID = default;
            return false;
        }

        int callerID = (config.Features & VFXSystemFeatures.Binding) != 0
            ? target.gameObject.GetInstanceID()
            : 0;
        Dictionary<int, VFXSystem> systems = allSystems[(int)config.SystemID];
        if (!systems.TryGetValue(callerID, out VFXSystem system))
        {
            systems[callerID] = system = CreateInstance(config.SystemID, target);
        }

        return system.Add(config, target, out vfxID);
    }

    private static VFXSystem CreateInstance(VFXSystemID id, GameObject target)
    {
        var system = Constructors[(int)id]();
        system.Bind(target);
        system.Init();
        return system;
    }

    public bool Remove(VFXHandler handler)
    {
        Dictionary<int, VFXSystem> systems = allSystems[(int)handler.systemID];
        if (!systems.TryGetValue(handler.targetID, out VFXSystem system))
        {
            // Target may be released so don't have to log here. 
            return false;
        }

        bool removed = system.Remove(handler.vfxID);
        return removed;
    }
}

public struct VFXHandler
{
    public readonly int targetID;
    public readonly VFXSystemID systemID;
    public readonly int vfxID;

    public VFXHandler(VFXSystemID systemID, int vfxID, int targetID)
    {
        this.systemID = systemID;
        this.vfxID = vfxID;
        this.targetID = targetID;
    }
}