using System;
using UnityEngine;

namespace InteractiveNPC.Speech.Data
{
    #region TTS Settings ScriptableObject
    
    /// <summary>
    /// ScriptableObject for configuring Text-to-Speech settings.
    /// Stores ElevenLabs API configuration and voice parameters.
    /// </summary>
    [CreateAssetMenu(fileName = "TTSSettings", menuName = "Interactive NPC/Speech/TTS Settings")]
    public class TTSSettings : ScriptableObject
    {
        #region Serialized Fields
        
        [Header("ElevenLabs API")]
        [SerializeField, Tooltip("ElevenLabs API Key")]
        private string _apiKey = "";
        
        [SerializeField, Tooltip("ElevenLabs API base URL")]
        private string _baseUrl = "https://api.elevenlabs.io/v1";
        
        [Header("Voice Configuration")]
        [SerializeField, Tooltip("ElevenLabs Voice ID")]
        private string _voiceId = "";
        
        [SerializeField, Tooltip("Voice name for reference")]
        private string _voiceName = "";
        
        [SerializeField, Tooltip("Model ID to use")]
        private ElevenLabsModel _model = ElevenLabsModel.ElevenMultilingualV2;
        
        [Header("Voice Settings")]
        [SerializeField, Tooltip("Voice stability (0-1): Lower = more expressive, Higher = more consistent")]
        [Range(0f, 1f)]
        private float _stability = 0.5f;
        
        [SerializeField, Tooltip("Similarity boost (0-1): How closely to match the original voice")]
        [Range(0f, 1f)]
        private float _similarityBoost = 0.75f;
        
        [SerializeField, Tooltip("Style exaggeration (0-1): Only for v2 models")]
        [Range(0f, 1f)]
        private float _style = 0f;
        
        [SerializeField, Tooltip("Use speaker boost for clarity")]
        private bool _useSpeakerBoost = true;
        
        [Header("Output Settings")]
        [SerializeField, Tooltip("Output audio format")]
        private AudioFormat _outputFormat = AudioFormat.MP3_44100_128;
        
        [SerializeField, Tooltip("Request timeout in seconds")]
        [Range(5f, 120f)]
        private float _timeoutSeconds = 30f;
        
        [Header("Fallback Settings")]
        [SerializeField, Tooltip("Enable fallback to Unity TTS if ElevenLabs fails")]
        private bool _enableFallback = true;
        
        [SerializeField, Tooltip("Cache generated audio for repeated phrases")]
        private bool _enableCaching = true;
        
        [SerializeField, Tooltip("Maximum cache size in MB")]
        [Range(10, 500)]
        private int _maxCacheSizeMB = 50;
        
        #endregion
        
        #region Properties
        
        public string ApiKey => _apiKey;
        public string BaseUrl => _baseUrl;
        public string VoiceId => _voiceId;
        public string VoiceName => _voiceName;
        public ElevenLabsModel Model => _model;
        public float Stability => _stability;
        public float SimilarityBoost => _similarityBoost;
        public float Style => _style;
        public bool UseSpeakerBoost => _useSpeakerBoost;
        public AudioFormat OutputFormat => _outputFormat;
        public float TimeoutSeconds => _timeoutSeconds;
        public bool EnableFallback => _enableFallback;
        public bool EnableCaching => _enableCaching;
        public int MaxCacheSizeMB => _maxCacheSizeMB;
        
        public bool IsConfigured => !string.IsNullOrEmpty(_apiKey) && !string.IsNullOrEmpty(_voiceId);
        
        public string ModelId => _model switch
        {
            ElevenLabsModel.ElevenMultilingualV2 => "eleven_multilingual_v2",
            ElevenLabsModel.ElevenTurboV2 => "eleven_turbo_v2",
            ElevenLabsModel.ElevenMonolingualV1 => "eleven_monolingual_v1",
            _ => "eleven_multilingual_v2"
        };
        
