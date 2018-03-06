﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FbxExporters.EditorTools;
using UnityEditor.Presets;

namespace FbxExporters
{
    namespace Editor
    {
        public abstract class ExportOptionsEditorWindow : EditorWindow
        {
            protected const string DefaultWindowTitle = "Export Options";
            protected const float SelectableLabelMinWidth = 90;
            protected const float BrowseButtonWidth = 25;
            protected const float LabelWidth = 175;
            protected const float FieldOffset = 18;
            protected const float TextFieldAlignOffset = 3;
            protected const float ExportButtonWidth = 100;
            protected const float FbxExtOffset = -7;

            protected virtual GUIContent m_windowTitle { get { return new GUIContent (DefaultWindowTitle); } }

            protected string m_exportFileName = "";
            protected ModelExporter.AnimationExportType m_animExportType = ModelExporter.AnimationExportType.all;
            protected bool m_singleHierarchyExport = true;

            protected UnityEditor.Editor m_innerEditor;
            private FbxExportPresetSelectorReceiver m_receiver;

            private static GUIContent presetIcon { get { return EditorGUIUtility.IconContent ("Preset.Context"); }}
            private static GUIStyle presetIconButton { get { return new GUIStyle("IconButton"); }}

            private bool m_showOptions;

            protected GUIStyle m_nameTextFieldStyle;
            protected GUIStyle m_fbxExtLabelStyle;
            protected float m_fbxExtLabelWidth;

            protected virtual void OnEnable(){
                InitializeReceiver ();
                m_showOptions = true;
                this.minSize = new Vector2 (SelectableLabelMinWidth + LabelWidth + BrowseButtonWidth, 220);

                m_nameTextFieldStyle = new GUIStyle(GUIStyle.none);
                m_nameTextFieldStyle.alignment = TextAnchor.LowerCenter;
                m_nameTextFieldStyle.clipping = TextClipping.Clip;

                m_fbxExtLabelStyle = new GUIStyle (GUIStyle.none);
                m_fbxExtLabelStyle.alignment = TextAnchor.MiddleLeft;
                m_fbxExtLabelStyle.richText = true;
                m_fbxExtLabelStyle.contentOffset = new Vector2 (FbxExtOffset, 0);

                m_fbxExtLabelWidth = m_fbxExtLabelStyle.CalcSize (new GUIContent (".fbx")).x;
            }

            protected static T CreateWindow<T>() where T : EditorWindow {
                return (T)EditorWindow.GetWindow <T>(DefaultWindowTitle, focus:true);
            }

            protected virtual void InitializeWindow(string filename = "", bool singleHierarchyExport = true, ModelExporter.AnimationExportType exportType = ModelExporter.AnimationExportType.all){
                this.SetTitle ();
                this.SetFilename (filename);
                this.SetAnimationExportType (exportType);
                this.SetSingleHierarchyExport (singleHierarchyExport);
            }

            private void SetTitle(){
                this.titleContent = m_windowTitle;
            }

            private void InitializeReceiver(){
                if (!m_receiver) {
                    m_receiver = ScriptableObject.CreateInstance<FbxExportPresetSelectorReceiver> () as FbxExportPresetSelectorReceiver;
                    m_receiver.SelectionChanged -= OnPresetSelectionChanged;
                    m_receiver.SelectionChanged += OnPresetSelectionChanged;
                    m_receiver.DialogClosed -= SaveExportSettings;
                    m_receiver.DialogClosed += SaveExportSettings;
                }
            }

            public void SetFilename(string filename){
                // remove .fbx from end of filename
                int extIndex = filename.LastIndexOf(".fbx");
                if (extIndex < 0) {
                    m_exportFileName = filename;
                    return;
                }
                m_exportFileName = filename.Remove(extIndex);
            }

            public void SetAnimationExportType(ModelExporter.AnimationExportType exportType){
                m_animExportType = exportType;
            }

            public void SetSingleHierarchyExport(bool singleHierarchy){
                m_singleHierarchyExport = singleHierarchy;

                if (m_innerEditor) {
                    var exportModelSettingsEditor = m_innerEditor as ExportModelSettingsEditor;
                    if (exportModelSettingsEditor) {
                        exportModelSettingsEditor.SetIsSingleHierarchy (m_singleHierarchyExport);
                    }
                }
            }

            public void SaveExportSettings()
            {
                // save once preset selection is finished
                EditorUtility.SetDirty (ExportSettings.instance);
                ExportSettings.instance.Save ();
            }

            public void OnPresetSelectionChanged()
            {
                this.Repaint ();
            }

            protected abstract void Export ();

            /// <summary>
            /// Function to be used by derived classes to add custom UI between the file path selector and export options.
            /// </summary>
            protected virtual void CreateCustomUI(){}

            protected virtual bool DisableNameSelection(){
                return false;
            }

