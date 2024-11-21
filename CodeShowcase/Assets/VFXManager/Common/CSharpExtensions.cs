using System;
using System.Collections.Generic;

public static class CSharpExtensions
{
    public static TValue ForceGetValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) where TValue : new()
    {
        if (!dictionary.TryGetValue(key, out var value))
        {
            dictionary[key] = value = new TValue();
        }

        return value;
    }
}

/// <summary>
/// Callback always invoke safely even exception occured.
/// </summary>
public struct CallbackScope : IDisposable
{
    private readonly Action callback;
    
    public CallbackScope(Action callback)
    {
        this.callback = callback;
    }

    public void Dispose()
    {
        callback?.Invoke();
    }
}