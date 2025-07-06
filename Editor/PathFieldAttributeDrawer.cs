using UnityEditor;
using UnityEngine;

namespace InspectorPathField.Editor
{
    [CustomPropertyDrawer(typeof(PathFieldAttribute))]
    internal class PathFieldAttributeDrawer : PathFieldBaseDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "Use [PathField] with string only!");
                return;
            }
            base.OnGUI(position, property, label);
        }
    }
}