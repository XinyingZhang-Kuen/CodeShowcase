using UnityEditor;
using UnityEngine;

namespace VFXManager.Editor
{
    [CustomPropertyDrawer(typeof(VFXMaterialInt))]
    public class VFXMaterialIntDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            VFXMaterialConfigEditor editor = VFXMaterialConfigEditor.Current;

            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
            SerializedProperty nameSp = property.FindPropertyRelative("name");
            GUIContent[] contents = editor.GetPropertyLabels(ShaderUtil.ShaderPropertyType.Int);
            int lastIndex = editor.GetPropertyIndex(ShaderUtil.ShaderPropertyType.Int, nameSp.stringValue);
            int newIndex = EditorGUI.Popup(labelRect, label, lastIndex, contents);
            if (newIndex != lastIndex)
            {
                nameSp.stringValue = contents[newIndex].text;
            }

            const int toggleWidth = 45;
            Rect curveRect = new Rect(
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
                newUseCurve = GUI.Button(curveRect, "Curve") ? !lastUseCurve : lastUseCurve;
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

            float valueRectX = curveRect.x + curveRect.width;
            Rect valueRect = new Rect(valueRectX - 13, curveRect.y - 1, position.x + position.width - valueRectX + 13, position.height + 1f);
            if (newUseCurve)
            {
                SerializedProperty fixedValueSp = property.FindPropertyRelative("fixedValue");
                fixedValueSp.intValue = EditorGUI.IntField(valueRect, fixedValueSp.intValue);
            }
            else
            {
                SerializedProperty curveSp = property.FindPropertyRelative("curve");
                EditorGUI.PropertyField(valueRect, curveSp, GUIContent.none);
            }
        }
    }
}