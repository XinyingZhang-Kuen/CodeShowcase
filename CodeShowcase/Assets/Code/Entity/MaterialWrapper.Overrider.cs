using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// TODO:
/// - Custom constant buffer for dynamic parameters to optimize performance.
/// - Implement material pools.
/// </summary>
public partial class MaterialWrapper
{
    private static readonly ValueOverriderSource BaseValueSource = new("base");

    // TODO: It would be faster to use List<(int, ValueOverrider<int>)> instead,
    // because the frequency of foreach is much more than add/remove,
    // and the size of these containers will not be greater than 10 commonly (it has a removal for which are dirty and empty.)
    private readonly Dictionary<int, ValueOverrider<int>> _intOverriders = new();
    private readonly Dictionary<int, ValueOverrider<float>> _floatOverriders = new();
    private readonly Dictionary<int, ValueOverrider<Vector4>> _vectorOverriders = new();
    private readonly Dictionary<int, ValueOverrider<Color>> _colorOverriders = new();
    private readonly Dictionary<int, ValueOverrider<Texture>> _textureOverriders = new();
    private readonly Dictionary<string, ValueOverrider<bool>> _keywordOverriders = new();
    private readonly Dictionary<string, ValueOverrider<bool>> _passOverriders = new();

    // *** Do NOT use pooling here because there will be redundant checking which cost O(n) by the size of the pool.
    private static readonly List<int> RemoveListInt = new();
    private static readonly List<string> RemoveListString = new();

    public ValueOverrider<int> GetOverriderInt(VFXMaterialModifier modifier)
    {
        if (!_intOverriders.TryGetValue(modifier.id, out var overrider))
        {
            float floatValue = instancedMaterial.GetFloat(modifier.id);
            int intValue = Unsafe.PointerCast<float, int>(floatValue);
            _intOverriders[modifier.id].Push(BaseValueSource, intValue, -1);
            _intOverriders[modifier.id] = overrider = GenericPool<ValueOverrider<int>>.Get();
        }

        return overrider;
    }

    public ValueOverrider<float> GetOverriderFloat(VFXMaterialModifier modifier)
    {
        if (!_floatOverriders.TryGetValue(modifier.id, out var overrider))
        {
            float value = instancedMaterial.GetFloat(modifier.id);
            _floatOverriders[modifier.id].Push(BaseValueSource, value, -1);
            _floatOverriders[modifier.id] = overrider = GenericPool<ValueOverrider<float>>.Get();
        }

        return overrider;
    }

    public ValueOverrider<Vector4> GetOverriderVector(VFXMaterialModifier modifier)
    {
        if (!_vectorOverriders.TryGetValue(modifier.id, out var overrider))
        {
            Vector4 value = instancedMaterial.GetVector(modifier.id);
            _vectorOverriders[modifier.id] = overrider = GenericPool<ValueOverrider<Vector4>>.Get();
            overrider.Push(BaseValueSource, value, -1);
        }

        return overrider;
    }

    public ValueOverrider<Color> GetOverriderColor(VFXMaterialModifier modifier)
    {
        if (!_colorOverriders.TryGetValue(modifier.id, out var overrider))
        {
            Color value = instancedMaterial.GetColor(modifier.id);
            _colorOverriders[modifier.id] = overrider = GenericPool<ValueOverrider<Color>>.Get();
            overrider.Push(BaseValueSource, value, -1);
        }

        return overrider;
    }

    public ValueOverrider<Texture> GetOverriderTexture(VFXMaterialModifier modifier)
    {
        if (!_textureOverriders.TryGetValue(modifier.id, out var overrider))
        {
            _textureOverriders[modifier.id] = overrider = GenericPool<ValueOverrider<Texture>>.Get();
            Texture value = instancedMaterial.GetTexture(modifier.id);
            overrider.Push(BaseValueSource, value, -1);
        }

        return overrider;
    }

    public ValueOverrider<bool> GetOverriderKeyword(VFXMaterialModifier modifier)
    {
        if (!_keywordOverriders.TryGetValue(modifier.propertyName, out var overrider))
        {
            _keywordOverriders[modifier.propertyName] = overrider = GenericPool<ValueOverrider<bool>>.Get();
            bool value = instancedMaterial.IsKeywordEnabled(modifier.propertyName);
            overrider.Push(BaseValueSource, value, -1);
        }

        return overrider;
    }

    public ValueOverrider<bool> GetOverriderPass(VFXMaterialModifier modifier)
    {
        if (!_passOverriders.TryGetValue(modifier.propertyName, out var overrider))
        {
            _passOverriders[modifier.propertyName] = overrider = GenericPool<ValueOverrider<bool>>.Get();
            bool value = instancedMaterial.GetShaderPassEnabled(modifier.propertyName);
            overrider.Push(BaseValueSource, value, -1);
        }

        return overrider;
    }

