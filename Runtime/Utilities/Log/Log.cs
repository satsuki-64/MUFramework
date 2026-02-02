// Log.cs
using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace MUFramework.Utilities
{
    /// <summary>
    /// 日志系统核心类
    /// </summary>
    public static class Log
    {
        private static LogSettings _settings;
        private static StreamWriter _logFileWriter;
        private static string _logFilePath;
        private static readonly StringBuilder _logBuilder = new StringBuilder();

        static Log()
        {
            LoadSettings();
            InitializeLogFile();
            Application.quitting += OnApplicationQuit;
        }

        #region Log 方法
        
        public static void Debug(string message, LogModule module = LogModule.Core)
        {
            if (IsModuleEnabled(module))
            {
                string formattedMessage = FormatMessage("DEBUG", message, module);
                UnityEngine.Debug.Log(formattedMessage);
                WriteToFile(formattedMessage);
            }
        }
        
        public static void Warning(string message, LogModule module = LogModule.Core)
        {
            if (IsModuleEnabled(module))
            {
                string formattedMessage = FormatMessage("WARNING", message, module);
                UnityEngine.Debug.LogWarning(formattedMessage);
                WriteToFile(formattedMessage);
            }
        }
        
        public static void Error(string message, LogModule module = LogModule.Core)
        {
            if (IsModuleEnabled(module))
            {
                string formattedMessage = FormatMessage("ERROR", message, module);
                UnityEngine.Debug.LogError(formattedMessage);
                WriteToFile(formattedMessage);
            }
        }

        #endregion

        #region Log加载与持久化

        private static bool IsModuleEnabled(LogModule module)
        {
            return _settings != null && (_settings.enabledModules.Contains(module));
        }

        private static string FormatMessage(string logType, string message, LogModule module)
        {
            _logBuilder.Clear();
            _logBuilder.Append($"[{module}]");
            _logBuilder.Append($" {message}");
            
            return _logBuilder.ToString();
        }

        private static void WriteToFile(string message)
        {
            if (_settings != null && _settings.enableFileLogging && _logFileWriter != null)
            {
                try
                {
                    _logFileWriter.WriteLine(message);
                    _logFileWriter.Flush();
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogError($"写入日志文件失败: {e.Message}");
                }
            }
        }

        private static void LoadSettings()
        {
            _settings = Resources.Load<LogSettings>("LogSettings");
            if (_settings == null)
            {
                _settings = ScriptableObject.CreateInstance<LogSettings>();
                LogModule[] temp = (LogModule[])Enum.GetValues(typeof(LogModule));
                _settings.enabledModules.AddRange(temp);
                _settings.enableFileLogging = true;
            }
        }

        private static void InitializeLogFile()
        {
            if (_settings != null && _settings.enableFileLogging)
            {
                try
                {
                    string logDirectory = Path.Combine(Application.persistentDataPath, "Logs");
                    if (!Directory.Exists(logDirectory))
                    {
                        Directory.CreateDirectory(logDirectory);
                    }

                    string fileName = $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                    _logFilePath = Path.Combine(logDirectory, fileName);
                    _logFileWriter = new StreamWriter(_logFilePath, true, Encoding.UTF8);
                    
                    _logFileWriter.WriteLine($"=== 日志开始于 {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                    _logFileWriter.Flush();
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogError($"初始化日志文件失败: {e.Message}");
                }
            }
        }

        private static void OnApplicationQuit()
        {
            if (_logFileWriter != null)
            {
                try
                {
                    _logFileWriter.WriteLine($"=== 日志结束于 {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                    _logFileWriter.Close();
                    _logFileWriter = null;
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogError($"关闭日志文件失败: {e.Message}");
                }
            }
        }

        #endregion
    }
}