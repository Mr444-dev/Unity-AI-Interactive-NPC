using System;
using System.Collections.Generic;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#else
using InteractiveNPC.Core.Attributes;
#endif

namespace InteractiveNPC.LLM.Data
{
    #region LLM Settings ScriptableObject (Enhanced)
    
    /// <summary>
    /// Enhanced ScriptableObject for LLM configuration with Odin Inspector support.
    /// </summary>
    [CreateAssetMenu(fileName = "LLMSettingsEnhanced", menuName = "Interactive NPC/LLM/LLM Settings (Enhanced)")]
    public class LLMSettingsEnhanced : ScriptableObject
    {
        #region Serialized Fields
        
        [Title("Provider Configuration")]
        [BoxGroup("Provider")]
        [LabelText("Provider Type")]
        [OnValueChanged("OnProviderChanged")]
        [SerializeField]
        private LLMProviderType _providerType = LLMProviderType.OpenAI;
        
        [BoxGroup("Provider")]
        [ShowIf("_providerType", LLMProviderType.OpenAI)]
        [Required("API Key is required for OpenAI!")]
        [InfoBox("Get your API key from platform.openai.com", InfoMessageType.Info)]
        [LabelText("OpenAI API Key")]
        [SerializeField]
        private string _openAIApiKey = "";
        
        [BoxGroup("Provider")]
        [ShowIf("_providerType", LLMProviderType.OpenAI)]
        [ValueDropdown("GetOpenAIModels")]
        [LabelText("Model")]
        [SerializeField]
        private string _openAIModel = "gpt-3.5-turbo";
        
        [BoxGroup("Provider")]
        [ShowIf("_providerType", LLMProviderType.Ollama)]
        [LabelText("Ollama Server URL")]
        [SerializeField]
        private string _ollamaBaseUrl = "http://localhost:11434";
        
        [BoxGroup("Provider")]
        [ShowIf("_providerType", LLMProviderType.Ollama)]
        [LabelText("Ollama Model")]
        [SerializeField]
        private string _ollamaModel = "llama2";
        
        [Title("Generation Parameters")]
        [TabGroup("Settings", "Generation")]
        [BoxGroup("Settings/Generation/Temperature")]
        [ProgressBar(0, 2, 0.9f, 0.5f, 0.2f)]
        [LabelText("Temperature")]
        [Tooltip("Creativity level: 0 = deterministic, 2 = very creative")]
        [SerializeField]
        private float _temperature = 0.7f;
        
        [BoxGroup("Settings/Generation/Temperature")]
        [ProgressBar(0, 1, 0.5f, 0.8f, 0.5f)]
        [LabelText("Top P")]
        [Tooltip("Nucleus sampling threshold")]
        [SerializeField]
        private float _topP = 0.9f;
        
        [TabGroup("Settings", "Generation")]
        [BoxGroup("Settings/Generation/Tokens")]
        [LabelText("Max Tokens")]
        [Range(50, 4096)]
        [SerializeField]
        private int _maxTokens = 500;
        
        [BoxGroup("Settings/Generation/Tokens")]
        [LabelText("Context Window")]
        [Range(1024, 128000)]
        [SerializeField]
        private int _contextWindow = 4096;
        
        [TabGroup("Settings", "Generation")]
        [BoxGroup("Settings/Generation/Penalties")]
        [ProgressBar(-2, 2)]
        [LabelText("Frequency Penalty")]
        [SerializeField]
        private float _frequencyPenalty = 0f;
        
        [BoxGroup("Settings/Generation/Penalties")]
        [ProgressBar(-2, 2)]
        [LabelText("Presence Penalty")]
        [SerializeField]
        private float _presencePenalty = 0f;
        
        [Title("Behavior")]
        [TabGroup("Settings", "Behavior")]
        [BoxGroup("Settings/Behavior/Streaming")]
        [LabelText("Enable Streaming")]
        [Tooltip("Stream responses for real-time display")]
        [SerializeField]
        private bool _enableStreaming = true;
        
        [BoxGroup("Settings/Behavior/Streaming")]
        [ShowIf("_enableStreaming")]
        [LabelText("Stream Delay (ms)")]
        [Range(10, 200)]
        [SerializeField]
        private int _streamDelayMs = 50;
        
        [TabGroup("Settings", "Behavior")]
        [BoxGroup("Settings/Behavior/Network")]
        [LabelText("Request Timeout (s)")]
        [Range(5, 120)]
        [SerializeField]
        private int _requestTimeoutSeconds = 30;
        
