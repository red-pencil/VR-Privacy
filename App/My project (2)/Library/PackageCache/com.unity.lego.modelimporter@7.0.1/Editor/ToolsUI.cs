// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using UnityEditor;
#if UNITY_2021_2_OR_NEWER
using UnityEditor.SceneManagement;
#else
using UnityEditor.Experimental. SceneManagement;
#endif
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace LEGOModelImporter
{
    [InitializeOnLoad]
    public class ToolsUI
    {
        const int windowID = 42;
        const int sceneViewTopBarHeight = 21;
        const int distanceFromBorders = 5;
        const int buttonSize = 40;
        const int buttonMargin = 4;

        static bool show;
        static bool initialised;

        static GUIStyle brickBuildingStyle;
        static GUIStyle selectConnectedStyle;

        static Texture2D brickBuildingOnImage;
        static Texture2D brickBuildingOffImage;
        static Texture2D selectConnectedOffImage;
        static Texture2D selectConnectedOnImage;

        static Rect toolsWindow = new Rect(10f, sceneViewTopBarHeight + 10, 80, 40);

        static ToolsUI()
        {
            ToolsSettings.showToolsChanged += ShowToolsChanged;

            SceneView.duringSceneGui += ToolsSceneGUI;

            show = ToolsSettings.ShowTools;
            
            SceneView.RepaintAll();
        }

        static void ShowToolsChanged(bool value)
        {
            show = value;
            SceneView.RepaintAll();
        }

        public static void ShowTools(bool value, bool keep = true)
        {
            show = value;
            if(keep)
            {
                ToolsSettings.ShowTools = value;
            }
            SceneView.RepaintAll();
        }

        static void ToolsSceneGUI(SceneView sceneview)
        {
            if (show)
            {
                Init();
                toolsWindow = ClampRectToSceneView(GUILayout.Window(windowID, toolsWindow, ToolsUIWindow, new GUIContent("LEGO® Tools")));
            }
        }

        static void Init()
        {
            if (!initialised)
            {
                brickBuildingOnImage = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.unity.lego.modelimporter/Textures/Brick Building On@2x.png");
                brickBuildingOffImage = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.unity.lego.modelimporter/Textures/Brick Building Off@2x.png");
                selectConnectedOnImage = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.unity.lego.modelimporter/Textures/Select Connected On@2x.png");
                selectConnectedOffImage = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.unity.lego.modelimporter/Textures/Select Connected Off@2x.png");

                brickBuildingStyle = new GUIStyle(GUIStyle.none);
                brickBuildingStyle.fixedWidth = buttonSize;
                brickBuildingStyle.fixedHeight = buttonSize;
                brickBuildingStyle.margin = new RectOffset(buttonMargin, buttonMargin, buttonMargin, buttonMargin);

                selectConnectedStyle = new GUIStyle(brickBuildingStyle);
                selectConnectedStyle.normal.background = selectConnectedOffImage;
                selectConnectedStyle.active.background = selectConnectedOnImage;

                initialised = true;
            }
        }

        static void ToolsUIWindow(int windowId)
        {
            bool brickBuildingActive = ToolsSettings.IsBrickBuildingOn;
            bool selectConnectedActive = ToolsSettings.SelectConnected;

            GUILayout.BeginHorizontal();

            // Toggle brick building
            string toggleBrickBuildingShortcut = ShortcutManager.instance.GetShortcutBinding("Main Menu/" + ToolsSettings.brickBuildingMenuPath).ToString();
            GUIContent toggleBrickBuildingContent = new GUIContent("", "Brick Building " + toggleBrickBuildingShortcut);
            brickBuildingStyle.normal.background = brickBuildingActive ? brickBuildingOnImage : brickBuildingOffImage;
            brickBuildingStyle.active.background = brickBuildingActive ? brickBuildingOffImage : brickBuildingOnImage;
            if (GUILayout.Button(toggleBrickBuildingContent, brickBuildingStyle))
            {
                ToolsSettings.IsBrickBuildingOn = !ToolsSettings.IsBrickBuildingOn;
            }

            // Toggle select connected
            string toggleSelectConnectedShortcut = ShortcutManager.instance.GetShortcutBinding("Main Menu/" + ToolsSettings.selectConnectedMenuPath).ToString();
            GUIContent toggleSelectConnectedContent = selectConnectedActive ? new GUIContent("", "Connected Brick Selection " + toggleSelectConnectedShortcut)
                                                                            : new GUIContent("", "Single Brick Selection " + toggleSelectConnectedShortcut);
            selectConnectedStyle.normal.background = selectConnectedActive ? selectConnectedOnImage : selectConnectedOffImage;
            selectConnectedStyle.active.background = selectConnectedActive ? selectConnectedOffImage : selectConnectedOnImage;
            GUI.enabled = ToolsSettings.IsBrickBuildingOn;
            if (GUILayout.Button(toggleSelectConnectedContent, selectConnectedStyle))
            {
                ToolsSettings.SelectConnected = !ToolsSettings.SelectConnected;
            }

            GUILayout.EndHorizontal();

            GUI.enabled = true;
            GUI.DragWindow();
        }

        static Rect ClampRectToSceneView(Rect rect)
        {
            var distanceFromTop = distanceFromBorders;
            if(PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                distanceFromTop += distanceFromBorders + sceneViewTopBarHeight;
            }
            Rect sceneViewArea = SceneView.lastActiveSceneView.position;
            rect.x = Mathf.Clamp(rect.x, distanceFromBorders, sceneViewArea.width - rect.width - distanceFromBorders);
            rect.y = Mathf.Clamp(rect.y, distanceFromTop + sceneViewTopBarHeight, sceneViewArea.height - rect.height - distanceFromTop);
            return rect;
        }
    }
}
