using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using MUFramework.Utilities;

namespace MUFramework.Editor.Log
{
    public class LogSettingsWindow : EditorWindow
    {
        private LogSettings _settings;
        private Vector2 _scrollPosition;
        private string[] _moduleNames;
        private LogModule[] _moduleValues;

        [MenuItem("MUFramework/Log/日志设置界面")]
        public static void ShowWindow()
        {
            GetWindow<LogSettingsWindow>("日志设置");
        }

        private void OnEnable()
        {
            LoadSettings();
            InitializeModuleData();
        }

        private void OnGUI()
        {
            // 确保 ScrollView 的Begin/End始终配对
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("日志模块设置", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 核心修复2：空值防护，显示提示并提供手动创建按钮，而非自动创建
            if (_settings == null)
            {
                EditorGUILayout.HelpBox("未找到日志设置文件，请先创建默认设置！", MessageType.Error);
                if (GUILayout.Button("创建默认日志设置", GUILayout.Height(30)))
                {
                    CreateDefaultSettings();
                    // 创建后重新加载
                    LoadSettings();
                    InitializeModuleData();
                }
                
                // 空值时直接结束布局，避免后续访问_settings
                EditorGUILayout.EndScrollView();
                return;
            }

            // 显示所有模块的开关
            EditorGUILayout.LabelField("启用模块:");
            EditorGUI.indentLevel++;

            // 提前解析枚举，避免重复调用 Enum.Parse
            for (int i = 0; i < _moduleValues.Length; i++)
            {
                LogModule currentModule = _moduleValues[i];
                bool isEnabled = _settings.enabledModules.Contains(currentModule);

                bool newEnabled = EditorGUILayout.Toggle(_moduleNames[i], isEnabled);
                
                if (newEnabled != isEnabled)
                {
                    if (newEnabled)
                    {
                        if(!_settings.enabledModules.Contains(currentModule))
                            _settings.enabledModules.Add(currentModule);
                    }
                    else
                    {
                        if(_settings.enabledModules.Contains(currentModule))
                            _settings.enabledModules.Remove(currentModule);
                    }
                    EditorUtility.SetDirty(_settings);
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            // 文件日志设置（已确保_settings非null）
            EditorGUILayout.LabelField("文件日志设置", EditorStyles.boldLabel);
            bool newFileLogging = EditorGUILayout.Toggle("启用文件日志", _settings.enableFileLogging);
            if (newFileLogging != _settings.enableFileLogging)
            {
                _settings.enableFileLogging = newFileLogging;
                EditorUtility.SetDirty(_settings);
            }

            EditorGUILayout.Space();

            // 操作按钮
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("保存设置"))
                {
                    SaveSettings();
                }
                
                if (GUILayout.Button("重置为默认"))
                {
                    ResetToDefault();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("日志文件保存在: " + Application.persistentDataPath + "/Logs/", MessageType.Info);

            // 核心修复1：确保EndScrollView始终执行
            EditorGUILayout.EndScrollView();
        }

        private void InitializeModuleData()
        {
            if (typeof(LogModule) == null) return;
            // 得到所有的枚举类型
            _moduleValues = (LogModule[])Enum.GetValues(typeof(LogModule));
            // 将其转化为字符串
            _moduleNames = _moduleValues.Select(m => m.ToString()).ToArray();
        }

        private void LoadSettings()
        {
            _settings = Resources.Load<LogSettings>("LogSettings");
        }

        /// <summary>
        /// 创建默认的 Log 设置
        /// </summary>
        private void CreateDefaultSettings()
        {
            // 防护：避免重复创建
            if (_settings != null) return;

            try
            {
                // 1. 确保 Resources 文件夹存在（处理路径分隔符和权限问题）
                string resourcesPath = "Assets/Resources";
                if (!AssetDatabase.IsValidFolder(resourcesPath))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                    AssetDatabase.Refresh(); // 创建后立即刷新，确保后续操作能识别
                }

                // 2. 创建 LogSettings 实例（必须继承 ScriptableObject）
                _settings = CreateInstance<LogSettings>();
        
                // 3. 关键修复：初始化 enabledModules 集合（核心问题！）
                if (_settings.enabledModules == null)
                {
                    _settings.enabledModules = new List<LogModule>();
                }

                // 4. 初始化默认值（增加枚举空值防护）
                _settings.enableFileLogging = true;
                LogModule[] allModules = (LogModule[])Enum.GetValues(typeof(LogModule));
                if (allModules.Length > 0)
                {
                    _settings.enabledModules.AddRange(allModules);
                }
                else
                {
                    Debug.LogWarning("LogModule 枚举中未定义任何模块！");
                }

                // 5. 创建资源文件（增加路径重复防护）
                string assetPath = $"{resourcesPath}/LogSettings.asset";
                if (AssetDatabase.LoadAssetAtPath<LogSettings>(assetPath) != null)
                {
                    Debug.LogWarning($"日志设置文件已存在：{assetPath}");
                    EditorUtility.FocusProjectWindow();
                    Selection.activeObject = AssetDatabase.LoadAssetAtPath<LogSettings>(assetPath);
                    return;
                }

                AssetDatabase.CreateAsset(_settings, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // 6. 选中创建的文件，方便用户查看
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = _settings;

                Debug.Log($"已成功创建默认日志设置：{assetPath}");
            }
            catch (Exception e)
            {
                // 捕获异常并提示，避免程序崩溃
                Debug.LogError($"创建日志设置失败：{e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("错误", $"创建默认设置失败：{e.Message}", "确定");
            }
        }

        private void SaveSettings()
        {
            if (_settings != null)
            {
                EditorUtility.SetDirty(_settings);
                AssetDatabase.SaveAssets();
                Debug.Log("日志设置已保存");
            }
        }

        private void ResetToDefault()
        {
            if (_settings != null)
            {
                // 清空后添加所有模块
                _settings.enabledModules.Clear();
                _settings.enabledModules.AddRange((LogModule[])Enum.GetValues(typeof(LogModule)));
                _settings.enableFileLogging = true;
                
                EditorUtility.SetDirty(_settings);
                Debug.Log("已重置为默认设置");
            }
        }
    }
}