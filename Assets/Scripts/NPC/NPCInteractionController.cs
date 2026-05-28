using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using InteractiveNPC.Core.Events;
using InteractiveNPC.Core.Utilities;
using InteractiveNPC.Memory;
using InteractiveNPC.Memory.Data;
using InteractiveNPC.Personality;
using InteractiveNPC.Personality.Data;
using InteractiveNPC.LLM;
using InteractiveNPC.LLM.Data;
using InteractiveNPC.LLM.Interfaces;
using InteractiveNPC.LLM.Providers;
using InteractiveNPC.Speech.Interfaces;
using InteractiveNPC.Speech.TTS;
using InteractiveNPC.Speech.STT;
using InteractiveNPC.Speech.Data;
using InteractiveNPC.LipSync;
using InteractiveNPC.NPC.Data;

namespace InteractiveNPC.NPC
{
    #region Conversation State Enum
    
    /// <summary>
    /// States of the NPC conversation state machine.
    /// </summary>
    public enum ConversationState
    {
        Idle,
        Greeting,
        Listening,
        Processing,
        Speaking,
        Reacting,
        Farewell
    }
    
    #endregion
    
    #region NPC Interaction Controller
    
    /// <summary>
    /// Main NPC component that orchestrates all systems for interactive dialogue.
    /// Handles conversation flow, LLM integration, speech, and animations.
    /// </summary>
    public class NPCInteractionController : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Configuration")]
        [SerializeField, Tooltip("NPC configuration asset")]
        private NPCConfiguration _configuration;
        
        [SerializeField, Tooltip("LLM settings asset")]
        private LLMSettings _llmSettings;
        
        [SerializeField, Tooltip("STT settings asset")]
        private STTSettings _sttSettings;
        
        [Header("Components")]
        [SerializeField, Tooltip("Audio source for NPC voice")]
        private AudioSource _audioSource;
        
        [SerializeField, Tooltip("Lip sync component")]
        private VisemeLipSync _lipSync;
        
        [SerializeField, Tooltip("Animator for NPC reactions")]
        private Animator _animator;
        
        [Header("Animation Settings")]
        [SerializeField, Tooltip("Transform to animate during speech")]
        private Transform _headTransform;
        
        [SerializeField, Tooltip("Amount of head movement during speech")]
        [Range(0f, 10f)]
        private float _speechHeadMovement = 2f;
        
        [Header("Events")]
        [SerializeField]
        private ConversationUnityEvent _onConversationStarted;
        
        [SerializeField]
        private ConversationUnityEvent _onConversationEnded;
        
        [SerializeField]
        private StringUnityEvent _onNPCResponse;
        
        [SerializeField]
        private UnityEvent _onProcessingStarted;
        
        [SerializeField]
        private UnityEvent _onProcessingCompleted;
        
        #endregion
        
        #region Private Fields
        
        // State
        private ConversationState _currentState = ConversationState.Idle;
        private bool _isInConversation;
        private CancellationTokenSource _conversationCts;
        
        // Core Systems
        private MemoryBank _memoryBank;
        private RelationshipTracker _relationshipTracker;
        private MoodController _moodController;
        private PromptBuilder _promptBuilder;
        private ResponseModifierService _responseModifier;
        
        // LLM
        private ILLMProvider _llmProvider;
        
        // Speech
        private ITextToSpeech _tts;
        private ISpeechToText _stt;
        
        // Persistence
        private MemoryPersistence _memoryPersistence;
        
        // Animation
        private Tween _currentSpeechTween;
        private Vector3 _originalHeadRotation;
        
        #endregion
        
        #region Properties
        
        public ConversationState CurrentState => _currentState;
        public bool IsInConversation => _isInConversation;
        public NPCConfiguration Configuration => _configuration;
        public MemoryBank MemoryBank => _memoryBank;
        public RelationshipTracker RelationshipTracker => _relationshipTracker;
        public MoodController MoodController => _moodController;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            DebugLogger.LogAwake(this, LogCategory.NPC);
            
