using KuanMi;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[CustomPropertyDrawer(typeof(RenderingLayerMask))]
public class RenderingLayerMaskDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        var amountRect = new Rect(position.x, position.y, position.width, position.height);

        var mask = property.FindPropertyRelative("m_Mask");

        var selected = (int)mask.uintValue;

        var options = GraphicsSettings.defaultRenderPipeline.renderingLayerMaskNames;
        selected = EditorGUI.MaskField(amountRect, selected, options);

        mask.uintValue = (uint)selected;

        EditorGUI.indentLevel = indent;

        EditorGUI.EndProperty();
    }
}