using System;
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

[CustomEditor(typeof(VFXMaterialConfig))]
public class VFXMaterialConfigEditor : OdinEditor
{
    public static VFXMaterialConfigEditor Current { get; private set; }
    private ShaderLibrary.ShaderMask shaderMask;
    private GUIContent[][] propertyNames = new GUIContent[ShaderLibrary.ShaderPropertyTypeCount][]
    {
        Array.Empty<GUIContent>(),
        Array.Empty<GUIContent>(),
        Array.Empty<GUIContent>(),
        Array.Empty<GUIContent>(),
        Array.Empty<GUIContent>(),
        Array.Empty<GUIContent>(),
    };

    public override void OnInspectorGUI()
    {
        VFXMaterialConfig config = target as VFXMaterialConfig;
        Current = this;
        if (!Current)
            return;

        ShaderLibrary.ShaderMask newShaderMask = new ShaderLibrary.ShaderMask(config.supportedShaders);

        if (newShaderMask != shaderMask)
        {
            shaderMask = newShaderMask;
            UpdatePropertyCache();
        }

        base.OnInspectorGUI();

        Current = null;
    }

    private void UpdatePropertyCache()
    {
        List<PropertyInfo> properties = ListPool<PropertyInfo>.Get();
        using (new CallbackScope(() => ListPool<PropertyInfo>.Release(properties)))
        {
            for (int typeIndex = 0; typeIndex < ShaderLibrary.ShaderPropertyTypeCount; typeIndex++)
            {
                ShaderUtil.ShaderPropertyType type = (ShaderUtil.ShaderPropertyType)typeIndex;
                ShaderLibrary.GetPropertiesOfTypeAndShader(shaderMask, type, properties);
                propertyNames[typeIndex] = new GUIContent[properties.Count];
                for (var propertyIndex = 0; propertyIndex < properties.Count; propertyIndex++)
                {
                    PropertyInfo propertyInfo = properties[propertyIndex];
                    propertyNames[typeIndex][propertyIndex] = new GUIContent(propertyInfo.name, propertyInfo.desc);
                }
            }
        }
    }
    
    public GUIContent[] GetPropertyLabels(ShaderUtil.ShaderPropertyType type)
    {
        return propertyNames[(int)type];
    }

    public int GetPropertyIndex(ShaderUtil.ShaderPropertyType type, string propertyName)
    {
        for (var i = 0; i < propertyNames[(int)type].Length; i++)
        {
            var content = propertyNames[(int)type][i];
            if (content.text == propertyName)
                return i;
        }

        return 0;
    }

}