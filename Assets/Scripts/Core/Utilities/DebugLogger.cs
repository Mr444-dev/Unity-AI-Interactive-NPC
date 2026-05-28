using UnityEngine;
using System;
using System.Diagnostics;

namespace InteractiveNPC.Core.Utilities
{
    #region Debug Logger Settings ScriptableObject
    
    /// <summary>
    /// ScriptableObject for controlling debug logging settings across the application.
    /// Allows toggling logging on/off and setting log levels from the Unity Editor.
    /// </summary>
    [CreateAssetMenu(fileName = "DebugLoggerSettings", menuName = "Interactive NPC/Core/Debug Logger Settings")]
    public class DebugLoggerSettings : ScriptableObject
    {
        #region Serialized Fields
        
        [Header("Logging Configuration")]
        [SerializeField, Tooltip("Enable or disable all logging")]
        private bool _enableLogging = true;
        
        [SerializeField, Tooltip("Minimum log level to display")]
        private LogLevel _minimumLogLevel = LogLevel.Info;
        
        [SerializeField, Tooltip("Include timestamp in log messages")]
        private bool _includeTimestamp = true;
        
        [SerializeField, Tooltip("Include class name in log messages")]
        private bool _includeClassName = true;
        
        [Header("Category Filters")]
        [SerializeField, Tooltip("Enable memory system logs")]
        private bool _enableMemoryLogs = true;
        
        [SerializeField, Tooltip("Enable personality system logs")]
        private bool _enablePersonalityLogs = true;
        
        [SerializeField, Tooltip("Enable LLM integration logs")]
        private bool _enableLLMLogs = true;
        
        [SerializeField, Tooltip("Enable speech system logs")]
        private bool _enableSpeechLogs = true;
        
        [SerializeField, Tooltip("Enable lip sync logs")]
        private bool _enableLipSyncLogs = true;
        
        [SerializeField, Tooltip("Enable NPC controller logs")]
        private bool _enableNPCLogs = true;
        
        [SerializeField, Tooltip("Enable UI system logs")]
        private bool _enableUILogs = true;
        
        #endregion
        
        #region Properties
        
        public bool EnableLogging => _enableLogging;
        public LogLevel MinimumLogLevel => _minimumLogLevel;
        public bool IncludeTimestamp => _includeTimestamp;
        public bool IncludeClassName => _includeClassName;
        public bool EnableMemoryLogs => _enableMemoryLogs;
        public bool EnablePersonalityLogs => _enablePersonalityLogs;
        public bool EnableLLMLogs => _enableLLMLogs;
        public bool EnableSpeechLogs => _enableSpeechLogs;
        public bool EnableLipSyncLogs => _enableLipSyncLogs;
        public bool EnableNPCLogs => _enableNPCLogs;
        public bool EnableUILogs => _enableUILogs;
        
        #endregion
    }
    
    #endregion
    
    #region Enums
    
    /// <summary>
    /// Log severity levels for filtering debug output.
    /// </summary>
    public enum LogLevel
    {
        Verbose = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        None = 4
    }
    
    /// <summary>
    /// Categories for organizing log messages by system.
    /// </summary>
    public enum LogCategory
    {
        General,
        Memory,
        Personality,
        LLM,
        Speech,
        LipSync,
        NPC,
        UI
    }
    
    #endregion
    
    #region Debug Logger Static Class
    
    /// <summary>
    /// Custom debug logging system with editor toggle, category filtering, and log levels.
    /// Provides centralized logging control for development and production builds.
    /// </summary>
    public static class DebugLogger
    {
        #region Private Fields
        
        private static DebugLoggerSettings _settings;
        private static bool _isInitialized;
        private const string SettingsPath = "Settings/DebugLoggerSettings";
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize the debug logger with settings from Resources.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            _settings = Resources.Load<DebugLoggerSettings>(SettingsPath);
            
            if (_settings == null)
            {
                UnityEngine.Debug.LogWarning("[DebugLogger] Settings not found at Resources/" + SettingsPath + 
                    ". Using default settings. Create a DebugLoggerSettings asset in Resources/Settings/");
            }
            
            _isInitialized = true;
        }
        
        /// <summary>
        /// Manually set settings reference (useful for testing or custom initialization).
        /// </summary>
        public static void SetSettings(DebugLoggerSettings settings)
        {
            _settings = settings;
            _isInitialized = true;
        }
        
        #endregion
        
        #region Public Logging Methods
        
        /// <summary>
        /// Log a verbose message (lowest priority, most detailed).
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogVerbose(string message, LogCategory category = LogCategory.General, UnityEngine.Object context = null)
        {
            Log(message, LogLevel.Verbose, category, context);
        }
        
        /// <summary>
        /// Log an info message (general information).
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogInfo(string message, LogCategory category = LogCategory.General, UnityEngine.Object context = null)
        {
            Log(message, LogLevel.Info, category, context);
        }
        
