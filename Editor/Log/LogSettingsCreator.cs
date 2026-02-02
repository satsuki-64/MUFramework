using MUFramework.Utilities;
using UnityEditor;
using UnityEngine;

namespace MUFramework.Editor.Log
{
    public static class LogSettingsCreator
    {
        [MenuItem("MUFramework/Log/创建日志设置")]
        public static void CreateLogSettings()
        {
            var settings = ScriptableObject.CreateInstance<LogSettings>();
            
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            
            AssetDatabase.CreateAsset(settings, "Assets/Resources/LogSettings.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = settings;
            
            Debug.Log("日志设置文件已创建: Assets/Resources/LogSettings.asset");
        }
    }
}