using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Rendering;
#if URP_ENABLED
using UnityEngine.Rendering.Universal;
using Wikitude;
#endif

namespace WikitudeEditor
{

    public class URPHelperItem : EditorWindow
    {
        private Vector2 _scrollPosition;
        private List<FileInfo> _sceneFiles;
        private enum ListTagStyle : int {
            Default = 0,
            Modified,
            Unchanged,
            Failed
        }
        private ListTagStyle[] _listTagStyles;
        private static string _urlToDocumentation = "https://www.wikitude.com/external/doc/expertedition/docs/UnitySupportedPackages.html#universal-render-pipeline-support";
#if URP_ENABLED
        private bool _changesApplied = false;
#endif

        [MenuItem("Window/Wikitude/URP Helper")]
        private static void Init() {
            /* Check if Universal Render Pipeline is used by checking if a corresponding asset is set */
            if (!GraphicsSettings.renderPipelineAsset || (
                GraphicsSettings.renderPipelineAsset && !GraphicsSettings.renderPipelineAsset.GetType().Name.Contains("UniversalRenderPipelineAsset"))) {
                Debug.LogError("Universal Render Pipeline not set in the Graphics Settings! Please try again after installing and setting the Universal Render Pipeline Asset! Link to Documentation: " + _urlToDocumentation);
                return;
            }

            var window = GetWindow<URPHelperItem>("URP Helper");
            SetURPSymbol();

            /* Sets the window size to be opened with. */
            window.minSize = new Vector2(350f, 400f);

            window.Show();

            /* Sets the actual minimum window size. */
            window.minSize = new Vector2(230f, 250f);
        }


        /* Sets a define symbol in order to unlock universal pipeline specific calculations. */
        private static void SetURPSymbol() {
            BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            List<string> defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup).Split(';').ToList();

            if (!defines.Contains("URP_ENABLED")) {
                defines.Add("URP_ENABLED");
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, string.Join(";", defines.ToArray()));
            }
        }

        /* Removes the previously set define symbol after it is not needed anymore. */
        private void OnDestroy() {
            BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            List<string> defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup).Split(';').ToList();

            if (defines.Contains("URP_ENABLED")) {
                defines.Remove("URP_ENABLED");
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, string.Join(";", defines.ToArray()));
            }
        }

        private void OnGUI() {
#if !URP_ENABLED
            /* Show this section while the new symbol is still compiling. */
            DrawHelpbox("Setting Universal Render Pipeline define symbol. Recompiling might need some time.\nReady in a few seconds...", MessageType.Info);
#else
            /* Show this section after symbol is set and compiled. */
            DrawHelpbox("This helper overwrites properties of the render pipeline and cameras in the listed Sample scenes in order to properly work with URP.", MessageType.Warning);
            DrawLinkButton("Open Documentation", _urlToDocumentation);

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            DrawHorizontalLine();

            EditorGUILayout.BeginHorizontal();
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawListOfScenes();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndHorizontal();

            DrawHorizontalLine();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space();
            if (!_changesApplied) {
                GUILayout.Label(_sceneFiles.Count.ToString() + " scenes found");
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Apply changes", GUILayout.Width(120f))) {
                    SetShadowSettings();
                    ModifyScenes();
                }
            } else {
                DrawHelpbox("Changes applied. See Console logs for more information.", MessageType.Info);
            }
            EditorGUILayout.Space();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.Space();
