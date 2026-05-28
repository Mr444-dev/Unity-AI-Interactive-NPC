using System;
using System.Collections.Generic;
using UnityEngine;

namespace InteractiveNPC.LLM.Data
{
    #region LLM Provider Type Enum
    
    /// <summary>
    /// Available LLM provider types.
    /// </summary>
    public enum LLMProviderType
    {
        OpenAI,
        Ollama,
        Custom
    }
    
    #endregion
    
    #region LLM Settings ScriptableObject
    
    /// <summary>
    /// ScriptableObject for configuring LLM provider settings.
    /// Stores API keys, model selection, and generation parameters.
    /// </summary>
    [CreateAssetMenu(fileName = "LLMSettings", menuName = "Interactive NPC/LLM/LLM Settings")]
    public class LLMSettings : ScriptableObject
    {
        #region Serialized Fields
        
        [Header("Provider Configuration")]
        [SerializeField, Tooltip("Which LLM provider to use")]
        private LLMProviderType _providerType = LLMProviderType.OpenAI;
        
        [Header("OpenAI Settings")]
        [SerializeField, Tooltip("OpenAI API Key (keep secret!)")]
        private string _openAIApiKey = "";
        
        [SerializeField, Tooltip("OpenAI model to use")]
        private OpenAIModel _openAIModel = OpenAIModel.GPT4oMini;
        
        [SerializeField, Tooltip("OpenAI API base URL")]
        private string _openAIBaseUrl = "https://api.openai.com/v1";
        
        [Header("Ollama Settings")]
        [SerializeField, Tooltip("Ollama server URL")]
        private string _ollamaUrl = "http://localhost:11434";
        
        [SerializeField, Tooltip("Ollama model name")]
        private string _ollamaModel = "llama3.2";
        
        [Header("Generation Parameters")]
        [SerializeField, Tooltip("Temperature (0-2): Lower = more focused, Higher = more creative")]
        [Range(0f, 2f)]
        private float _temperature = 0.7f;
        
        [SerializeField, Tooltip("Maximum tokens to generate")]
        [Range(50, 4096)]
        private int _maxTokens = 500;
        
        [SerializeField, Tooltip("Top P (nucleus sampling)")]
        [Range(0f, 1f)]
        private float _topP = 0.9f;
        
        [SerializeField, Tooltip("Frequency penalty (reduce repetition)")]
        [Range(-2f, 2f)]
        private float _frequencyPenalty = 0.3f;
        
        [SerializeField, Tooltip("Presence penalty (encourage new topics)")]
        [Range(-2f, 2f)]
        private float _presencePenalty = 0.2f;
        
        [Header("Behavior Settings")]
        [SerializeField, Tooltip("Enable streaming responses")]
        private bool _enableStreaming = true;
        
        [SerializeField, Tooltip("Request timeout in seconds")]
        [Range(5f, 120f)]
        private float _timeoutSeconds = 30f;
        
        [SerializeField, Tooltip("Number of retries on failure")]
        [Range(0, 5)]
        private int _maxRetries = 2;
        
        [SerializeField, Tooltip("Delay between retries in seconds")]
        [Range(0.5f, 10f)]
        private float _retryDelaySeconds = 1f;
        
        [Header("Safety Settings")]
        [SerializeField, Tooltip("Content filter level")]
        private ContentFilterLevel _contentFilter = ContentFilterLevel.Moderate;
        
        [SerializeField, Tooltip("Words to filter from responses")]
        private List<string> _blockedWords = new List<string>();
        
        #endregion
        
        #region Properties
        
        public LLMProviderType ProviderType => _providerType;
        
        // OpenAI
        public string OpenAIApiKey => _openAIApiKey;
        public OpenAIModel OpenAIModel => _openAIModel;
        public string OpenAIBaseUrl => _openAIBaseUrl;
        public string OpenAIModelName => GetOpenAIModelName();
        
        // Ollama
        public string OllamaUrl => _ollamaUrl;
        public string OllamaModel => _ollamaModel;
        
