using UnityEditor;
using UnityEngine;

namespace InspectorPathField.Editor
{
    [CustomPropertyDrawer(typeof(PathField))]
    public class PathFieldDrawer : PropertyDrawer
    {
        private int _pickerControlID = -1;
        private const float GOTO_BUTTON_WIDTH = 20f;
        private const float SEARCH_BUTTON_WIDTH = 20f;
        private const float BUTTON_SPACING = 3;
        private float _textFieldWidth;


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Get property
            SerializedProperty assetPath = property.FindPropertyRelative("AssetPath");

            // Calculate text field size depends on the button size
            _textFieldWidth = position.width - GOTO_BUTTON_WIDTH - SEARCH_BUTTON_WIDTH - BUTTON_SPACING;

            // Setup rectangle for text field
            Rect textFieldRect = new(position.x, position.y, _textFieldWidth, position.height);
            EditorGUI.PropertyField(textFieldRect, assetPath, label);

            // Goto button
            if (!string.IsNullOrEmpty(assetPath.stringValue))
            {
                // Setup rectangle for goto button
                Rect gotoButtonRect = new(position.x + _textFieldWidth, position.y, GOTO_BUTTON_WIDTH, position.height);

                // On press action
                if (GUI.Button(gotoButtonRect, "▶"))
                {
                    Object obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath.stringValue);
                    if (obj != null)
                        EditorGUIUtility.PingObject(obj);
                }
            }

            // Search button
            Rect searchButtonRect = new(position.x + _textFieldWidth + GOTO_BUTTON_WIDTH + BUTTON_SPACING, position.y,
                                        SEARCH_BUTTON_WIDTH,
                                        position.height);

            // On press action
            if (GUI.Button(searchButtonRect, "s"))
            {
                _pickerControlID = GUIUtility.GetControlID(FocusType.Passive);
                EditorGUIUtility.ShowObjectPicker<Object>(null, false, "", _pickerControlID);
            }

            if (Event.current.commandName != "ObjectSelectorUpdated" ||
                EditorGUIUtility.GetObjectPickerControlID() != _pickerControlID)
            {
                return;
            }


            Object pickedObject = EditorGUIUtility.GetObjectPickerObject();

            if (pickedObject == null)
            {
                return;
            }

            // Save path to asset
            string path = AssetDatabase.GetAssetPath(pickedObject);
            assetPath.stringValue = path;
        }
    }
}