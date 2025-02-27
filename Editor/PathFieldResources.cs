using UnityEditor;
using UnityEngine;

namespace InspectorPathField.Editor
{
    internal class PathFieldResources : ScriptableObject
    {
        public Texture2D GotoButtonTexture;
        public Texture2D SearchButtonTexture;
        public Texture2D ResetButtonTexture;


        internal static PathFieldResources GetAssets()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(PathFieldResources)}");

            if (guids.Length <= 0)
            {
                Debug.LogError($"No {nameof(PathFieldResources)} found in the project.");
                return null;
            }

            PathFieldResources asset =
                AssetDatabase.LoadAssetAtPath<PathFieldResources>(AssetDatabase.GUIDToAssetPath(guids[0]));

            if (asset != null)
            {
                return asset;
            }

            Debug.LogError($"Failed to load {nameof(PathFieldResources)} from the project.");
            return null;
        }
    }
}