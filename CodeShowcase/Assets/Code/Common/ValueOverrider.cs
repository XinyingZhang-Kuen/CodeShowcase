using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Faster string compare type, but it uses more memory.
/// </summary>
public struct ValueOverriderSource
{
    private static readonly Dictionary<string, int> exists = new Dictionary<string, int>();
    private static int increasingID;
    
#if UNITY_EDITOR
    // Debug only.
    public readonly string name;
#endif
    private readonly int id;

    public ValueOverriderSource(string name)
    {
#if UNITY_EDITOR
        this.name = name;
#endif
        if (!exists.TryGetValue(name, out id))
        {
            id = increasingID++;
        }
    }

    public static bool operator ==(ValueOverriderSource a, ValueOverriderSource b)
    {
        return a.id == b.id;
    }

    public static bool operator !=(ValueOverriderSource a, ValueOverriderSource b)
    {
        return a.id != b.id;
    }
    
    public bool Equals(ValueOverriderSource other)
    {
        return id == other.id;
    }

    public override bool Equals(object obj)
    {
        return obj is ValueOverriderSource other && other.id == id;
    }
    
    public override int GetHashCode()
    {
        return id;
    }
}

public class ValueOverrider<T>
{
    private struct Layer
    {
        public ValueOverriderSource source;
        public int priority;
        public T value;
        public int counter;
    }

    private readonly List<Layer> _layers = new();
    private bool _isDirty;
    public bool IsDirty => _isDirty;
    public int LayerCount => _layers.Count;

    public T GetValue()
    {
        if (_layers.Count == 0)
        {
            Debug.LogError("Overrider should have one layer of value at least!");
            return default;
        }
        return _layers[_layers.Count - 1].value;
    }

    public bool Pop(ValueOverriderSource source, bool forceRemove)
    {
        for (int i = 0; i < _layers.Count; i++)
        {
            Layer layer = _layers[i];
            if (layer.source == source || forceRemove)
            {
                layer.counter--;
                if (layer.counter == 0 || forceRemove)
                {
                    _layers.RemoveAt(i);
                    if (i == _layers.Count - 1)
                    {
                        _isDirty = true;
                    }
                }
                return true;
            }
        }

        return false;
    }

    public void Push(ValueOverriderSource source, T value, int priority = 0)
    {
        int insertLocation = 0;
        for (int index = _layers.Count - 1; index >= 0; index--)
        {
            Layer layer = _layers[index];
            if (layer.source == source)
            {
                // TODO: Use ref to optimize performance.
                layer.priority = priority;
                layer.value = value;
                layer.counter++;
                _layers[index] = layer;
                return;
            }

            if (priority >= layer.priority)
            {
                insertLocation = index + 1;
            }
        }

        Layer newLayer = new Layer()
        {
            source = source,
            priority = priority,
            value = value,
            counter = 1
        };
        _layers.Insert(insertLocation, newLayer);
        if (insertLocation == _layers.Count)
        {
            _isDirty = true;
        }
    }

    /// <summary>
    /// Set overriden value of source without increase counter. 
    /// </summary>
    /// <param name="source"></param>
    /// <param name="newValue"></param>
    public void Set(ValueOverriderSource source, T newValue)
    {
        for (int index = _layers.Count - 1; index >= 0; index--)
        {
            // TODO: Use ref to optimize performance.
            Layer layer = _layers[index];
            if (layer.source == source)
            {
                layer.value = newValue;
                _layers[index] = layer;
                if (index == _layers.Count - 1)
                {
                    _isDirty = true;
                }
                break;
            }
        }
    }

    public void ClearDirty()
    {
        _isDirty = false;
    }

    public void Clear()
    {
        _layers.Clear();
    }
}