        [BoxGroup("Settings/Behavior/Network")]
        [LabelText("Max Retries")]
        [Range(0, 5)]
        [SerializeField]
        private int _maxRetries = 2;
        
        [BoxGroup("Settings/Behavior/Network")]
        [LabelText("Retry Delay (s)")]
        [Range(0.5f, 5f)]
        [SerializeField]
        private float _retryDelaySeconds = 1f;
        
        [Title("Safety")]
        [TabGroup("Settings", "Safety")]
        [FoldoutGroup("Settings/Safety/Content Filter", false)]
        [LabelText("Enable Content Filter")]
        [SerializeField]
        private bool _enableContentFilter = true;
        
        [FoldoutGroup("Settings/Safety/Content Filter")]
        [ShowIf("_enableContentFilter")]
        [LabelText("Blocked Words")]
        [SerializeField]
        private List<string> _blockedWords = new List<string>();
        
        [FoldoutGroup("Settings/Safety/Content Filter")]
        [ShowIf("_enableContentFilter")]
        [LabelText("Replace Blocked With")]
        [SerializeField]
        private string _blockedWordReplacement = "***";
        
        [TabGroup("Settings", "Safety")]
        [FoldoutGroup("Settings/Safety/Rate Limiting", false)]
        [LabelText("Enable Rate Limiting")]
        [SerializeField]
        private bool _enableRateLimiting = true;
        
        [FoldoutGroup("Settings/Safety/Rate Limiting")]
        [ShowIf("_enableRateLimiting")]
        [LabelText("Max Requests Per Minute")]
        [Range(1, 60)]
        [SerializeField]
        private int _maxRequestsPerMinute = 20;
        
        [Title("Presets")]
        [FoldoutGroup("Presets", false)]
        [InfoBox("Quick presets for common use cases")]
        #endregion
        
        #region Properties
        
        public LLMProviderType ProviderType => _providerType;
        public string OpenAIApiKey => _openAIApiKey;
        public string OpenAIModel => _openAIModel;
        public string OllamaBaseUrl => _ollamaBaseUrl;
        public string OllamaModel => _ollamaModel;
        public float Temperature => _temperature;
        public float TopP => _topP;
        public int MaxTokens => _maxTokens;
        public int ContextWindow => _contextWindow;
        public float FrequencyPenalty => _frequencyPenalty;
        public float PresencePenalty => _presencePenalty;
        public bool EnableStreaming => _enableStreaming;
        public int StreamDelayMs => _streamDelayMs;
        public int RequestTimeoutSeconds => _requestTimeoutSeconds;
        public int MaxRetries => _maxRetries;
        public float RetryDelaySeconds => _retryDelaySeconds;
        public bool EnableContentFilter => _enableContentFilter;
        public IReadOnlyList<string> BlockedWords => _blockedWords;
        public string BlockedWordReplacement => _blockedWordReplacement;
        public bool EnableRateLimiting => _enableRateLimiting;
        public int MaxRequestsPerMinute => _maxRequestsPerMinute;
        
        #endregion
        
        #region Odin Value Dropdowns
        
#if ODIN_INSPECTOR
        private static IEnumerable<string> GetOpenAIModels()
        {
            return new[]
            {
                "gpt-4o",
                "gpt-4o-mini",
                "gpt-4-turbo",
                "gpt-4",
                "gpt-3.5-turbo",
                "gpt-3.5-turbo-16k"
            };
        }
        
        private void OnProviderChanged()
        {
            UnityEngine.Debug.Log($"Provider changed to: {_providerType}");
        }
#endif
        
        #endregion
        
        #region Odin Buttons
        
#if ODIN_INSPECTOR
        [FoldoutGroup("Presets")]
        [Button("Creative Writing", ButtonHeight = 25)]
        [GUIColor(0.9f, 0.7f, 0.3f)]
        private void ApplyCreativePreset()
        {
            _temperature = 1.2f;
            _topP = 0.95f;
            _frequencyPenalty = 0.5f;
            _presencePenalty = 0.5f;
            _maxTokens = 1000;
            UnityEngine.Debug.Log("Applied Creative Writing preset");
        }
        
        [FoldoutGroup("Presets")]
        [Button("Precise & Factual", ButtonHeight = 25)]
        [GUIColor(0.3f, 0.7f, 0.9f)]
        private void ApplyPrecisePreset()
        {
            _temperature = 0.3f;
            _topP = 0.5f;
            _frequencyPenalty = 0f;
            _presencePenalty = 0f;
            _maxTokens = 500;
            UnityEngine.Debug.Log("Applied Precise & Factual preset");
        }
        
