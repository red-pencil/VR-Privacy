// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using UnityEditor;

namespace LEGOModelImporter
{
    public class ToolsSettingsWindow : EditorWindow
    {
        const string sceneBrickBuildingSettingsMenuPath = "LEGO Tools/Brick Building Settings";

        [MenuItem(sceneBrickBuildingSettingsMenuPath, priority = 30)]
        private static void ShowSettingsWindow()
        {
            ToolsSettingsWindow settings = (ToolsSettingsWindow)EditorWindow.GetWindow(typeof(ToolsSettingsWindow));
            settings.Show();
        }

        [MenuItem(sceneBrickBuildingSettingsMenuPath, validate = true)]
        private static bool ValidateBrickBuildingSettings()
        {
            return !EditorApplication.isPlaying;
        }

        private void OnGUI()
        {
            var snapDistance = EditorGUILayout.FloatField("Sticky Snap Distance", ToolsSettings.StickySnapDistance);
            ToolsSettings.StickySnapDistance = snapDistance;

            var maxTries = EditorGUILayout.IntSlider("Max Tries Per Brick", ToolsSettings.MaxTriesPerBrick, 1, 20);
            ToolsSettings.MaxTriesPerBrick = maxTries;
        }

    }
}