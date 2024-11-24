using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using ShaderPropertyType = UnityEditor.ShaderUtil.ShaderPropertyType;

public class PropertyInfo
{
    public string name;
    public int id;
    public ShaderPropertyType type;
    public string desc;
    public ShaderLibrary.ShaderMask supportedShaderFlags;
}

public class KeywordInfo
{
    public string keyword;
    public ShaderLibrary.ShaderMask shaderMask;

    public KeywordInfo(string keyword)
    {
        this.keyword = keyword;
    }
}
    
public class PassInfo
{
    public string passName;
    public ShaderLibrary.ShaderMask shaderMask;
        
    public PassInfo(string passName)
    {
        this.passName = passName;
    }
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
            hashCode = (id << 16) | (int)type;
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
        public readonly HashSet<Shader> supportedShader = new();
        public readonly HashSet<string> keywords = new();
        public readonly HashSet<string> passNames = new();
        public readonly List<PropertyInfo>[] propertiesOfType = new List<PropertyInfo>[ShaderPropertyTypeCount]
        {
            new List<PropertyInfo>(),
            new List<PropertyInfo>(),
            new List<PropertyInfo>(),
            new List<PropertyInfo>(),
            new List<PropertyInfo>(),
            new List<PropertyInfo>(),
        };
    }
    
    private static bool _initialized;
    public const int ShaderPropertyTypeCount = 6;
    private static readonly Dictionary<Shader, CacheShaderInfo> ShaderCache = new Dictionary<Shader, CacheShaderInfo>();
    private static int _increasingShaderID;
    private static readonly Dictionary<Shader, int> ShaderToShaderID = new();
    private static readonly Dictionary<PropertyKey, PropertyInfo> propertyMap = new();
    private static readonly Dictionary<int, string> propertyNameMap = new Dictionary<int, string>();
    private static readonly Dictionary<string, KeywordInfo> keywords = new();
    private static readonly Dictionary<string, PassInfo> passes = new();
    private static readonly List<PropertyInfo>[] propertyOfTypes = new List<PropertyInfo>[ShaderPropertyTypeCount]
    {
        new List<PropertyInfo>(),
        new List<PropertyInfo>(),
        new List<PropertyInfo>(),
        new List<PropertyInfo>(),
        new List<PropertyInfo>(),
        new List<PropertyInfo>(),
    };

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
        ShaderMask shaderMask = (ShaderMask)(1ul << shaderID);
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
                cachedShaderInfo.propertiesOfType[(int)type].Add(propertyInfo);
                propertyOfTypes[(int)type].Add(propertyInfo);
            } 

            propertyInfo.supportedShaderFlags.Add((ShaderMask)(1ul << _increasingShaderID));
        }

        ShaderData shaderData = ShaderUtil.GetShaderData(shader);
        // Get keywords from SubShader 0 only, because there is a bug of ShaderUtil.GetPassKeywords.
        // int subPassCount = shaderData.SubshaderCount;
        // for (int subShaderIndex = 0; subShaderIndex < subPassCount; subShaderIndex++)
        int subShaderIndex = 0;
        {
            ShaderData.Subshader subShader = shaderData.GetSubshader(subShaderIndex);
            int passCount = subShader.PassCount;
            for (int passIndex = 0; passIndex < passCount; passIndex++)
            {
                ShaderData.Pass pass = subShader.GetPass(passIndex);
                string passName = pass.Name;
                cachedShaderInfo.passNames.Add(passName);
                PassIdentifier passIdentifier = new PassIdentifier((uint)subShaderIndex, (uint)passIndex);
                LocalKeyword[] keywords = ShaderUtil.GetPassKeywords(shader, passIdentifier);
                foreach (LocalKeyword localKeyword in keywords)
                {
                    cachedShaderInfo.keywords.Add(localKeyword.name);
                }
            }
        }
 
        foreach (string keyword in cachedShaderInfo.keywords)
        {
            if (!keywords.TryGetValue(keyword, out KeywordInfo keywordInfo))
            {
                keywords[keyword] = keywordInfo = new KeywordInfo(keyword);
            }
            keywordInfo.shaderMask.Add(shaderMask);
        }
        
        foreach (string passName in cachedShaderInfo.passNames)
        {
            if (!passes.TryGetValue(passName, out PassInfo passInfo))
            {
                passes[passName] = passInfo = new PassInfo(passName);
            }
            passInfo.shaderMask.Add(shaderMask);
        }
        
        _increasingShaderID++;

        return shaderID;
    }

    public static IReadOnlyCollection<PropertyInfo> GetPropertiesOfType(Shader shader, ShaderPropertyType type)
    {
        return ShaderCache.TryGetValue(shader, out CacheShaderInfo cacheShaderInfo)
            ? cacheShaderInfo.propertiesOfType[(int)type]
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

        // Quick check for all properties of type.
        List<PropertyInfo> propertyInfos = propertyOfTypes[(int)type];
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
                if (shader != null)
                {
                    _value |= 1ul << GetShaderID(shader);
                }
            }
        }

        public ShaderMask(Shader shader)
        {
            _value = 1ul << GetShaderID(shader);
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

    public static int PropertyToID(string name)
    {
        int id = Shader.PropertyToID(name);
        propertyNameMap[id] = name;
        return id;
    }

    public static bool IdToProperty(int id, string name)
    {
        return propertyNameMap.TryGetValue(id, out name);
    }

    public static void GetKeywords(ShaderMask shaderMask, List<KeywordInfo> keywordInfos)
    {
        foreach (KeywordInfo keywordInfo in keywords.Values)
        {
            if (keywordInfo.shaderMask.ContainsAny(shaderMask))
            {
                keywordInfos.Add(keywordInfo);
            }
        }
    }

    public static void GetPasses(ShaderMask shaderMask, List<PassInfo> passInfos)
    {
        foreach (PassInfo passInfo in passes.Values)
        {
            if (passInfo.shaderMask.ContainsAny(shaderMask))
            {
                passInfos.Add(passInfo);
            }
        }
    }
}