        // Generation
        public float Temperature => _temperature;
        public int MaxTokens => _maxTokens;
        public float TopP => _topP;
        public float FrequencyPenalty => _frequencyPenalty;
        public float PresencePenalty => _presencePenalty;
        
        // Behavior
        public bool EnableStreaming => _enableStreaming;
        public float TimeoutSeconds => _timeoutSeconds;
        public int MaxRetries => _maxRetries;
        public float RetryDelaySeconds => _retryDelaySeconds;
        
        // Safety
        public ContentFilterLevel ContentFilter => _contentFilter;
        public IReadOnlyList<string> BlockedWords => _blockedWords;
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Get the appropriate API endpoint URL.
        /// </summary>
        public string GetEndpointUrl()
        {
            return _providerType switch
            {
                LLMProviderType.OpenAI => $"{_openAIBaseUrl}/chat/completions",
                LLMProviderType.Ollama => $"{_ollamaUrl}/api/generate",
                _ => ""
            };
        }
        
        /// <summary>
        /// Create a copy of settings for runtime modification.
        /// </summary>
        public LLMRuntimeSettings CreateRuntimeSettings()
        {
            return new LLMRuntimeSettings
            {
                Temperature = _temperature,
                MaxTokens = _maxTokens,
                TopP = _topP,
                FrequencyPenalty = _frequencyPenalty,
                PresencePenalty = _presencePenalty,
                EnableStreaming = _enableStreaming
            };
        }
        
        #endregion
        
        #region Private Methods
        
        private string GetOpenAIModelName()
        {
            return _openAIModel switch
            {
                OpenAIModel.GPT4o => "gpt-4o",
                OpenAIModel.GPT4oMini => "gpt-4o-mini",
                OpenAIModel.GPT4Turbo => "gpt-4-turbo",
                OpenAIModel.GPT35Turbo => "gpt-3.5-turbo",
                _ => "gpt-4o-mini"
            };
        }
        
        #endregion
    }
    
    #endregion
    
    #region OpenAI Model Enum
    
    /// <summary>
    /// Available OpenAI models.
    /// </summary>
    public enum OpenAIModel
    {
        GPT4o,
        GPT4oMini,
        GPT4Turbo,
        GPT35Turbo
    }
    
    #endregion
    
    #region Content Filter Level Enum
    
    /// <summary>
    /// Content filtering strictness levels.
    /// </summary>
    public enum ContentFilterLevel
    {
        None,
        Low,
        Moderate,
        Strict
    }
    
    #endregion
    
    #region Runtime Settings
    
    /// <summary>
    /// Runtime-modifiable LLM settings.
    /// </summary>
    [Serializable]
    public class LLMRuntimeSettings
    {
        public float Temperature;
        public int MaxTokens;
        public float TopP;
        public float FrequencyPenalty;
        public float PresencePenalty;
        public bool EnableStreaming;
    }
    
    #endregion
    
    #region LLM Request/Response Classes
    
    /// <summary>
    /// Request structure for LLM generation.
    /// </summary>
    [Serializable]
    public class LLMRequest
    {
        public string SystemPrompt;
        public List<LLMMessage> Messages;
        public LLMRuntimeSettings Settings;
        
        public LLMRequest()
        {
            Messages = new List<LLMMessage>();
        }
    }
    
    /// <summary>
    /// Message in a conversation.
    /// </summary>
    [Serializable]
    public class LLMMessage
    {
        public string Role; // "system", "user", "assistant"
        public string Content;
        
        public LLMMessage() { }
        
        public LLMMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }
        
        public static LLMMessage System(string content) => new LLMMessage("system", content);
        public static LLMMessage User(string content) => new LLMMessage("user", content);
        public static LLMMessage Assistant(string content) => new LLMMessage("assistant", content);
    }
    
    /// <summary>
    /// Response from LLM generation.
    /// </summary>
    [Serializable]
    public class LLMResponse
    {
        public bool Success;
        public string Content;
        public string ErrorMessage;
        public int TokensUsed;
        public float GenerationTimeSeconds;
        public string FinishReason;
    }
    
    #endregion
}