            ValidateConfiguration();
            InitializeSystems();
        }
        
        private void Start()
        {
            DebugLogger.LogStart(this, LogCategory.NPC);
            
            LoadPersistedData();
            SubscribeToEvents();
            
            if (_headTransform != null)
            {
                _originalHeadRotation = _headTransform.localEulerAngles;
            }
        }
        
        private void Update()
        {
            // Update mood decay
            _moodController?.Update(Time.deltaTime);
        }
        
        private void OnDestroy()
        {
            DebugLogger.LogOnDestroy(this, LogCategory.NPC);
            
            UnsubscribeFromEvents();
            SavePersistedData();
            
            _conversationCts?.Cancel();
            _conversationCts?.Dispose();
            _currentSpeechTween?.Kill();
            
            _llmProvider?.Shutdown();
            _tts?.Shutdown();
            _stt?.Shutdown();
        }
        
        #endregion
        
        #region Public Methods - Conversation Control
        
        /// <summary>
        /// Start a conversation with the NPC.
        /// </summary>
        public async UniTask StartConversationAsync()
        {
            if (_isInConversation)
            {
                DebugLogger.LogWarning("Already in conversation", LogCategory.NPC);
                return;
            }
            
            _isInConversation = true;
            _conversationCts = new CancellationTokenSource();
            
            SetState(ConversationState.Greeting);
            
            // Get greeting
            bool isReturning = _memoryBank?.TotalMemoryCount > 0;
            float relationshipScore = _relationshipTracker?.OverallScore ?? 0f;
            string greeting = _configuration.GetGreeting(isReturning, relationshipScore);
            
            // Modify greeting based on mood
            if (_responseModifier != null)
            {
                greeting = _responseModifier.ModifyResponse(greeting, _moodController, _relationshipTracker);
            }
            
            var eventArgs = new ConversationEventArgs
            {
                NPCId = _configuration.NPCId,
                NPCResponse = greeting
            };
            
            _onConversationStarted?.Invoke(eventArgs);
            GameEvents.RaiseConversationStarted(eventArgs);
            
            // Speak greeting
            await SpeakAsync(greeting);
            
            SetState(ConversationState.Idle);
            
            DebugLogger.LogInfo($"Conversation started with {_configuration.DisplayName}", LogCategory.NPC);
        }
        
        /// <summary>
        /// End the current conversation.
        /// </summary>
        public async UniTask EndConversationAsync()
        {
            if (!_isInConversation) return;
            
            SetState(ConversationState.Farewell);
            
            string farewell = _configuration.GetFarewell();
            
            if (_responseModifier != null)
            {
                farewell = _responseModifier.ModifyResponse(farewell, _moodController, _relationshipTracker);
            }
            
            await SpeakAsync(farewell);
            
            _isInConversation = false;
            _conversationCts?.Cancel();
            
            var eventArgs = new ConversationEventArgs
            {
                NPCId = _configuration.NPCId,
                NPCResponse = farewell
            };
            
            _onConversationEnded?.Invoke(eventArgs);
            GameEvents.RaiseConversationEnded(eventArgs);
            
            SetState(ConversationState.Idle);
            
            // Save memories
            SavePersistedData();
            
            // Consolidate memories
            _memoryBank?.ConsolidateMemories();
            
            DebugLogger.LogInfo($"Conversation ended with {_configuration.DisplayName}", LogCategory.NPC);
        }
        
        /// <summary>
        /// Send a text message to the NPC and get a response.
        /// </summary>
        public async UniTask<string> SendMessageAsync(string playerMessage)
        {
            if (!_isInConversation)
            {
                await StartConversationAsync();
            }
            
            if (string.IsNullOrWhiteSpace(playerMessage))
            {
                return "";
            }
            
            DebugLogger.LogInfo($"Player message: {playerMessage}", LogCategory.NPC);
            
            SetState(ConversationState.Processing);
            _onProcessingStarted?.Invoke();
            GameEvents.RaiseNPCProcessingStarted(_configuration.NPCId);
            
            try
            {
                // Build prompt
                LLMRequest request = _promptBuilder.BuildRequest(playerMessage, _llmSettings?.CreateRuntimeSettings());
                
                // Generate response
                string response = await GenerateLLMResponseAsync(request);
                
                if (string.IsNullOrEmpty(response))
                {
                    response = "I... I'm not sure what to say.";
                }
                
                // Modify response based on mood/relationship
                if (_responseModifier != null)
                {
                    response = _responseModifier.ModifyResponse(response, _moodController, _relationshipTracker);
                }
                
                // Add to conversation history
                _promptBuilder.AddAssistantResponse(response);
                
                // Create memory from exchange
                _promptBuilder.CreateMemoryFromExchange(playerMessage, response);
                
                _onProcessingCompleted?.Invoke();
                GameEvents.RaiseNPCProcessingCompleted(_configuration.NPCId);
                
                // Speak response
                await SpeakAsync(response);
                
                // Raise events
                var eventArgs = new ConversationEventArgs
                {
                    NPCId = _configuration.NPCId,
                    PlayerInput = playerMessage,
                    NPCResponse = response
                };
                
                _onNPCResponse?.Invoke(response);
                GameEvents.RaiseNPCResponseReceived(eventArgs);
                
                SetState(ConversationState.Idle);
                
                return response;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, LogCategory.NPC);
                SetState(ConversationState.Idle);
                return "I seem to be having trouble thinking right now...";
            }
        }
        
        /// <summary>
        /// Start listening for voice input.
        /// </summary>
        public async UniTask<string> ListenForVoiceInputAsync()
        {
            if (_stt == null || !_stt.IsAvailable)
            {
                DebugLogger.LogWarning("Speech-to-text not available", LogCategory.NPC);
                return "";
            }
            
            SetState(ConversationState.Listening);
            
            try
            {
                await _stt.StartListeningAsync(_conversationCts.Token);
                var result = await _stt.StopListeningAsync();
                
                if (result.Success && !string.IsNullOrEmpty(result.Text))
                {
                    return await SendMessageAsync(result.Text);
                }
                
                return "";
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, LogCategory.NPC);
                return "";
            }
            finally
            {
                if (_currentState == ConversationState.Listening)
                {
                    SetState(ConversationState.Idle);
                }
            }
        }
        
        /// <summary>
        /// Stop any current speech.
        /// </summary>
        public void StopSpeaking()
        {
            if (_configuration.CanBeInterrupted && _currentState == ConversationState.Speaking)
            {
                _tts?.Stop();
                _currentSpeechTween?.Kill();
                SetState(ConversationState.Idle);
            }
        }
        
        #endregion
        
        #region Private Methods - Initialization
        
        private void ValidateConfiguration()
        {
            if (_configuration == null)
            {
                DebugLogger.LogError("NPCConfiguration is not assigned!", LogCategory.NPC, this);
                enabled = false;
                return;
            }
            
            if (_audioSource == null)
            {
                _audioSource = GetComponentInChildren<AudioSource>();
                if (_audioSource == null)
                {
                    _audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
        }
        
        private void InitializeSystems()
        {
            // Memory System
            _memoryBank = new MemoryBank(
                _configuration.NPCId,
                _configuration.MemoryData
            );
            
            _relationshipTracker = new RelationshipTracker(_configuration.NPCId);
            
            // Personality System
            _moodController = new MoodController(
                _configuration.NPCId,
                _configuration.PersonalityData
            );
            
            _responseModifier = new ResponseModifierService(_configuration.PersonalityData);
            
            // Prompt Builder
            _promptBuilder = new PromptBuilder(
                _configuration.PersonalityData,
                _memoryBank,
                _relationshipTracker,
                _moodController
            );
            
            _promptBuilder.SetWorldContext(_configuration.GetFullWorldContext());
            
            // LLM Provider
            if (_llmSettings != null)
            {
                _llmProvider = _llmSettings.ProviderType switch
                {
                    LLMProviderType.OpenAI => new OpenAIProvider(_llmSettings),
                    LLMProviderType.Ollama => new OllamaProvider(_llmSettings),
                    _ => new OpenAIProvider(_llmSettings)
                };
                
                _llmProvider.Initialize();
            }
            
            // TTS
            if (_configuration.VoiceSettings != null)
            {
                _tts = new ElevenLabsTTS(_configuration.VoiceSettings, _audioSource);
                _tts.Initialize();
                
                // Connect TTS to lip sync
                if (_lipSync != null)
                {
                    _tts.OnVisemeReceived += _lipSync.OnVisemeReceived;
                }
            }
            
            // STT
            if (_sttSettings != null)
            {
                _stt = new MicrophoneSpeechRecognizer(_sttSettings);
                _stt.Initialize();
            }
            
            // Persistence
            _memoryPersistence = new MemoryPersistence();
            
            DebugLogger.LogInfo($"NPC systems initialized for: {_configuration.DisplayName}", LogCategory.NPC);
        }
        
        private void LoadPersistedData()
        {
            var saveData = _memoryPersistence.LoadNPCMemory(_configuration.NPCId);
            
            if (saveData != null)
            {
                if (saveData.Memories != null)
                {
                    _memoryBank.ImportMemories(saveData.Memories);
                }
                
                if (saveData.Relationship != null)
                {
                    _relationshipTracker = new RelationshipTracker(_configuration.NPCId, saveData.Relationship);
                }
                
                DebugLogger.LogInfo($"Loaded persisted data for {_configuration.NPCId}", LogCategory.NPC);
            }
        }
        
        private void SavePersistedData()
        {
            _memoryPersistence.SaveNPCMemory(
                _configuration.NPCId,
                _memoryBank,
                _relationshipTracker
            );
        }
        
        private void SubscribeToEvents()
        {
            GameEvents.OnMoodChanged += HandleMoodChanged;
            GameEvents.OnRelationshipChanged += HandleRelationshipChanged;
        }
        
        private void UnsubscribeFromEvents()
        {
            GameEvents.OnMoodChanged -= HandleMoodChanged;
            GameEvents.OnRelationshipChanged -= HandleRelationshipChanged;
        }
        
        #endregion
        
        #region Private Methods - Core Functionality
        
        private void SetState(ConversationState newState)
        {
            if (_currentState == newState) return;
            
            DebugLogger.LogVerbose($"State change: {_currentState} -> {newState}", LogCategory.NPC);
            _currentState = newState;
            
            // Trigger animator state if available
            if (_animator != null)
            {
                _animator.SetInteger("ConversationState", (int)newState);
            }
        }
        
        private async UniTask<string> GenerateLLMResponseAsync(LLMRequest request)
        {
            if (_llmProvider == null || !_llmProvider.IsAvailable)
            {
                DebugLogger.LogWarning("LLM provider not available", LogCategory.NPC);
                return "";
            }
            
            // Add response delay for natural feel
            if (_configuration.ResponseDelay > 0)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_configuration.ResponseDelay), cancellationToken: _conversationCts.Token);
            }
            
            var response = await _llmProvider.GenerateAsync(request, _conversationCts.Token);
            
            if (response.Success)
            {
                return response.Content;
            }
            
            DebugLogger.LogError($"LLM generation failed: {response.ErrorMessage}", LogCategory.NPC);
            return "";
        }
        
        private async UniTask SpeakAsync(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            
            SetState(ConversationState.Speaking);
            
            // Start speech animation
            StartSpeechAnimation();
            
            if (_tts != null && _tts.IsAvailable)
            {
                var result = await _tts.SpeakAsync(text, _conversationCts.Token);
                
                if (!result.Success)
                {
                    DebugLogger.LogWarning($"TTS failed: {result.ErrorMessage}", LogCategory.NPC);
                }
            }
            else
            {
                // Simulate speaking time if no TTS
                float speakingTime = text.Length * 0.05f;
                await UniTask.Delay(TimeSpan.FromSeconds(speakingTime), cancellationToken: _conversationCts.Token);
            }
            
            // Stop speech animation
            StopSpeechAnimation();
        }
        
        private void StartSpeechAnimation()
        {
            if (_headTransform == null) return;
            
            _currentSpeechTween?.Kill();
            
            // Subtle head nodding during speech
            _currentSpeechTween = _headTransform
                .DOLocalRotate(
                    _originalHeadRotation + new Vector3(
                        UnityEngine.Random.Range(-_speechHeadMovement, _speechHeadMovement),
                        UnityEngine.Random.Range(-_speechHeadMovement * 0.5f, _speechHeadMovement * 0.5f),
                        0
                    ),
                    0.3f
                )
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }
        
        private void StopSpeechAnimation()
        {
            _currentSpeechTween?.Kill();
            
            if (_headTransform != null)
            {
                _headTransform.DOLocalRotate(_originalHeadRotation, 0.3f)
                    .SetEase(Ease.OutSine);
            }
            
            _lipSync?.ResetBlendShapes();
        }
        
        #endregion
        
        #region Event Handlers
        
        private void HandleMoodChanged(MoodEventArgs args)
        {
            if (args.NPCId != _configuration.NPCId) return;
            
            // Trigger mood-based animation
            if (_animator != null)
            {
                _animator.SetInteger("Mood", (int)args.NewMood);
                _animator.SetFloat("MoodIntensity", args.Intensity);
            }
        }
        
        private void HandleRelationshipChanged(RelationshipEventArgs args)
        {
            if (args.NPCId != _configuration.NPCId) return;
            
            DebugLogger.LogInfo($"Relationship {args.Metric} changed: {args.PreviousValue:F2} -> {args.NewValue:F2}", LogCategory.NPC);
        }
        
        #endregion
    }
    
    #endregion
}