    /// <summary>
    /// Called once per frame only.
    /// Apply all dirty values to material, and remove overriders that is empty.
    /// </summary>
    private void ApplyOverriders()
    {
        #region Int

        // TODO: Use MaterialProperty.IsPointerCastInt to decide use pointer cast or not.
        foreach (KeyValuePair<int, ValueOverrider<int>> pair in _intOverriders)
        {
            ValueOverrider<int> overrider = pair.Value;
            if (overrider.IsDirty)
            {
                int propertyID = pair.Key;
                int intValue = overrider.GetValue();
                float floatValue = Unsafe.PointerCast<int, float>(intValue);
                instancedMaterial.SetFloat(propertyID, floatValue);
                if (overrider.LayerCount == 1)
                {
                    RemoveListInt.Add(propertyID);
                }
            }
        }

        foreach (int propertyID in RemoveListInt)
        {
            _intOverriders.Remove(propertyID);
        }

        RemoveListInt.Clear();

        #endregion

        #region Float

        foreach (KeyValuePair<int, ValueOverrider<float>> pair in _floatOverriders)
        {
            ValueOverrider<float> overrider = pair.Value;
            if (overrider.IsDirty)
            {
                int propertyID = pair.Key;
                float value = overrider.GetValue();
                instancedMaterial.SetFloat(propertyID, value);
                if (overrider.LayerCount == 1)
                {
                    RemoveListInt.Add(propertyID);
                }
            }
        }

        foreach (int propertyID in RemoveListInt)
        {
            _floatOverriders.Remove(propertyID);
        }

        RemoveListInt.Clear();

        #endregion

        #region Vector

        foreach (KeyValuePair<int, ValueOverrider<Vector4>> pair in _vectorOverriders)
        {
            int propertyID = pair.Key;
            ValueOverrider<Vector4> overrider = pair.Value;
            if (overrider.IsDirty)
            {
                Vector4 value = overrider.GetValue();
                instancedMaterial.SetVector(propertyID, value);
                if (overrider.LayerCount == 1)
                {
                    RemoveListInt.Add(propertyID);
                }
            }
        }

        foreach (int propertyID in RemoveListInt)
        {
            _vectorOverriders.Remove(propertyID);
        }

        RemoveListInt.Clear();

        #endregion

        #region Color

        foreach (KeyValuePair<int, ValueOverrider<Color>> pair in _colorOverriders)
        {
            int propertyID = pair.Key;
            ValueOverrider<Color> overrider = pair.Value;
            if (overrider.IsDirty)
            {
                Color value = overrider.GetValue();
                instancedMaterial.SetColor(propertyID, value);
                if (overrider.LayerCount == 1)
                {
                    RemoveListInt.Add(propertyID);
                }
            }
        }

        foreach (int propertyID in RemoveListInt)
        {
            _colorOverriders.Remove(propertyID);
        }

        RemoveListInt.Clear();

        #endregion

        #region Texture

        foreach (KeyValuePair<int, ValueOverrider<Texture>> pair in _textureOverriders)
        {
            int propertyID = pair.Key;
            ValueOverrider<Texture> overrider = pair.Value;
            if (overrider.IsDirty)
            {
                Texture value = overrider.GetValue();
                instancedMaterial.SetTexture(propertyID, value);
                if (overrider.LayerCount == 1)
                {
                    RemoveListInt.Add(propertyID);
                }
            }
        }

        foreach (int propertyID in RemoveListInt)
        {
            _textureOverriders.Remove(propertyID);
        }

        _textureOverriders.Clear();

        #endregion

        #region Keyword

        foreach (KeyValuePair<string, ValueOverrider<bool>> pair in _keywordOverriders)
        {
            string keyword = pair.Key;
            ValueOverrider<bool> overrider = pair.Value;
            if (overrider.IsDirty)
            {
                bool value = overrider.GetValue();
                if (value)
                {
                    instancedMaterial.EnableKeyword(keyword);
                }
                else
                {
                    instancedMaterial.DisableKeyword(keyword);
                }

                if (overrider.LayerCount == 1)
                {
                    RemoveListString.Add(keyword);
                }
            }
        }

        foreach (string keyword in RemoveListString)
        {
            _keywordOverriders.Remove(keyword);
        }

        _keywordOverriders.Clear();

        #endregion

        #region Pass

        foreach (KeyValuePair<string, ValueOverrider<bool>> pair in _passOverriders)
        {
            string passName = pair.Key;
            ValueOverrider<bool> overrider = pair.Value;
            if (overrider.IsDirty)
            {
                bool value = overrider.GetValue();
                instancedMaterial.SetShaderPassEnabled(passName, value);
            }

            if (overrider.LayerCount == 1)
            {
                RemoveListString.Add(passName);
            }
        }

        foreach (string keyword in RemoveListString)
        {
            _passOverriders.Remove(keyword);
        }

        RemoveListString.Clear();

        #endregion
    }

    private void ClearOverriders()
    {
        ReleaseOverriders(_intOverriders);
        ReleaseOverriders(_floatOverriders);
        ReleaseOverriders(_vectorOverriders);
        ReleaseOverriders(_colorOverriders);
        ReleaseOverriders(_textureOverriders);
        ReleaseOverriders(_keywordOverriders);
        ReleaseOverriders(_passOverriders);

        void ReleaseOverriders<TKey, T>(Dictionary<TKey, ValueOverrider<T>> dictionary)
        {
            Dictionary<TKey, ValueOverrider<T>>.ValueCollection values = dictionary.Values;
            foreach (ValueOverrider<T> value in values)
            {
                value.Clear();
                GenericPool<ValueOverrider<T>>.Release(value);
            }

            dictionary.Clear();
        }
    }
}