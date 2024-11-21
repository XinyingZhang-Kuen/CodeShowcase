using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ShaderPropertyType = UnityEditor.ShaderUtil.ShaderPropertyType;

public class PropertyInfo
{
    public string name;
    public int id;
    public ShaderPropertyType type;
    public string desc;
    public ShaderLibrary.ShaderMask supportedShaderFlags;
}

/// <summary>
/// High performance library with optimized shader mask.
/// </summary>
public class ShaderLibrary
{
    private struct PropertyKey
    {
        private int hashCode;

        public PropertyKey(int id, ShaderPropertyType type)
        {
            hashCode = (id << 16) & (int)type;
        }

        public override int GetHashCode()
        {
            return hashCode;
        }
    }

    private class CacheShaderInfo
    {
        public readonly Dictionary<string, PropertyInfo> nameToProperty = new();
        public readonly Dictionary<int, PropertyInfo> idToProperty = new();
        public readonly List<PropertyInfo>[] typeToProperties = new List<PropertyInfo>[ShaderPropertyTypeCount];
        public readonly HashSet<Shader> supportedShader = new();
    }
    
    private static bool _initialized;
    public const int ShaderPropertyTypeCount = 6;
    private static readonly Dictionary<Shader, CacheShaderInfo> ShaderCache = new Dictionary<Shader, CacheShaderInfo>();
    private static int _increasingShaderID;
    private static readonly Dictionary<Shader, int> ShaderToShaderID = new();
    private static readonly Dictionary<PropertyKey, PropertyInfo> propertyMap = new();
    private static readonly Dictionary<ShaderPropertyType, List<PropertyInfo>> propertyOfTypes = new();

    private static void InitializeIfNotYet()
    {
        if (_initialized)
            return;

        string[] guids = AssetDatabase.FindAssets("t:shader");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            CacheShader(shader);
        }

        _initialized = true;
    }

    private static int GetShaderID(Shader shader)
    {
        if (ShaderToShaderID.TryGetValue(shader, out int id))
            return id;
        id = CacheShader(shader);
        return id;
    }

    private static int CacheShader(Shader shader)
    {
        CacheShaderInfo cachedShaderInfo = new CacheShaderInfo();
        int shaderID = ShaderToShaderID[shader] = _increasingShaderID;
        int propertyCount = ShaderUtil.GetPropertyCount(shader);
        for (int propertyIndex = 0; propertyIndex < propertyCount; propertyIndex++)
        {
            string name = ShaderUtil.GetPropertyName(shader, propertyIndex);
            int id = Shader.PropertyToID(name);
            ShaderPropertyType type = ShaderUtil.GetPropertyType(shader, propertyIndex);
            PropertyKey propertyKey = new PropertyKey(id, type);
            if (!propertyMap.TryGetValue(propertyKey, out PropertyInfo propertyInfo))
            {
                propertyMap[propertyKey] = propertyInfo = new PropertyInfo();
                propertyInfo.id = id;
                propertyInfo.name = name;
                propertyInfo.type = type;
                propertyInfo.desc = ShaderUtil.GetPropertyDescription(shader, propertyIndex);
                cachedShaderInfo.idToProperty[id] = propertyInfo;
                cachedShaderInfo.nameToProperty[name] = propertyInfo;
                List<PropertyInfo> list = cachedShaderInfo.typeToProperties[(int)type] ??= new List<PropertyInfo>();
                list.Add(propertyInfo);
                propertyOfTypes.ForceGetValue(type).Add(propertyInfo);
            }

            propertyInfo.supportedShaderFlags.Add((ShaderMask)(1ul << _increasingShaderID));
            _increasingShaderID++;
        }

        return shaderID;
    }

    public static IReadOnlyCollection<PropertyInfo> GetPropertiesOfType(Shader shader, ShaderPropertyType type)
    {
        return ShaderCache.TryGetValue(shader, out CacheShaderInfo cacheShaderInfo)
            ? cacheShaderInfo.typeToProperties[(int)type]
            : Array.Empty<PropertyInfo>();
    }

    /// <summary>
    /// Get multiple property list in O(n).
    /// </summary>
    /// <param name="shaders"></param>
    /// <param name="type"></param>
    /// <param name="properties"></param>
    public static void GetPropertiesOfTypeAndShader(ShaderMask shaderMask, ShaderPropertyType type,
        List<PropertyInfo> properties)
    {
        InitializeIfNotYet();
        
        if (!propertyOfTypes.TryGetValue(type, out List<PropertyInfo> propertyInfos))
            return;

        // Quick check for all properties of type.
        foreach (PropertyInfo propertyInfo in propertyInfos)
        {
            if (propertyInfo.supportedShaderFlags.ContainsAny(shaderMask))
            {
                properties.Add(propertyInfo);
            }
        }
    }

    /// <summary>
    /// *** DO NOT try to serialize it! Value changed when shader or C# recompiled.
    /// </summary>
    public struct ShaderMask
    {
        public override bool Equals(object obj)
        {
            return obj is ShaderMask other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        private ulong _value;

        public ShaderMask(IEnumerable<Shader> shaders)
        {
            _value = 0;
            foreach (Shader shader in shaders)
            {
                _value |= 1ul << GetShaderID(shader);
            }
        }

        public bool ContainsAny(ShaderMask mask)
        {
            return (mask._value & _value) != 0ul;
        }

        public void Add(Shader shader)
        {
            _value |= 1ul << GetShaderID(shader);
        }

        public void Add(ShaderMask shaderMask)
        {
            _value |= shaderMask._value;
        }

        public void Remove(Shader shader)
        {
            _value &= ~(1ul << GetShaderID(shader));
        }

        public void Remove(ShaderMask shaderMask)
        {
            _value &= ~shaderMask._value;
        }

        public bool Contains(Shader shader)
        {
            return (_value & (1ul << GetShaderID(shader))) != 0ul;
        }

        public bool IsEmpty()
        {
            return _value == 0ul;
        }

        public static explicit operator ShaderMask(ulong mask)
        {
            return new ShaderMask() { _value = mask };
        }
        
        public static bool operator ==(ShaderMask a, ShaderMask b)
        {
            return a._value == b._value;
        }

        public static bool operator !=(ShaderMask a, ShaderMask b)
        {
            return a._value != b._value;
        }
        
        public bool Equals(ShaderMask other)
        {
            return _value == other._value;
        }
    }
}