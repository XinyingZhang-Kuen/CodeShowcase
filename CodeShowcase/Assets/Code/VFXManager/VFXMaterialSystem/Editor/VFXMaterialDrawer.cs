using UnityEditor;
using UnityEngine;
using ShaderPropertyType = UnityEditor.ShaderUtil.ShaderPropertyType;

namespace VFXManager.Editor
{
    public abstract class VFXMaterialModifierDrawer : PropertyDrawer
    {
        private static readonly GUIContent supoortedCurveModeDesc = new("Curve", "Toggle between constant value mode and curve mode."); 
        private static readonly GUIContent unsupoortedCurveModeDesc = new("Curve", "Curve mode is not supported for textures."); 
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            VFXMaterialConfigEditor editor = VFXMaterialConfigEditor.Current;

            #region Label, property name

            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
            SerializedProperty nameSp = property.FindPropertyRelative(nameof(VFXMaterialModifierColor.propertyName));
            GUIContent[] contents = GetGUIContents(editor);
            int lastIndex = GetSelectionIndex(editor, nameSp.stringValue);
            int newIndex = EditorGUI.Popup(labelRect, label, lastIndex, contents);
            if (newIndex != lastIndex)
            {
                nameSp.stringValue = contents[newIndex].text;
            }
            
            #endregion

            if (string.IsNullOrEmpty(nameSp.stringValue))
            {
                Rect tipRect = new Rect(
                    labelRect.x + labelRect.width + 1,
                    labelRect.y,
                    position.width - (labelRect.x + labelRect.width + 1),
                    position.height);
                EditorGUI.LabelField(tipRect, "Select a valid name first.");
                return;
            }

            SerializedProperty curvesSp = property.FindPropertyRelative("curves");

            #region Curve toggle

            bool supportCurveMode = curvesSp != null;
            const int toggleWidth = 45;
            Rect curveToggleRect = new Rect(
                labelRect.x + labelRect.width + 1,
                labelRect.y,
                toggleWidth,
                position.height);

            SerializedProperty stateBitmaskSp = property.FindPropertyRelative("_stateBitmask");
            int lastIntValue = stateBitmaskSp.intValue;
            int targetBit = 1 << 0;
            bool lastUseCurve = (lastIntValue & targetBit) > 0;
            Color prevColor = GUI.color;
            GUI.color = lastUseCurve ? Color.green : Color.grey;
            bool newUseCurve;
            using (new CallbackScope(() => GUI.color = prevColor))
            {
                EditorGUI.BeginDisabledGroup(!supportCurveMode);
                GUIContent desc = supportCurveMode ? supoortedCurveModeDesc : unsupoortedCurveModeDesc;
                newUseCurve = GUI.Button(curveToggleRect, desc) ? !lastUseCurve : lastUseCurve;
                EditorGUI.EndDisabledGroup();
            }
            if (newUseCurve != lastUseCurve)
            {
                if (newUseCurve)
                {
                    stateBitmaskSp.intValue = lastIntValue | targetBit;
                }
                else
                {
                    stateBitmaskSp.intValue = lastIntValue & ~targetBit;
                }
            }

            #endregion

            #region Value

            float valueRectX = curveToggleRect.x + curveToggleRect.width;
            Rect valueRect = new Rect(valueRectX - 13, curveToggleRect.y,
                position.x + position.width - valueRectX + 13, position.height + 0f);
            SerializedProperty fixedValuesSp = property.FindPropertyRelative("fixedValues");
            for (int i = 0; i < VFXSystem.VFXStageCount; i++)
            {
                int interval = -15;
                float subWidth = (valueRect.width - interval) / 3 + 5;
                Rect subValueRect = new Rect(valueRect.x + (subWidth + interval) * i, valueRect.y, subWidth, valueRect.height);
                if (newUseCurve)
                {
                    SerializedProperty gradientSp = curvesSp.GetArrayElementAtIndex(i);
                    CurveValueField(subValueRect, gradientSp);
                }
                else
                {
                    SerializedProperty fixedValueSp = fixedValuesSp.GetArrayElementAtIndex(i);
                    FixedValueField(subValueRect, fixedValueSp);
                }
            }
            
            #endregion
        }

        protected abstract int GetSelectionIndex(VFXMaterialConfigEditor editor, string name);

        protected abstract GUIContent[] GetGUIContents(VFXMaterialConfigEditor editor);

        protected virtual void FixedValueField(Rect position, SerializedProperty serializedProperty)
        {
            EditorGUI.PropertyField(position, serializedProperty, GUIContent.none);
        }

