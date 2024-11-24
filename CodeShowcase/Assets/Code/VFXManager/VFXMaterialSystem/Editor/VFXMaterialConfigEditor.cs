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

    private GUIContent[] keywords = Array.Empty<GUIContent>();
    private GUIContent[] passes = Array.Empty<GUIContent>();

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
                properties.Clear();
            }

            List<KeywordInfo> keywordInfos = ListPool<KeywordInfo>.Get();
            ShaderLibrary.GetKeywords(shaderMask, keywordInfos);
            keywords = new GUIContent[keywordInfos.Count];
            for (int index = 0; index < keywordInfos.Count; index++)
            {
                var keywordInfo = keywordInfos[index];
                keywords[index] = new GUIContent(keywordInfo.keyword);
            }
            ListPool<KeywordInfo>.Release(keywordInfos);

            List<PassInfo> passInfos = ListPool<PassInfo>.Get();
            ShaderLibrary.GetPasses(shaderMask, passInfos);
            passes = new GUIContent[passInfos.Count];
            for (int index = 0; index < passInfos.Count; index++)
            {
                var keywordInfo = passInfos[index];
                passes[index] = new GUIContent(keywordInfo.passName);
            }
            ListPool<PassInfo>.Release(passInfos);
            
        }
    }
    
    public GUIContent[] GetPropertyLabels(ShaderUtil.ShaderPropertyType type)
    {
        return propertyNames[(int)type];
    }

    public GUIContent[] GetKeywordLabels()
    {
        return keywords;
    }

    public GUIContent[] GetPassNameLabels()
    {
        return passes;
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

    public int GetKeywordIndex(string name)
    {
        for (int index = 0; index < keywords.Length; index++)
        {
            GUIContent guiContent = keywords[index];
            if (guiContent.text == name)
            {
                return index;
            }
        }

        return 0;
    }

    public int GetPassIndex(string name)
    {
        for (int index = 0; index < passes.Length; index++)
        {
            GUIContent guiContent = passes[index];
            if (guiContent.text == name)
            {
                return index;
            }
        }

        return 0;
    }
}