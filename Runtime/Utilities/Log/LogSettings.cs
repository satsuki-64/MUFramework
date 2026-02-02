using System.Collections.Generic;
using UnityEngine;

namespace MUFramework.Utilities
{
    /// <summary>
    /// 日志设置数据
    /// </summary>
    public class LogSettings : ScriptableObject
    {
        [Header("启用的日志模块")]
        public List<LogModule> enabledModules;
        
        [Header("启用文件日志记录")]
        public bool enableFileLogging = true;
        
        [Header("日志文件设置")]
        public string logFileNameFormat = "log_{0}.txt";
    }
}