        public string OutputFormatString => _outputFormat switch
        {
            AudioFormat.MP3_44100_128 => "mp3_44100_128",
            AudioFormat.MP3_44100_64 => "mp3_44100_64",
            AudioFormat.PCM_16000 => "pcm_16000",
            AudioFormat.PCM_22050 => "pcm_22050",
            AudioFormat.PCM_24000 => "pcm_24000",
            AudioFormat.PCM_44100 => "pcm_44100",
            _ => "mp3_44100_128"
        };
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Get the API endpoint URL for text-to-speech.
        /// </summary>
        public string GetTTSEndpoint()
        {
            return $"{_baseUrl}/text-to-speech/{_voiceId}";
        }
        
        /// <summary>
        /// Get the API endpoint URL with streaming.
        /// </summary>
        public string GetStreamingTTSEndpoint()
        {
            return $"{_baseUrl}/text-to-speech/{_voiceId}/stream";
        }
        
        #endregion
    }
    
    #endregion
    
    #region Enums
    
    /// <summary>
    /// ElevenLabs model options.
    /// </summary>
    public enum ElevenLabsModel
    {
        ElevenMultilingualV2,
        ElevenTurboV2,
        ElevenMonolingualV1
    }
    
    /// <summary>
    /// Audio output format options.
    /// </summary>
    public enum AudioFormat
    {
        MP3_44100_128,
        MP3_44100_64,
        PCM_16000,
        PCM_22050,
        PCM_24000,
        PCM_44100
    }
    
    #endregion
    
    #region STT Settings ScriptableObject
    
    /// <summary>
    /// ScriptableObject for configuring Speech-to-Text settings.
    /// </summary>
    [CreateAssetMenu(fileName = "STTSettings", menuName = "Interactive NPC/Speech/STT Settings")]
    public class STTSettings : ScriptableObject
    {
        #region Serialized Fields
        
        [Header("Recognition Settings")]
        [SerializeField, Tooltip("Language code for speech recognition")]
        private string _languageCode = "en-US";
        
        [SerializeField, Tooltip("Maximum recording duration in seconds")]
        [Range(1f, 60f)]
        private float _maxRecordingDuration = 15f;
        
        [SerializeField, Tooltip("Silence threshold to stop recording")]
        [Range(0f, 1f)]
        private float _silenceThreshold = 0.02f;
        
        [SerializeField, Tooltip("Seconds of silence before stopping")]
        [Range(0.5f, 5f)]
        private float _silenceDuration = 1.5f;
        
        [Header("Audio Settings")]
        [SerializeField, Tooltip("Sample rate for recording")]
        private int _sampleRate = 16000;
        
        [SerializeField, Tooltip("Audio source to use for recording")]
        private AudioRecordingSource _recordingSource = AudioRecordingSource.Default;
        
        [Header("UI Settings")]
        [SerializeField, Tooltip("Show visual feedback during recording")]
        private bool _showVisualFeedback = true;
        
        [SerializeField, Tooltip("Play audio cue when recording starts")]
        private bool _playStartCue = true;
        
        [SerializeField, Tooltip("Play audio cue when recording ends")]
        private bool _playEndCue = true;
        
        #endregion
        
        #region Properties
        
        public string LanguageCode => _languageCode;
        public float MaxRecordingDuration => _maxRecordingDuration;
        public float SilenceThreshold => _silenceThreshold;
        public float SilenceDuration => _silenceDuration;
        public int SampleRate => _sampleRate;
        public AudioRecordingSource RecordingSource => _recordingSource;
        public bool ShowVisualFeedback => _showVisualFeedback;
        public bool PlayStartCue => _playStartCue;
        public bool PlayEndCue => _playEndCue;
        
        #endregion
    }
    
    /// <summary>
    /// Audio recording source options.
    /// </summary>
    public enum AudioRecordingSource
    {
        Default,
        Specified
    }
    
    #endregion
}
