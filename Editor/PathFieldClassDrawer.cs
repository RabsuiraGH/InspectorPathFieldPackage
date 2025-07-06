using UnityEditor;
using UnityEngine;

namespace InspectorPathField.Editor
{
    [CustomPropertyDrawer(typeof(PathField))]
    internal class PathFieldClassDrawer : PathFieldBaseDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            base.OnGUI(position, property.FindPropertyRelative("AssetPath"), label);
        }
    }
}