#endif
        }

        /* Get List of samples scenes, excluding Main Menu and AR Foundation scenes. */
        private void CalcListOfScenes() {
            var directoryInfo = new DirectoryInfo(Application.dataPath + "/Samples");

            _sceneFiles = directoryInfo.GetFiles("*.unity", SearchOption.AllDirectories).ToList();
            _sceneFiles.RemoveAll(item => item.Name.Contains("Main Menu"));
            _sceneFiles.RemoveAll(item => item.Name.Contains("AR Foundation"));

            _listTagStyles = new ListTagStyle[_sceneFiles.Count];
        }

         /* Draws List of Scenes, with corresponding styling. */
        private void DrawListOfScenes() {
            if (_sceneFiles == null) {
                CalcListOfScenes();
            }

            GUIStyle ListItemStyle;
            ListItemStyle = new GUIStyle(EditorStyles.label);
            ListItemStyle.richText = true;
            ListItemStyle.padding = new RectOffset(20, 0, 0, 0);

            EditorGUILayout.BeginVertical(GUILayout.Height(_sceneFiles.Count * 15f));
            for (int i = 0; i < _sceneFiles.Count; i++) {
                string tag = "";
                ListItemStyle.fontStyle = FontStyle.Bold;
                switch (_listTagStyles[i]) {
                    case ListTagStyle.Modified:
                        tag = " <color=#22ff00>- modified</color>";
                        break;
                    case ListTagStyle.Failed:
                        tag = " <color=#ff2200>- failed</color>";
                        break;
                    case ListTagStyle.Unchanged:
                        tag = " <color=#ffec00>- unchanged</color>";
                        break;
                    default:
                        ListItemStyle.fontStyle = FontStyle.Normal;
                        break;
                }
                EditorGUILayout.LabelField("•  " + _sceneFiles[i].Name + tag, ListItemStyle);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawHelpbox(string message, MessageType messageType) {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(message, messageType);
            EditorGUILayout.Space();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        private void DrawHorizontalLine() {
            GUIStyle horizontalLineStyle = new GUIStyle();
            horizontalLineStyle.normal.background = EditorGUIUtility.whiteTexture;
            horizontalLineStyle.margin = new RectOffset(0, 0, 4, 4 );
            horizontalLineStyle.fixedHeight = 1;

            Color GUIColor = GUI.color;
            GUI.color = Color.grey;
            GUILayout.Box(GUIContent.none, horizontalLineStyle);
            GUI.color = GUIColor;
        }

        private void DrawLinkButton(string title, string url) {
            GUIStyle linkButtonStyle = new GUIStyle(GUI.skin.label);
            linkButtonStyle.normal.textColor = new Color(0f, 0.5f, 0.95f, 1f);
            linkButtonStyle.hover.textColor = linkButtonStyle.normal.textColor;
            linkButtonStyle.fixedWidth = 125f;
            linkButtonStyle.margin = new RectOffset(10, 0, 0, 0);

            if (GUILayout.Button(title, linkButtonStyle)) {
                Application.OpenURL(url);
            }

            Rect buttonRect = GUILayoutUtility.GetLastRect();
            GUI.Box(new Rect(buttonRect.x, buttonRect.y + buttonRect.height, buttonRect.width, 1), GUIContent.none);
        }

#if URP_ENABLED
        /* Since the boundaries of  AR scenes are relatively limited, the default shadow distance should be overwritten. */
        private void SetShadowSettings() {
            string assetPath = AssetDatabase.GetAssetPath(GraphicsSettings.renderPipelineAsset);
            var asset = AssetDatabase.LoadMainAssetAtPath(assetPath) as UniversalRenderPipelineAsset;
            asset.shadowDistance = 5f;
            asset.shadowCascadeOption = ShadowCascadesOption.FourCascades;
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log("Changed RenderPipelineAsset shadow distance to 5 and cascades to 'Four Cascades'");
        }

        /* Iterates through every found scene and applies the needed changes for URP. */
        private void ModifyScenes() {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
                return;
            }

            string activeScenePath = EditorSceneManager.GetSceneAt(0).path;

            for (int i = 0; i < _sceneFiles.Count; i++) {
                try {
                    EditorSceneManager.OpenScene(_sceneFiles[i].FullName, OpenSceneMode.Single);

                    UniversalAdditionalCameraData mainCameraData = Camera.main.GetUniversalAdditionalCameraData();

                    GameObject cameraFrameObject = GameObject.FindObjectOfType<CameraFrameRenderer>().gameObject;
                    UniversalAdditionalCameraData cameraFrameRendererCameraData = cameraFrameObject.GetComponent<Camera>().GetUniversalAdditionalCameraData();

                    if (mainCameraData.renderType == CameraRenderType.Overlay && cameraFrameRendererCameraData.cameraStack.Contains(Camera.main)) {
                        Debug.LogWarning("URP relevant changes already seem to be set in scene: " + _sceneFiles[i].Name);
                        _listTagStyles[i] = ListTagStyle.Unchanged;
                    } else {
                        mainCameraData.renderType = CameraRenderType.Overlay;
                        cameraFrameRendererCameraData.cameraStack.Add(Camera.main);

                        /* The alignment initialization sample scene has two additional camera, which have to be added as an overlay. */
                        foreach (Camera camera in Resources.FindObjectsOfTypeAll(typeof(Camera)) as Camera[]) {
                            if (camera.transform.name.Contains("Alignment Initializer")) {
                                UniversalAdditionalCameraData alignmentCameraData = camera.GetUniversalAdditionalCameraData();
                                alignmentCameraData.renderType = CameraRenderType.Overlay;
                                cameraFrameRendererCameraData.cameraStack.Add(camera);
                                EditorUtility.SetDirty(camera);
                            }
                        }

                        EditorUtility.SetDirty(Camera.main.gameObject);
                        EditorUtility.SetDirty(cameraFrameObject);
                        EditorSceneManager.SaveOpenScenes();
                        Debug.Log("Applied URP relevant changes to scene: " + _sceneFiles[i].Name);
                        _listTagStyles[i] = ListTagStyle.Modified;
                    }
                } catch (Exception e) {
                    Debug.LogError("Failed to apply URP relevant changes to scene: " + _sceneFiles[i].Name + " (Error:+ " + e.Message + ")");
                        _listTagStyles[i] = ListTagStyle.Failed;
                }
            }

            EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);
            Repaint();
            _changesApplied = true;
        }
#endif
    }
}