        protected virtual void CurveValueField(Rect position, SerializedProperty serializedProperty)
        {
            EditorGUI.PropertyField(position, serializedProperty, GUIContent.none);
        }
    }

    [CustomPropertyDrawer(typeof(VFXMaterialModifierVector))]
    public class VFXMaterialModifierVectorDrawer : VFXMaterialModifierDrawer
    {
        private static readonly string[] componentNames = new string[4]
        {
            nameof(Vector4.x),
            nameof(Vector4.y),
            nameof(Vector4.z),
            nameof(Vector4.w),
        };

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 4;
        }

        protected override int GetSelectionIndex(VFXMaterialConfigEditor editor, string name)
        {
            return editor.GetPropertyIndex(ShaderPropertyType.Vector, name);
        }

        protected override GUIContent[] GetGUIContents(VFXMaterialConfigEditor editor)
        {
            throw new System.NotImplementedException();
        }

        protected override void FixedValueField(Rect position, SerializedProperty serializedProperty)
        {
            EditorGUILayout.BeginVertical();
            float lineHeight = position.height / 4; 
            Rect labelPos = new Rect(position.x + 3, position.y, 40, EditorGUIUtility.singleLineHeight - 2);
            Rect valuePos = new Rect(position.x + 16, position.y, position.width - 16, EditorGUIUtility.singleLineHeight - 2);
            for (int i = 0; i < 4; i++)
            {
                EditorGUI.LabelField(labelPos, componentNames[i]);
                SerializedProperty componentSp = serializedProperty.FindPropertyRelative(componentNames[i]);
                EditorGUI.PropertyField(valuePos, componentSp, GUIContent.none);
                labelPos.y += lineHeight;
                valuePos.y += lineHeight;
            }
            EditorGUILayout.EndVertical();
        }

        protected override void CurveValueField(Rect position, SerializedProperty serializedProperty)
        {
            EditorGUILayout.BeginVertical();
            const string fieldName = nameof(VFXMaterialModifierVector.VectorCurves.curves);
            SerializedProperty curvesSp = serializedProperty.FindPropertyRelative(fieldName);
            float lineHeight = position.height / 4;
            Rect labelPos = new Rect(position.x + 3, position.y, 40, EditorGUIUtility.singleLineHeight);
            Rect valuePos = new Rect(position.x + 16, position.y, position.width - 16, EditorGUIUtility.singleLineHeight);
            for (int i = 0; i < 4; i++)
            {
                EditorGUI.LabelField(labelPos, componentNames[i]);
                SerializedProperty componentSp = curvesSp.GetArrayElementAtIndex(i);
                EditorGUI.PropertyField(valuePos, componentSp, GUIContent.none);
                labelPos.y += lineHeight;
                valuePos.y += lineHeight;
            }
            EditorGUILayout.EndVertical();
        }
    }
    
    [CustomPropertyDrawer(typeof(VFXMaterialModifierFloat))]
    public class VFXMaterialModifierFloatDrawer : VFXMaterialModifierDrawer
    {
        protected override int GetSelectionIndex(VFXMaterialConfigEditor editor, string name)
        {
            return editor.GetPropertyIndex(ShaderPropertyType.Float, name);
        }

        protected override GUIContent[] GetGUIContents(VFXMaterialConfigEditor editor)
        {
            return editor.GetPropertyLabels(ShaderPropertyType.Float);
        }
    }
    
    [CustomPropertyDrawer(typeof(VFXMaterialModifierInt))]
    public class VFXMaterialModifierIntDrawer : VFXMaterialModifierDrawer
    {
        protected override int GetSelectionIndex(VFXMaterialConfigEditor editor, string name)
        {
            return editor.GetPropertyIndex(ShaderPropertyType.Int, name);
        }

        protected override GUIContent[] GetGUIContents(VFXMaterialConfigEditor editor)
        {
            return editor.GetPropertyLabels(ShaderPropertyType.Int);
        }
    }

    [CustomPropertyDrawer(typeof(VFXMaterialModifierTexture))]
    public class VFXMaterialModifierTextureDrawer : VFXMaterialModifierDrawer
    {
        protected override int GetSelectionIndex(VFXMaterialConfigEditor editor, string name)
        {
            return editor.GetPropertyIndex(ShaderPropertyType.TexEnv, name);
        }

        protected override GUIContent[] GetGUIContents(VFXMaterialConfigEditor editor)
        {
            return editor.GetPropertyLabels(ShaderPropertyType.TexEnv);
        }
    }

    [CustomPropertyDrawer(typeof(VFXMaterialModifierKeyword))]
    public class VFXMaterialModifierKeywordDrawer : VFXMaterialModifierDrawer
    {
        protected override int GetSelectionIndex(VFXMaterialConfigEditor editor, string name)
        {
            return editor.GetKeywordIndex(name);
        }

        protected override GUIContent[] GetGUIContents(VFXMaterialConfigEditor editor)
        {
            return editor.GetKeywordLabels();
        }
    }

    [CustomPropertyDrawer(typeof(VFXMaterialModifierShaderPass))]
    public class VFXMaterialModifierPassDrawer : VFXMaterialModifierDrawer
    {
        protected override GUIContent[] GetGUIContents(VFXMaterialConfigEditor editor)
        {
            return editor.GetPassNameLabels();
        }
        
        protected override int GetSelectionIndex(VFXMaterialConfigEditor editor, string name)
        {
            return editor.GetPassIndex(name);
        }
    }
}