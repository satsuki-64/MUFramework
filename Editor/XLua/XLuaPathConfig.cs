using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using XLua;

namespace MUFramework.Editor.XLua
{
    /// <summary>
    /// XLua 路径管理编辑器窗口
    /// </summary>
    public class XLuaPathEditorWindow : EditorWindow
    {
        private XLuaPathConfig config;
        private Vector2 scrollPosition;
        private string newPath = "";
        private string selectedLuaFile = "Main";

        [MenuItem("MUFramework/XLua/设置Lua代码加载路径")]
        public static void ShowWindow()
        {
            var window = GetWindow<XLuaPathEditorWindow>("XLua 路径配置");
            window.minSize = new Vector2(400, 400);
            window.Show();
        }

        private void OnEnable()
        {
            LoadConfig();
        }

        private void LoadConfig()
        {
            // 查找现有的配置文件
            // 注意：如果有多个配置文件，只加载查找到的第一个，因此注意保证 XLuaPathConfig 文件的唯一性，不要创建多个
            var guids = AssetDatabase.FindAssets("t:XLuaPathConfig");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                config = AssetDatabase.LoadAssetAtPath<XLuaPathConfig>(path);
            }
            
            // 如果没有找到配置文件，创建一个新的
            if (config == null)
            {
                config = CreateInstance<XLuaPathConfig>();
                // 添加默认路径
                config.luaPaths.Add(Path.Combine(Application.dataPath, "MUFramework/ThirdParty/XLuaScripts/Lua"));
                config.luaPaths.Add(Path.Combine(Application.dataPath, "MUFramework/Runtime/Utilities/Example"));
                
                // 保存配置文件
                var folderPath = "Assets/Resources";
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                
                AssetDatabase.CreateAsset(config, Path.Combine(folderPath, "XLuaPathConfig.asset"));
                AssetDatabase.SaveAssets();
            }
            
            selectedLuaFile = config.defaultLuaFile;
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawPathList();
            DrawAddPathSection();
            DrawActions();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("XLua Lua代码加载路径管理", EditorStyles.boldLabel);
            EditorGUILayout.Space();
        }

        /// <summary>
        /// 绘制和更改当前已有的 XLua 文件路径
        /// </summary>
        private void DrawPathList()
        {
            EditorGUILayout.LabelField("当前Lua加载路径列表:", EditorStyles.boldLabel);
            
            if (config.luaPaths.Count == 0)
            {
                EditorGUILayout.HelpBox("没有配置任何Lua加载路径", MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
            
            // 遍历所有的 LuaPath 路径
            for (int i = 0; i < config.luaPaths.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                
                // 路径显示和编辑
                EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(30));
                config.luaPaths[i] = EditorGUILayout.TextField(config.luaPaths[i]);
                
                // 浏览文件夹按钮
                if (GUILayout.Button("浏览", GUILayout.Width(50))) 
                {
                    string selectedPath = EditorUtility.OpenFolderPanel("选择Lua文件夹", config.luaPaths[i], "");
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        config.luaPaths[i] = selectedPath;
                    }
                }
                
                // 删除按钮
                if (GUILayout.Button("删除", GUILayout.Width(50)))
                {
                    config.luaPaths.RemoveAt(i);
                    break;
                }
                
                EditorGUILayout.EndHorizontal();
                
                // 显示路径验证信息
                if (Directory.Exists(config.luaPaths[i]))
                {
                    var luaFiles = Directory.GetFiles(config.luaPaths[i], "*.lua", SearchOption.AllDirectories);
                    EditorGUILayout.LabelField($"  找到 {luaFiles.Length} 个Lua文件", EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("  路径不存在!", EditorStyles.miniLabel);
                }
            }
            
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 增加更多文件路径
        /// </summary>
        private void DrawAddPathSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("添加新路径:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            newPath = EditorGUILayout.TextField("路径:", newPath);
            
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                var selectedPath = EditorUtility.OpenFolderPanel("选择Lua文件夹", Application.dataPath, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    newPath = selectedPath;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("添加路径", GUILayout.Width(100)))
            {
                if (!string.IsNullOrEmpty(newPath) && Directory.Exists(newPath))
                {
                    if (!config.luaPaths.Contains(newPath))
                    {
                        config.luaPaths.Add(newPath);
                        newPath = "";
                        EditorUtility.SetDirty(config);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("提示", "该路径已存在!", "确定");
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("错误", "路径不存在或为空!", "确定");
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawActions()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("保存配置"))
            {
                SaveConfig();
            }
            
            if (GUILayout.Button("重置为默认"))
            {
                ResetToDefault();
            }
            
            if (GUILayout.Button("刷新"))
            {
                RefreshPaths();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        

        private void SaveConfig()
        {
            config.defaultLuaFile = selectedLuaFile;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("保存成功", "XLua路径配置已保存!", "确定");
        }

        private void ResetToDefault()
        {
            config.luaPaths.Clear();
            config.luaPaths.Add(Path.Combine(Application.dataPath, "MUFramework/ThirdParty/XLuaScripts/Lua"));
            config.luaPaths.Add(Path.Combine(Application.dataPath,"MUFramework/Runtime/Utilities/Example"));
            selectedLuaFile = "Main";
            EditorUtility.SetDirty(config);
        }

        private void RefreshPaths()
        {
            // 移除不存在的路径
            for (int i = config.luaPaths.Count - 1; i >= 0; i--)
            {
                var path = config.luaPaths[i];
                var fullPath = path;
                
                if (!Path.IsPathRooted(path))
                {
                    fullPath = Path.Combine(Application.dataPath, path.Replace("Assets/", ""));
                }
                
                if (!Directory.Exists(fullPath))
                {
                    config.luaPaths.RemoveAt(i);
                }
            }
            EditorUtility.SetDirty(config);
        }
    }  
}
