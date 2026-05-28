using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using DG.Tweening;
using InteractiveNPC.Core.Events;
using InteractiveNPC.Core.Utilities;
using InteractiveNPC.NPC;

namespace InteractiveNPC.UI
{
    #region Conversation UI Controller
    
    /// <summary>
    /// Main UI controller for NPC conversation interface.
    /// Handles text input, dialogue display, and visual feedback.
    /// </summary>
    public class ConversationUI : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("UI References")]
        [SerializeField, Tooltip("Main conversation panel")]
        private RectTransform _conversationPanel;
        
        [SerializeField, Tooltip("NPC name display")]
        private TextMeshProUGUI _npcNameText;
        
        [SerializeField, Tooltip("NPC portrait image")]
        private Image _npcPortrait;
        
        [SerializeField, Tooltip("Dialogue text display")]
        private TextMeshProUGUI _dialogueText;
        
        [SerializeField, Tooltip("Player input field")]
        private TMP_InputField _inputField;
        
        [SerializeField, Tooltip("Send button")]
        private Button _sendButton;
        
        [SerializeField, Tooltip("Microphone button")]
        private Button _microphoneButton;
        
        [SerializeField, Tooltip("Close conversation button")]
        private Button _closeButton;
        
        [SerializeField, Tooltip("Processing indicator")]
        private GameObject _processingIndicator;
        
        [SerializeField, Tooltip("Microphone recording indicator")]
        private GameObject _recordingIndicator;
        
        [Header("Dialogue History")]
        [SerializeField, Tooltip("Scroll view for dialogue history")]
        private ScrollRect _dialogueScrollRect;
        
        [SerializeField, Tooltip("Container for dialogue bubbles")]
        private RectTransform _dialogueContainer;
        
        [SerializeField, Tooltip("Player message bubble prefab")]
        private GameObject _playerBubblePrefab;
        
        [SerializeField, Tooltip("NPC message bubble prefab")]
        private GameObject _npcBubblePrefab;
        
        [Header("Mood Indicator")]
        [SerializeField, Tooltip("Mood indicator image")]
        private Image _moodIndicator;
        
        [SerializeField, Tooltip("Mood colors")]
        private MoodColorConfig _moodColors;
        
        [Header("Animation Settings")]
        [SerializeField, Tooltip("Panel show/hide duration")]
        private float _panelAnimationDuration = 0.3f;
        
        [SerializeField, Tooltip("Typewriter effect speed (chars per second)")]
        private float _typewriterSpeed = 50f;
        
        [SerializeField, Tooltip("Enable typewriter effect")]
        private bool _enableTypewriter = true;
        
        [Header("Events")]
        [SerializeField]
        private UnityEvent<string> _onMessageSent;
        
        [SerializeField]
        private UnityEvent _onMicrophonePressed;
        
        [SerializeField]
        private UnityEvent _onConversationClosed;
        
        #endregion
        
        #region Private Fields
        
        private NPCInteractionController _currentNPC;
        private List<GameObject> _dialogueBubbles = new List<GameObject>();
        private Tween _currentTypewriterTween;
        private bool _isShowing;
        
        #endregion
        
        #region Properties
        
        public bool IsShowing => _isShowing;
        public NPCInteractionController CurrentNPC => _currentNPC;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            DebugLogger.LogAwake(this, LogCategory.UI);
            
            // Setup button listeners
            _sendButton?.onClick.AddListener(OnSendButtonClicked);
            _microphoneButton?.onClick.AddListener(OnMicrophoneButtonClicked);
            _closeButton?.onClick.AddListener(OnCloseButtonClicked);
            
            // Setup input field
            if (_inputField != null)
            {
                _inputField.onSubmit.AddListener(OnInputSubmitted);
            }
            
            // Hide by default
            if (_conversationPanel != null)
            {
                _conversationPanel.gameObject.SetActive(false);
            }
        }
        
        private void Start()
        {
            DebugLogger.LogStart(this, LogCategory.UI);
            
            // Subscribe to events
            GameEvents.OnNPCProcessingStarted += HandleProcessingStarted;
            GameEvents.OnNPCProcessingCompleted += HandleProcessingCompleted;
            GameEvents.OnMoodChanged += HandleMoodChanged;
            GameEvents.OnSpeechRecognitionStarted += HandleRecordingStarted;
            GameEvents.OnSpeechRecognitionEnded += HandleRecordingEnded;
        }
        
        private void OnDestroy()
        {
            DebugLogger.LogOnDestroy(this, LogCategory.UI);
            
            GameEvents.OnNPCProcessingStarted -= HandleProcessingStarted;
            GameEvents.OnNPCProcessingCompleted -= HandleProcessingCompleted;
            GameEvents.OnMoodChanged -= HandleMoodChanged;
            GameEvents.OnSpeechRecognitionStarted -= HandleRecordingStarted;
            GameEvents.OnSpeechRecognitionEnded -= HandleRecordingEnded;
            
            _currentTypewriterTween?.Kill();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Show the conversation UI for the given NPC.
        /// </summary>
        public void Show(NPCInteractionController npc)
        {
            if (npc == null)
            {
                DebugLogger.LogWarning("Cannot show conversation UI: NPC is null", LogCategory.UI);
                return;
            }
            
            _currentNPC = npc;
            
            // Setup UI with NPC data
            if (npc.Configuration != null)
            {
                if (_npcNameText != null)
                {
                    _npcNameText.text = npc.Configuration.DisplayName;
                }
                
                if (_npcPortrait != null && npc.Configuration.Portrait != null)
                {
                    _npcPortrait.sprite = npc.Configuration.Portrait;
                    _npcPortrait.color = Color.white;
                }
                
                // Apply theme color
                ApplyThemeColor(npc.Configuration.ThemeColor);
            }
            
            // Clear previous messages
            ClearDialogueHistory();
            
            // Show panel with animation
            _conversationPanel.gameObject.SetActive(true);
            _isShowing = true;
            
            _conversationPanel.localScale = Vector3.zero;
            _conversationPanel.DOScale(Vector3.one, _panelAnimationDuration)
                .SetEase(Ease.OutBack);
            
            // Focus input field
            if (_inputField != null)
            {
                _inputField.text = "";
                _inputField.ActivateInputField();
            }
            
            // Update mood indicator
            UpdateMoodIndicator(npc.MoodController?.CurrentEmotion ?? EmotionalState.Neutral);
            
            DebugLogger.LogInfo($"Showing conversation UI for: {npc.Configuration?.DisplayName}", LogCategory.UI);
        }
        
        /// <summary>
        /// Hide the conversation UI.
        /// </summary>
        public void Hide()
        {
            if (!_isShowing) return;
            
            _isShowing = false;
            
            _conversationPanel.DOScale(Vector3.zero, _panelAnimationDuration)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    _conversationPanel.gameObject.SetActive(false);
                    _currentNPC = null;
                });
            
            _onConversationClosed?.Invoke();
            
            DebugLogger.LogInfo("Hiding conversation UI", LogCategory.UI);
        }
        
        /// <summary>
        /// Display NPC dialogue with optional typewriter effect.
        /// </summary>
        public void DisplayNPCDialogue(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            
            // Add to dialogue history
            AddDialogueBubble(text, false);
            
            // Display in main text area
            if (_dialogueText != null)
            {
                _currentTypewriterTween?.Kill();
                
                if (_enableTypewriter)
                {
                    _dialogueText.text = "";
                    _currentTypewriterTween = DOTween.To(
                        () => 0,
                        x => _dialogueText.text = text.Substring(0, Mathf.Min(x, text.Length)),
                        text.Length,
                        text.Length / _typewriterSpeed
                    ).SetEase(Ease.Linear);
                }
                else
                {
                    _dialogueText.text = text;
                }
            }
        }
        
        /// <summary>
        /// Display player message.
        /// </summary>
        public void DisplayPlayerMessage(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            
            AddDialogueBubble(text, true);
        }
        
        /// <summary>
        /// Clear dialogue history.
        /// </summary>
        public void ClearDialogueHistory()
        {
            foreach (var bubble in _dialogueBubbles)
            {
                if (bubble != null)
                {
                    Destroy(bubble);
                }
            }
            _dialogueBubbles.Clear();
        }
        
        #endregion
        
        #region Private Methods
        
        private void OnSendButtonClicked()
        {
            SendCurrentMessage();
        }
        
        private void OnMicrophoneButtonClicked()
        {
            _onMicrophonePressed?.Invoke();
            
            if (_currentNPC != null)
            {
                _ = _currentNPC.ListenForVoiceInputAsync();
            }
        }
        
        private void OnCloseButtonClicked()
        {
            if (_currentNPC != null)
            {
                _ = _currentNPC.EndConversationAsync();
            }
            Hide();
        }
        
        private void OnInputSubmitted(string text)
        {
            SendCurrentMessage();
        }
        
        private async void SendCurrentMessage()
        {
            if (_inputField == null || string.IsNullOrWhiteSpace(_inputField.text)) return;
            
            string message = _inputField.text.Trim();
            _inputField.text = "";
            _inputField.ActivateInputField();
            
            DisplayPlayerMessage(message);
            
            _onMessageSent?.Invoke(message);
            
            if (_currentNPC != null)
            {
                string response = await _currentNPC.SendMessageAsync(message);
                DisplayNPCDialogue(response);
            }
        }
        
        private void AddDialogueBubble(string text, bool isPlayer)
        {
            if (_dialogueContainer == null) return;
            
            GameObject prefab = isPlayer ? _playerBubblePrefab : _npcBubblePrefab;
            if (prefab == null) return;
            
            GameObject bubble = Instantiate(prefab, _dialogueContainer);
            _dialogueBubbles.Add(bubble);
            
            // Set text
            var textComponent = bubble.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = text;
            }
            
            // Animate in
            RectTransform bubbleRect = bubble.GetComponent<RectTransform>();
            if (bubbleRect != null)
            {
                bubbleRect.localScale = Vector3.zero;
                bubbleRect.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
            }
            
            // Scroll to bottom
            if (_dialogueScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                _dialogueScrollRect.verticalNormalizedPosition = 0f;
            }
        }
        
        private void ApplyThemeColor(Color color)
        {
            // Apply theme color to UI elements
            if (_npcNameText != null)
            {
                _npcNameText.color = color;
            }
            
            // Could apply to other elements as needed
        }
        
        private void UpdateMoodIndicator(EmotionalState mood)
        {
            if (_moodIndicator == null || _moodColors == null) return;
            
            Color targetColor = mood switch
            {
                EmotionalState.Happy => _moodColors.Happy,
                EmotionalState.Angry => _moodColors.Angry,
                EmotionalState.Sad => _moodColors.Sad,
                EmotionalState.Fearful => _moodColors.Fearful,
                EmotionalState.Surprised => _moodColors.Surprised,
                _ => _moodColors.Neutral
            };
            
            _moodIndicator.DOColor(targetColor, 0.3f);
        }
        
        #endregion
        
        #region Event Handlers
        
        private void HandleProcessingStarted(string npcId)
        {
            if (_currentNPC?.Configuration?.NPCId != npcId) return;
            
            if (_processingIndicator != null)
            {
                _processingIndicator.SetActive(true);
            }
            
            if (_sendButton != null)
            {
                _sendButton.interactable = false;
            }
        }
        
        private void HandleProcessingCompleted(string npcId)
        {
            if (_currentNPC?.Configuration?.NPCId != npcId) return;
            
            if (_processingIndicator != null)
            {
                _processingIndicator.SetActive(false);
            }
            
            if (_sendButton != null)
            {
                _sendButton.interactable = true;
            }
        }
        
        private void HandleMoodChanged(MoodEventArgs args)
        {
            if (_currentNPC?.Configuration?.NPCId != args.NPCId) return;
            
            UpdateMoodIndicator(args.NewMood);
        }
        
        private void HandleRecordingStarted()
        {
            if (_recordingIndicator != null)
            {
                _recordingIndicator.SetActive(true);
            }
        }
        
        private void HandleRecordingEnded()
        {
            if (_recordingIndicator != null)
            {
                _recordingIndicator.SetActive(false);
            }
        }
        
        #endregion
    }
    
    #endregion
    
    #region Mood Color Config
    
    /// <summary>
    /// Configuration for mood indicator colors.
    /// </summary>
    [Serializable]
    public class MoodColorConfig
    {
        public Color Neutral = Color.gray;
        public Color Happy = new Color(0.2f, 0.8f, 0.2f);
        public Color Angry = new Color(0.9f, 0.2f, 0.2f);
        public Color Sad = new Color(0.3f, 0.3f, 0.8f);
        public Color Fearful = new Color(0.6f, 0.3f, 0.8f);
        public Color Surprised = new Color(0.9f, 0.8f, 0.2f);
    }
    
    #endregion
}
