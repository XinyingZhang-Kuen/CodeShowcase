using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Use this scope to ensure the release of list never skipped even there's a exception in the scope.
/// *** Note that scope can only be used within a method, unless you know what you are doing.
/// *** The component list will be disposed at the moment when the running out of the scope "{}", which means that the lifetime of a struct is ended.
/// </summary>
/// <typeparam name="T"></typeparam>
public struct GetComponentsInChildrenScope<T> : IDisposable where T : Component
{
    public readonly List<T> components;

    public GetComponentsInChildrenScope(GameObject gameObject, bool includeInactive)
    {
        components = ListPool<T>.Get();
        gameObject.GetComponentsInChildren<T>(includeInactive, components);
    }
    
    public void Dispose()
    {
        ListPool<T>.Release(components);
    }
}