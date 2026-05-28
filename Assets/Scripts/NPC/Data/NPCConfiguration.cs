using System;
using System.Collections.Generic;
using UnityEngine;

namespace InteractiveNPC.NPC.Data
{
    #region NPC Configuration ScriptableObject
    
    /// <summary>
    /// Complete configuration for an NPC combining personality, memory, voice, and visual settings.
    /// This is the main asset that defines an NPC's characteristics.
    /// </summary>
    [CreateAssetMenu(fileName = "NPCConfiguration", menuName = "Interactive NPC/NPC/NPC Configuration")]
    public class NPCConfiguration : ScriptableObject
    {
        #region Serialized Fields
        
        [Header("Identity")]
        [SerializeField, Tooltip("Unique identifier for this NPC")]
        private string _npcId = "";
        
        [SerializeField, Tooltip("Display name shown to the player")]
        private string _displayName = "NPC";
        
        [SerializeField, Tooltip("Optional title or role displayed below name")]
        private string _title = "";
        
        [Header("Personality Reference")]
        [SerializeField, Tooltip("Reference to personality data asset")]
        private Personality.Data.NPCPersonalityData _personalityData;
        
        [Header("Memory Reference")]
        [SerializeField, Tooltip("Reference to memory data asset")]
        private Memory.Data.NPCMemoryData _memoryData;
        
        [Header("Voice Settings")]
        [SerializeField, Tooltip("Reference to TTS settings for this NPC")]
        private Speech.Data.TTSSettings _voiceSettings;
        
        [Header("Visual Settings")]
        [SerializeField, Tooltip("Portrait image for dialogue UI")]
        private Sprite _portrait;
        
        [SerializeField, Tooltip("Color theme for this NPC's dialogue")]
        private Color _themeColor = new Color(0.3f, 0.5f, 0.7f);
        
        [Header("Interaction Settings")]
        [SerializeField, Tooltip("Distance at which player can interact")]
        [Range(1f, 10f)]
        private float _interactionDistance = 3f;
        
        [SerializeField, Tooltip("Whether NPC can be interrupted while speaking")]
        private bool _canBeInterrupted = true;
        
        [SerializeField, Tooltip("Delay before NPC starts responding")]
        [Range(0f, 2f)]
        private float _responseDelay = 0.3f;
        
        [Header("World Context")]
        [SerializeField, Tooltip("Information about the NPC's location and situation")]
        [TextArea(2, 5)]
        private string _worldContext = "";
        
        [SerializeField, Tooltip("Current time of day context")]
        private TimeOfDayBehavior _timeOfDayBehavior = TimeOfDayBehavior.AlwaysAvailable;
        
        [SerializeField, Tooltip("Available time range (if time-restricted)")]
        private Vector2Int _availableHours = new Vector2Int(8, 20);
        
        [Header("Greeting Settings")]
        [SerializeField, Tooltip("Initial greeting when conversation starts")]
        [TextArea(2, 4)]
        private string _defaultGreeting = "Hello there! What can I do for you today?";
        
        [SerializeField, Tooltip("Alternative greetings (randomly selected)")]
        private List<string> _alternativeGreetings = new List<string>();
        
        [SerializeField, Tooltip("Greeting when player returns after absence")]
        [TextArea(2, 4)]
        private string _returnGreeting = "Ah, you're back! Good to see you again.";
        
        [Header("Farewell Settings")]
        [SerializeField, Tooltip("Default farewell message")]
        private string _defaultFarewell = "Farewell! Safe travels.";
        
        [SerializeField, Tooltip("Alternative farewells")]
        private List<string> _alternativeFarewells = new List<string>();
        
        #endregion
        
        #region Properties
        
        public string NPCId => string.IsNullOrEmpty(_npcId) ? name : _npcId;
        public string DisplayName => _displayName;
        public string Title => _title;
        public Personality.Data.NPCPersonalityData PersonalityData => _personalityData;
        public Memory.Data.NPCMemoryData MemoryData => _memoryData;
        public Speech.Data.TTSSettings VoiceSettings => _voiceSettings;
        public Sprite Portrait => _portrait;
        public Color ThemeColor => _themeColor;
        public float InteractionDistance => _interactionDistance;
        public bool CanBeInterrupted => _canBeInterrupted;
        public float ResponseDelay => _responseDelay;
        public string WorldContext => _worldContext;
        public TimeOfDayBehavior TimeOfDayBehavior => _timeOfDayBehavior;
        public Vector2Int AvailableHours => _availableHours;
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Get an appropriate greeting based on context.
        /// </summary>
        public string GetGreeting(bool isReturning = false, float relationshipScore = 0f)
        {
            if (isReturning && !string.IsNullOrEmpty(_returnGreeting))
            {
                return _returnGreeting;
            }
            
            if (_alternativeGreetings.Count > 0 && UnityEngine.Random.value > 0.5f)
            {
                return _alternativeGreetings[UnityEngine.Random.Range(0, _alternativeGreetings.Count)];
            }
            
            return _defaultGreeting;
        }
        
        /// <summary>
        /// Get an appropriate farewell.
        /// </summary>
        public string GetFarewell()
        {
            if (_alternativeFarewells.Count > 0 && UnityEngine.Random.value > 0.5f)
            {
                return _alternativeFarewells[UnityEngine.Random.Range(0, _alternativeFarewells.Count)];
            }
            
            return _defaultFarewell;
        }
        
        /// <summary>
        /// Check if NPC is available at the given hour.
        /// </summary>
        public bool IsAvailableAtHour(int hour)
        {
            if (_timeOfDayBehavior == TimeOfDayBehavior.AlwaysAvailable)
                return true;
            
            if (_timeOfDayBehavior == TimeOfDayBehavior.NeverAvailable)
                return false;
            
            return hour >= _availableHours.x && hour <= _availableHours.y;
        }
        
        /// <summary>
        /// Get the complete world context string.
        /// </summary>
        public string GetFullWorldContext()
        {
            if (string.IsNullOrEmpty(_worldContext))
            {
                return $"You are {_displayName}" + (string.IsNullOrEmpty(_title) ? "." : $", a {_title}.");
            }
            
            return _worldContext;
        }
        
        #endregion
        
        #region Validation
        
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_npcId))
            {
                _npcId = name;
            }
            
            if (string.IsNullOrEmpty(_displayName))
            {
                _displayName = name;
            }
        }
        
        #endregion
    }
    
    #endregion
    
    #region Enums
    
    /// <summary>
    /// Time of day availability behavior.
    /// </summary>
    public enum TimeOfDayBehavior
    {
        AlwaysAvailable,
        TimeRestricted,
        NeverAvailable
    }
    
    #endregion
}
