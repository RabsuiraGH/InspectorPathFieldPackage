using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Search;
using UnityEngine.UIElements;
using UnityEngine.XR;

namespace InspectorPathField.Editor
{
    [Serializable]
    [CreateAssetMenu(menuName = "Editor")]
    internal class PathFieldSettings : ScriptableObject
    {
        private static PathFieldSettings _instance;
        private static PathFieldResources _resources;

        public PathDisplayMode PathDisplayMode;
        public readonly PathDisplayMode DefaultPathDisplayMode = PathDisplayMode.ShortPath;

        public SearchType SearchType;
        public readonly SearchType DefaultSearchType = SearchType.UnitySearch;

        public List<string> ShortPathTokensToRemove;
        public readonly List<string> DefaultShortPathTokensToRemove = new() { "Assets", "Resources", "Plugins" };

        public string SearchQuery;
        public readonly string DefaultSearchQuery = "a:assets -t:MonoScript -dir:Assets/Plugins";

        public SearchViewFlags SearchViewFlags;
        public readonly SearchViewFlags DefaultSearchViewFlags = SearchViewFlags.GridView;

        private event Action onSettingsChanged;


        [SettingsProvider]
        private static SettingsProvider CreatePathFieldSettingsProvider()
        {
            if (_resources == null)
            {
                _resources = PathFieldResources.GetAssets();

#if UNITY_EDITOR_PATH_FIELD_DEBUG
                if (_resources != null) Debug.Log(($"Asset load done"));
#endif
            }

            SettingsProvider provider = new("Project/InspectorPathField", SettingsScope.Project)
            {
                label = "Path Field",
                activateHandler = (_, rootElement) =>
                {
                    PathFieldSettings settings = GetAssets();

#region INITIALIZATION

                    EnumField pathDisplayModeField = new(settings.DefaultPathDisplayMode);
                    EnumField searchTypeField = new(settings.DefaultSearchType);
                    IMGUIContainer pathTokensContainer = new();
                    TextField searchQueryField = new();
                    EnumFlagsField searchQueryFlagsField = new(settings.DefaultSearchViewFlags);

                    VisualElement mainContainer = new();
                    HorizontalLayout settingsHeader = new(0);
                    Label hierarchyTitle = new("Path Field");
                    HorizontalLayout buttonContainer = new(0, Justify.FlexEnd);
                    Button importButton = new();
                    Button exportButton = new();
                    Button resetButton = new();


                    HorizontalLayout pathDisplayModeContainer = new(0);
                    HorizontalLayout searchModeContainer = new(0);
                    HorizontalLayout searchQueryContainer = new(0);
                    HorizontalLayout searchQueryFlagsContainer = new(0);


                    _instance.onSettingsChanged = null;
                    _instance.onSettingsChanged += () => UpdateUI(settings.PathDisplayMode, settings.SearchType,
                                                                  settings.SearchQuery, settings.SearchViewFlags,
                                                                  settings.ShortPathTokensToRemove);

#endregion

                    mainContainer.style.flexDirection = FlexDirection.Column;
                    mainContainer.StyleMargin(10, 10, 10, 10);


#region HEADER

                    {
                        // Background
                        settingsHeader.StyleBackgroundColor(new Color(0, 0, 0, 0.2f));
                        settingsHeader.StyleMargin(-10, 0, 0, 0);
                        settingsHeader.StylePadding(0, 0, 4, 10);
                        settingsHeader.StyleHeight(36);
                        mainContainer.Add(settingsHeader);
                    }
                    {
                        // Title
                        hierarchyTitle.StyleFontSize(20);
                        hierarchyTitle.StyleMargin(10, 0, 2, 2);
                        hierarchyTitle.StyleFont(FontStyle.Bold);
                        settingsHeader.Add(hierarchyTitle);
                    }
                    {
                        // Import Button
                        importButton.text = "Import";
                        importButton.StyleFontSize(14);
                        importButton.StyleMargin(5, 0, 2, 2);
                        importButton.StyleHeight(24);
                        importButton.clicked += () =>
                        {
                            _instance.ImportFromJson();
                            UpdateUI(settings.PathDisplayMode, settings.SearchType,
                                     settings.SearchQuery, settings.SearchViewFlags,
                                     settings.ShortPathTokensToRemove);
                        };
                        buttonContainer.Add(importButton);
                    }
                    {
                        // Export Button
                        exportButton.text = "Export";
                        exportButton.StyleFontSize(14);
                        exportButton.StyleMargin(5, 0, 2, 2);
                        exportButton.StyleHeight(24);
                        exportButton.clicked += _instance.ExportToJson;
                        buttonContainer.Add(exportButton);
                    }

                    {
                        // Reset Buttons
                        resetButton.clicked += () =>
                        {
                            if (!EditorUtility.DisplayDialog("Confirm reset",
                                                             "Are you sure you want to reset settings?",
                                                             "Reset", "Cancel")) return;
                            UpdateUI(settings.DefaultPathDisplayMode, settings.DefaultSearchType,
                                     settings.DefaultSearchQuery, settings.DefaultSearchViewFlags,
                                     settings.DefaultShortPathTokensToRemove);
                        };
                        resetButton.style.backgroundImage = _resources.ResetButtonTexture;
                        resetButton.StyleSize(32, 32);
                        resetButton.StyleMargin(5, 2, -2, 4);
                        buttonContainer.Add(resetButton);
                    }

                    settingsHeader.Add(buttonContainer);

#endregion

#region PARAMETERS

                    {
                        // Path Display Mode
                        pathDisplayModeContainer.StyleMargin(0, 12, 5, 0);
                        pathDisplayModeContainer.tooltip =
                            $"<b>{nameof(PathDisplayMode.FullPath)}</b> - Display full path to object.\n" +
                            $"<b>{nameof(PathDisplayMode.ShortPath)}</b> - Display path without removed tokens.\n" +
                            $"<b>{nameof(PathDisplayMode.FileName)}</b> - Display only file name.";

                        mainContainer.Add(pathDisplayModeContainer);


                        pathDisplayModeField.value = settings.PathDisplayMode;
                        AttachLabelToField(pathDisplayModeContainer, "Default Path Display Mode", pathDisplayModeField);
                        RegisterChange(pathDisplayModeField,
                                       newValue => settings.PathDisplayMode = (PathDisplayMode)newValue);
                    }

                    {
                        // Remove Tokens
                        CreateTokenList();
                        mainContainer.Add(pathTokensContainer);
                    }


                    {
                        // Search Mode
                        searchModeContainer.StyleMargin(0, 12, 5, 0);
                        searchModeContainer.tooltip =
                            $"<b>{nameof(SearchType.ObjectPicker)}</b> - Uses default unity objet picker.\n" +
                            $"<b>{nameof(SearchType.UnitySearch)}</b> - Uses unity search tool.";

                        mainContainer.Add(searchModeContainer);

                        searchTypeField.value = settings.SearchType;
                        AttachLabelToField(searchModeContainer, "Search Type", searchTypeField);
                        RegisterChange(searchTypeField, newValue =>
                        {
                            settings.SearchType = (SearchType)newValue;
                            searchQueryContainer.StyleDisplay((SearchType)newValue == SearchType.UnitySearch);
                            searchQueryFlagsContainer.StyleDisplay((SearchType)newValue == SearchType.UnitySearch);
                        });
                    }


                    {
                        // Search Query
                        searchQueryContainer.StyleMargin(0, 12, 5, 0);
                        searchQueryContainer.tooltip = "Default query will be placed in the search line when opened.";
                        mainContainer.Add(searchQueryContainer);

                        searchQueryField.value = settings.SearchQuery;
                        AttachLabelToField(searchQueryContainer, "Default Search Query", searchQueryField);
                        RegisterChange(searchQueryField, newValue => settings.SearchQuery = newValue);
                    }

                    {
                        // Search View Flags
                        searchQueryFlagsContainer.StyleMargin(0, 12, 5, 0);
                        searchQueryFlagsContainer.tooltip = "Flags for unity search view.\n" +
                                                            "<b>WARNING:</b> Use this parameter on your own ricks.";
                        mainContainer.Add(searchQueryFlagsContainer);

                        searchQueryFlagsField.value = settings.SearchViewFlags;
                        AttachLabelToField(searchQueryFlagsContainer, "Search View Flags", searchQueryFlagsField);
                        RegisterChange(searchQueryFlagsField,
                                       newValue => settings.SearchViewFlags = (SearchViewFlags)newValue);
                    }

#endregion

                    rootElement.Add(mainContainer);


                    Undo.undoRedoPerformed -= OnUndoRedoPerformed;
                    Undo.undoRedoPerformed += OnUndoRedoPerformed;

                    EditorUtility.SetDirty(settings);

                    return;

#region UI UTILITIES

                    void CreateTokenList()
                    {
                        ReorderableList pathTokenUIList =
                            new(settings.ShortPathTokensToRemove, typeof(string), true, true, true, true)
                            {
                                drawHeaderCallback = (rect) =>
                                {
                                    EditorGUI.LabelField(rect, "Short Path Tokens To Remove");
                                },

                                drawElementCallback = (rect, index, _, _) =>
                                {
                                    rect.y += 2;
                                    settings.ShortPathTokensToRemove[index] =
                                        EditorGUI.TextField(new Rect(rect.x, rect.y,
                                                                     rect.width,
                                                                     EditorGUIUtility
                                                                        .singleLineHeight),
                                                            settings.ShortPathTokensToRemove[index]);
                                },

                                onAddCallback = _ => { settings.ShortPathTokensToRemove.Add(""); },

                                onRemoveCallback = list =>
                                {
                                    if (list.index < 0) return;
                                    settings.ShortPathTokensToRemove.RemoveAt(list.index);
                                }
                            };

                        SerializedObject serializedSetting = new(settings);

                        pathTokensContainer.onGUIHandler = null;
                        pathTokensContainer.onGUIHandler += () =>
                        {
                            serializedSetting.Update();
                            pathTokenUIList.DoLayoutList();
                            serializedSetting.ApplyModifiedProperties();
                        };

                        pathTokensContainer.StyleMarginRight(-1);
                        pathTokensContainer.StyleMarginTop(10);
                    }

                    void RegisterChange<T>(BaseField<T> field, Action<T> assignValue)
                    {
                        field.RegisterValueChangedCallback(evt =>
                        {
                            Undo.RecordObject(settings, "Change Settings");

                            assignValue((T)evt.newValue);

                            EditorUtility.SetDirty(settings);
                        });
                    }

                    void AttachLabelToField(VisualElement container, string labelText, VisualElement field)
                    {
                        Label label = new();
                        label.text = labelText;
                        label.StyleWidth(Length.Percent(30));
                        label.StyleMarginRight(10);
                        container.Add(label);

                        field.StyleWidth(Length.Percent(70));
                        container.Add(field);
                    }

                    void UpdateUI(PathDisplayMode pathDisplayMode, SearchType searchType, string searchQuery,
                                  SearchViewFlags searchFlags, List<string> tokensToHide)
                    {
                        pathDisplayModeField.value = pathDisplayMode;
                        searchTypeField.value = searchType;
                        settings.ShortPathTokensToRemove = new(tokensToHide);
                        CreateTokenList();
                        pathTokensContainer.MarkDirtyLayout();
                        searchQueryField.value = searchQuery;
                        searchQueryFlagsField.value = searchFlags;
                    }

#endregion
                },
                deactivateHandler = () => { Undo.undoRedoPerformed -= OnUndoRedoPerformed; }
            };

            return provider;
        }