        /// <summary>
        /// Log a warning message (potential issues).
        /// </summary>
        public static void LogWarning(string message, LogCategory category = LogCategory.General, UnityEngine.Object context = null)
        {
            Log(message, LogLevel.Warning, category, context);
        }
        
        /// <summary>
        /// Log an error message (critical issues).
        /// </summary>
        public static void LogError(string message, LogCategory category = LogCategory.General, UnityEngine.Object context = null)
        {
            Log(message, LogLevel.Error, category, context);
        }
        
        /// <summary>
        /// Log an exception with full stack trace.
        /// </summary>
        public static void LogException(Exception exception, LogCategory category = LogCategory.General, UnityEngine.Object context = null)
        {
            if (!ShouldLog(LogLevel.Error, category)) return;
            
            string formattedMessage = FormatMessage($"Exception: {exception.Message}\n{exception.StackTrace}", LogLevel.Error, category);
            UnityEngine.Debug.LogError(formattedMessage, context);
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Core logging method that handles formatting and filtering.
        /// </summary>
        private static void Log(string message, LogLevel level, LogCategory category, UnityEngine.Object context)
        {
            if (!ShouldLog(level, category)) return;
            
            string formattedMessage = FormatMessage(message, level, category);
            
            switch (level)
            {
                case LogLevel.Verbose:
                case LogLevel.Info:
                    UnityEngine.Debug.Log(formattedMessage, context);
                    break;
                case LogLevel.Warning:
                    UnityEngine.Debug.LogWarning(formattedMessage, context);
                    break;
                case LogLevel.Error:
                    UnityEngine.Debug.LogError(formattedMessage, context);
                    break;
            }
        }
        
        /// <summary>
        /// Check if a message should be logged based on settings.
        /// </summary>
        private static bool ShouldLog(LogLevel level, LogCategory category)
        {
            // Always log errors in builds without settings
            if (!_isInitialized || _settings == null)
            {
                return level >= LogLevel.Warning;
            }
            
            if (!_settings.EnableLogging) return false;
            if (level < _settings.MinimumLogLevel) return false;
            
            // Check category-specific filters
            return category switch
            {
                LogCategory.Memory => _settings.EnableMemoryLogs,
                LogCategory.Personality => _settings.EnablePersonalityLogs,
                LogCategory.LLM => _settings.EnableLLMLogs,
                LogCategory.Speech => _settings.EnableSpeechLogs,
                LogCategory.LipSync => _settings.EnableLipSyncLogs,
                LogCategory.NPC => _settings.EnableNPCLogs,
                LogCategory.UI => _settings.EnableUILogs,
                _ => true
            };
        }
        
        /// <summary>
        /// Format log message with optional timestamp, category, and level.
        /// </summary>
        private static string FormatMessage(string message, LogLevel level, LogCategory category)
        {
            string prefix = "";
            
            if (_settings != null && _settings.IncludeTimestamp)
            {
                prefix += $"[{DateTime.Now:HH:mm:ss.fff}] ";
            }
            
            prefix += $"[{level}] ";
            
            if (category != LogCategory.General)
            {
                prefix += $"[{category}] ";
            }
            
            return prefix + message;
        }
        
        #endregion
        
        #region Lifecycle Logging Helpers
        
        /// <summary>
        /// Log Awake lifecycle event for a MonoBehaviour.
        /// </summary>
        public static void LogAwake(MonoBehaviour behaviour, LogCategory category = LogCategory.General)
        {
            LogVerbose($"{behaviour.GetType().Name}.Awake() called on {behaviour.gameObject.name}", category, behaviour);
        }
        
        /// <summary>
        /// Log Start lifecycle event for a MonoBehaviour.
        /// </summary>
        public static void LogStart(MonoBehaviour behaviour, LogCategory category = LogCategory.General)
        {
            LogVerbose($"{behaviour.GetType().Name}.Start() called on {behaviour.gameObject.name}", category, behaviour);
        }
        
        /// <summary>
        /// Log OnEnable lifecycle event for a MonoBehaviour.
        /// </summary>
        public static void LogOnEnable(MonoBehaviour behaviour, LogCategory category = LogCategory.General)
        {
            LogVerbose($"{behaviour.GetType().Name}.OnEnable() called on {behaviour.gameObject.name}", category, behaviour);
        }
        
        /// <summary>
        /// Log OnDisable lifecycle event for a MonoBehaviour.
        /// </summary>
        public static void LogOnDisable(MonoBehaviour behaviour, LogCategory category = LogCategory.General)
        {
            LogVerbose($"{behaviour.GetType().Name}.OnDisable() called on {behaviour.gameObject.name}", category, behaviour);
        }
        
        /// <summary>
        /// Log OnDestroy lifecycle event for a MonoBehaviour.
        /// </summary>
        public static void LogOnDestroy(MonoBehaviour behaviour, LogCategory category = LogCategory.General)
        {
            LogVerbose($"{behaviour.GetType().Name}.OnDestroy() called on {behaviour.gameObject.name}", category, behaviour);
        }
        
        #endregion
    }
    
    #endregion
}