            protected void OnGUI ()
            {
                // Increasing the label width so that none of the text gets cut off
                EditorGUIUtility.labelWidth = LabelWidth;

                GUILayout.BeginHorizontal ();
                GUILayout.FlexibleSpace ();
                if(EditorGUILayout.DropdownButton(presetIcon, FocusType.Keyboard, presetIconButton)){
                    InitializeReceiver ();
                    m_receiver.SetTarget(ExportSettings.instance.exportModelSettings);
                    m_receiver.SetInitialValue (new Preset (ExportSettings.instance.exportModelSettings));
                    UnityEditor.Presets.PresetSelector.ShowSelector(ExportSettings.instance.exportModelSettings, null, true, m_receiver);
                }
                GUILayout.EndHorizontal();

                EditorGUILayout.LabelField("Naming");
                EditorGUI.indentLevel++;

                GUILayout.BeginHorizontal ();
                EditorGUILayout.LabelField(new GUIContent(
                    "Export Name:",
                    "Filename to save model to."),GUILayout.Width(LabelWidth-TextFieldAlignOffset));

                EditorGUI.BeginDisabledGroup (DisableNameSelection());
                // Show the export name with an uneditable ".fbx" at the end
                //-------------------------------------
                EditorGUILayout.BeginVertical ();
                EditorGUILayout.BeginHorizontal(EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                EditorGUI.indentLevel--;
                // continually resize to contents
                var textFieldSize = m_nameTextFieldStyle.CalcSize (new GUIContent(m_exportFileName));
                m_exportFileName = EditorGUILayout.TextField (m_exportFileName, m_nameTextFieldStyle, GUILayout.Width(textFieldSize.x + 5), GUILayout.MinWidth(5));
                m_exportFileName = ModelExporter.ConvertToValidFilename (m_exportFileName);

                EditorGUILayout.LabelField ("<color=#808080ff>.fbx</color>", m_fbxExtLabelStyle, GUILayout.Width(m_fbxExtLabelWidth));
                EditorGUI.indentLevel++;

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical ();
                //-----------------------------------
                EditorGUI.EndDisabledGroup ();
                GUILayout.EndHorizontal ();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent(
                    "Export Path:",
                    "Relative path for saving Model Prefabs."),GUILayout.Width(LabelWidth - FieldOffset));

                var pathLabels = ExportSettings.GetRelativeFbxSavePaths();

                ExportSettings.instance.selectedFbxPath = EditorGUILayout.Popup (ExportSettings.instance.selectedFbxPath, pathLabels, GUILayout.MinWidth(SelectableLabelMinWidth));

                if (GUILayout.Button(new GUIContent("...", "Browse to a new location to export to"), EditorStyles.miniButton, GUILayout.Width(BrowseButtonWidth)))
                {
                    string initialPath = Application.dataPath;

                    string fullPath = EditorUtility.OpenFolderPanel(
                        "Select Export Model Path", initialPath, null
                    );

                    // Unless the user canceled, make sure they chose something in the Assets folder.
                    if (!string.IsNullOrEmpty(fullPath))
                    {
                        var relativePath = ExportSettings.ConvertToAssetRelativePath(fullPath);
                        if (string.IsNullOrEmpty(relativePath))
                        {
                            Debug.LogWarning("Please select a location in the Assets folder");
                        }
                        else
                        {
                            ExportSettings.AddFbxSavePath(relativePath);

                            // Make sure focus is removed from the selectable label
                            // otherwise it won't update
                            GUIUtility.hotControl = 0;
                            GUIUtility.keyboardControl = 0;
                        }
                    }
                }
                GUILayout.EndHorizontal();

                CreateCustomUI();

                EditorGUILayout.Space ();
                EditorGUI.indentLevel--;
                m_showOptions = EditorGUILayout.Foldout (m_showOptions, "Options");
                EditorGUI.indentLevel++;
                if (m_showOptions) {
                    m_innerEditor.OnInspectorGUI ();
                }

                GUILayout.FlexibleSpace ();

                GUILayout.BeginHorizontal ();
                GUILayout.FlexibleSpace ();
                if (GUILayout.Button ("Cancel", GUILayout.Width(ExportButtonWidth))) {
                    this.Close ();
                }

                if (GUILayout.Button ("Export", GUILayout.Width(ExportButtonWidth))) {
                    Export ();
                    this.Close ();
                }
                GUILayout.EndHorizontal ();

                if (GUI.changed) {
                    SaveExportSettings ();
                }
            }

            protected void CheckFileExists(string filePath){
                // check if file already exists, give a warning if it does
                if (System.IO.File.Exists (filePath)) {
                    bool overwrite = UnityEditor.EditorUtility.DisplayDialog (
                        string.Format("{0} Warning", ModelExporter.PACKAGE_UI_NAME), 
                        string.Format("File {0} already exists.", filePath), 
                        "Overwrite", "Cancel");
                    if (!overwrite) {
                        this.Close ();

                        if (GUI.changed) {
                            SaveExportSettings ();
                        }
                    }
                }
            }
        }

        public class ExportModelEditorWindow : ExportOptionsEditorWindow
        {
            public static void Init (string filename = "", bool singleHierarchyExport = true, ModelExporter.AnimationExportType exportType = ModelExporter.AnimationExportType.all)
            {
                ExportModelEditorWindow window = CreateWindow<ExportModelEditorWindow> ();
                window.InitializeWindow (filename, singleHierarchyExport, exportType);
                window.Show ();
            }

            protected override void OnEnable ()
            {
                base.OnEnable ();

                if (!m_innerEditor) {
                    var ms = ExportSettings.instance.exportModelSettings;
                    if (!ms) {
                        ExportSettings.LoadSettings ();
                        ms = ExportSettings.instance.exportModelSettings;
                    }
                    m_innerEditor = UnityEditor.Editor.CreateEditor (ms);
                    this.SetSingleHierarchyExport (m_singleHierarchyExport);
                }
            }

            protected override void Export(){
                var filePath = ExportSettings.GetFbxAbsoluteSavePath ();

                filePath = System.IO.Path.Combine (filePath, m_exportFileName + ".fbx");

                CheckFileExists (filePath);

                if (ModelExporter.ExportObjects (filePath, exportType: m_animExportType, lodExportType: ExportSettings.GetLODExportType()) != null) {
                    // refresh the asset database so that the file appears in the
                    // asset folder view.
                    AssetDatabase.Refresh ();
                }
            }
        }
    }
}