        internal static void OnUndoRedoPerformed()
        {
            SettingsService.NotifySettingsProviderChanged();

            if (_instance != null)
            {
                _instance.onSettingsChanged?.Invoke(); // Refresh components on undo & redo
            }
        }


        internal static PathFieldSettings GetAssets()
        {
            if (_instance != null)
                return _instance;

            string[] guids = AssetDatabase.FindAssets($"t:{nameof(PathFieldSettings)}");

            foreach (string t in guids)
            {
                _instance = AssetDatabase.LoadAssetAtPath<PathFieldSettings>(AssetDatabase.GUIDToAssetPath(t));

                if (_instance != null)
                    return _instance;
            }

            return _instance = CreateAssets();
        }


        internal static PathFieldSettings CreateAssets()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save as...", "Path Field Settings", "asset", "");

            if (path.Length <= 0)
            {
                return null;
            }

            PathFieldSettings settings = CreateInstance<PathFieldSettings>();
            AssetDatabase.CreateAsset(settings, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = settings;
            return settings;
        }


        internal void ImportFromJson()
        {
            string path = EditorUtility.OpenFilePanel("Import Path Field settings", "", "json");

            if (path.Length <= 0) return;

            string json;
            using (StreamReader sr = new(path))
            {
                json = sr.ReadToEnd();
            }

            if (string.IsNullOrEmpty(json)) return;
            JsonUtility.FromJsonOverwrite(json, this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }


        internal void ExportToJson()
        {
            string path = EditorUtility.SaveFilePanelInProject("Export Path Field settings as...",
                                                               "Path Field Settings", "json", "");

            if (path.Length <= 0) return;

            string json = JsonUtility.ToJson(_instance, true);

            using (StreamWriter sw = new(path))
            {
                sw.Write(json);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
        }
    }

    internal class HorizontalLayout : VisualElement
    {
        internal HorizontalLayout(int grow, Justify justify = Justify.SpaceBetween)
        {
            name = nameof(HorizontalLayout);
            this.StyleFlexDirection(FlexDirection.Row);
            this.StyleFlexGrow(grow);
            this.StyleJustifyContent(justify);
        }
    }
}