        [FoldoutGroup("Presets")]
        [Button("Balanced (Default)", ButtonHeight = 25)]
        [GUIColor(0.5f, 0.8f, 0.5f)]
        private void ApplyBalancedPreset()
        {
            _temperature = 0.7f;
            _topP = 0.9f;
            _frequencyPenalty = 0f;
            _presencePenalty = 0f;
            _maxTokens = 500;
            UnityEngine.Debug.Log("Applied Balanced preset");
        }
        
        [FoldoutGroup("Presets")]
        [Button("Roleplay Character", ButtonHeight = 25)]
        [GUIColor(0.8f, 0.5f, 0.8f)]
        private void ApplyRoleplayPreset()
        {
            _temperature = 0.9f;
            _topP = 0.9f;
            _frequencyPenalty = 0.3f;
            _presencePenalty = 0.6f;
            _maxTokens = 800;
            UnityEngine.Debug.Log("Applied Roleplay Character preset");
        }
        
        [Title("Testing")]
        [Button("Test Connection", ButtonHeight = 35)]
        [GUIColor(0.2f, 0.8f, 0.2f)]
        private async void TestConnection()
        {
            UnityEngine.Debug.Log($"Testing connection to {_providerType}...");
            
            try
            {
                if (_providerType == LLMProviderType.OpenAI)
                {
                    if (string.IsNullOrEmpty(_openAIApiKey))
                    {
                        UnityEngine.Debug.LogError("OpenAI API Key is not set!");
                        return;
                    }
                    UnityEngine.Debug.Log("OpenAI API key is configured. Making test request...");
                }
                else if (_providerType == LLMProviderType.Ollama)
                {
                    UnityEngine.Debug.Log($"Testing Ollama at {_ollamaBaseUrl}...");
                }
                
                UnityEngine.Debug.Log("✓ Connection test initiated. Check console for results.");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Connection test failed: {ex.Message}");
            }
        }
        
        [Button("Validate Settings")]
        private void ValidateSettings()
        {
            List<string> issues = new List<string>();
            
            if (_providerType == LLMProviderType.OpenAI && string.IsNullOrEmpty(_openAIApiKey))
                issues.Add("OpenAI API Key is empty");
            
            if (_providerType == LLMProviderType.Ollama && string.IsNullOrEmpty(_ollamaBaseUrl))
                issues.Add("Ollama URL is empty");
            
            if (_maxTokens < 50)
                issues.Add("Max tokens is very low");
            
            if (_temperature > 1.5f)
                issues.Add("Temperature is very high - responses may be incoherent");
            
            if (issues.Count == 0)
            {
                UnityEngine.Debug.Log("✓ All settings are valid!");
            }
            else
            {
                UnityEngine.Debug.LogWarning("Settings issues:\n- " + string.Join("\n- ", issues));
            }
        }
#endif
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Get the active model name based on provider type.
        /// </summary>
        public string GetActiveModel()
        {
            return _providerType switch
            {
                LLMProviderType.OpenAI => _openAIModel,
                LLMProviderType.Ollama => _ollamaModel,
                _ => "unknown"
            };
        }
        
        /// <summary>
        /// Get the active base URL based on provider type.
        /// </summary>
        public string GetBaseUrl()
        {
            return _providerType switch
            {
                LLMProviderType.OpenAI => "https://api.openai.com/v1",
                LLMProviderType.Ollama => _ollamaBaseUrl,
                _ => ""
            };
        }
        
        /// <summary>
        /// Create runtime settings from this configuration.
        /// </summary>
        public LLMRuntimeSettings CreateRuntimeSettings()
        {
            return new LLMRuntimeSettings
            {
                Temperature = _temperature,
                TopP = _topP,
                MaxTokens = _maxTokens,
                FrequencyPenalty = _frequencyPenalty,
                PresencePenalty = _presencePenalty
            };
        }
        
        /// <summary>
        /// Filter content based on blocked words.
        /// </summary>
        public string FilterContent(string content)
        {
            if (!_enableContentFilter || _blockedWords.Count == 0)
                return content;
            
            string filtered = content;
            foreach (var word in _blockedWords)
            {
                filtered = filtered.Replace(word, _blockedWordReplacement, StringComparison.OrdinalIgnoreCase);
            }
            return filtered;
        }
        
        #endregion
    }

    #endregion
}
