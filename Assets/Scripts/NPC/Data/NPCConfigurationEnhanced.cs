using System;
using System.Collections.Generic;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#else
using InteractiveNPC.Core.Attributes;
#endif

namespace InteractiveNPC.NPC.Data
{
    #region NPC Configuration Enhanced
    
    /// <summary>
    /// Enhanced NPC Configuration with full Odin Inspector support.
    /// Complete NPC setup with visual feedback and validation.
    /// </summary>
    [CreateAssetMenu(fileName = "NPCConfigurationEnhanced", menuName = "Interactive NPC/NPC/NPC Configuration (Enhanced)")]
    public class NPCConfigurationEnhanced : ScriptableObject
    {
        #region Serialized Fields
        
        [Title("Identity", "Core identification and display settings")]
        [HorizontalGroup("Identity", Width = 80)]
        [PreviewField(80, ObjectFieldAlignment.Left)]
        [HideLabel]
        [SerializeField]
        private Sprite _portrait;
        
        [VerticalGroup("Identity/Info")]
        [LabelText("NPC ID")]
        [InfoBox("Unique identifier used for saves and events")]
        [SerializeField]
        private string _npcId = "";
        
        [VerticalGroup("Identity/Info")]
        [Required("Display name is required!")]
        [LabelText("Display Name")]
        [SerializeField]
        private string _displayName = "NPC";
        
        [VerticalGroup("Identity/Info")]
        [LabelText("Title/Role")]
        [SerializeField]
        private string _title = "";
        
        [VerticalGroup("Identity/Info")]
        [LabelText("Theme Color")]
        [SerializeField]
        private Color _themeColor = new Color(0.3f, 0.5f, 0.7f);
        
        [Title("Data References", "Link personality, memory, and voice assets")]
        [BoxGroup("References")]
        [Required("Personality data is required!")]
        [InlineEditor(InlineEditorModes.GUIOnly)]
        [LabelText("Personality")]
        [SerializeField]
        private Personality.Data.NPCPersonalityData _personalityData;
        
        [BoxGroup("References")]
        [InlineEditor(InlineEditorModes.GUIOnly)]
        [LabelText("Memory")]
        [SerializeField]
        private Memory.Data.NPCMemoryData _memoryData;
        
        [BoxGroup("References")]
        [InlineEditor(InlineEditorModes.GUIOnly)]
        [LabelText("Voice (TTS)")]
        [SerializeField]
        private Speech.Data.TTSSettings _voiceSettings;
        
        [Title("Interaction", "Player interaction settings")]
        [TabGroup("Settings", "Interaction")]
        [BoxGroup("Settings/Interaction/Distance")]
        [LabelText("Interaction Distance")]
        [ProgressBar(1, 10, 0.3f, 0.7f, 0.9f)]
        [SerializeField]
        private float _interactionDistance = 3f;
        
        [BoxGroup("Settings/Interaction/Distance")]
        [LabelText("Can Be Interrupted")]
        [SerializeField]
        private bool _canBeInterrupted = true;
        
        [BoxGroup("Settings/Interaction/Distance")]
        [LabelText("Response Delay")]
        [ProgressBar(0, 2, 0.5f, 0.5f, 0.7f)]
        [SerializeField]
        private float _responseDelay = 0.3f;
        
        [Title("World Context", "Location and situation context")]
        [TabGroup("Settings", "Context")]
        [TextArea(3, 8)]
        [LabelText("World Context")]
        [InfoBox("Describe the NPC's location, situation, and immediate surroundings")]
        [SerializeField]
        private string _worldContext = "";
        
        [TabGroup("Settings", "Context")]
        [BoxGroup("Settings/Context/Time")]
        [LabelText("Availability")]
        [SerializeField]
        private TimeOfDayBehavior _timeOfDayBehavior = TimeOfDayBehavior.AlwaysAvailable;
        
        [BoxGroup("Settings/Context/Time")]
        [ShowIf("_timeOfDayBehavior", TimeOfDayBehavior.TimeRestricted)]
        [LabelText("Available Hours")]
        [MinMaxSlider(0, 24, true)]
        [SerializeField]
        private Vector2 _availableHours = new Vector2(8, 20);
        
        [Title("Dialogue", "Greetings and farewell messages")]
        [TabGroup("Settings", "Dialogue")]
        [FoldoutGroup("Settings/Dialogue/Greetings", true)]
        [TextArea(2, 4)]
        [LabelText("Default Greeting")]
        [SerializeField]
        private string _defaultGreeting = "Hello there! What can I do for you today?";
        
        [FoldoutGroup("Settings/Dialogue/Greetings")]
        [LabelText("Alternative Greetings")]
        [SerializeField]
        private List<string> _alternativeGreetings = new List<string>();
        
        [FoldoutGroup("Settings/Dialogue/Greetings")]
        [TextArea(2, 4)]
        [LabelText("Return Greeting")]
        [InfoBox("Shown when player returns after talking before")]
        [SerializeField]
        private string _returnGreeting = "Ah, you're back! Good to see you again.";
        
        [TabGroup("Settings", "Dialogue")]
        [FoldoutGroup("Settings/Dialogue/Farewells", false)]
        [LabelText("Default Farewell")]
        [SerializeField]
        private string _defaultFarewell = "Farewell! Safe travels.";
        
        [FoldoutGroup("Settings/Dialogue/Farewells")]
        [LabelText("Alternative Farewells")]
        [SerializeField]
        private List<string> _alternativeFarewells = new List<string>();
        
        #endregion
        
        #region Properties
        
        public string NPCId => string.IsNullOrEmpty(_npcId) ? name : _npcId;
        public string DisplayName => _displayName;
        public string Title => _title;
        public Sprite Portrait => _portrait;
        public Color ThemeColor => _themeColor;
        public Personality.Data.NPCPersonalityData PersonalityData => _personalityData;
        public Memory.Data.NPCMemoryData MemoryData => _memoryData;
        public Speech.Data.TTSSettings VoiceSettings => _voiceSettings;
        public float InteractionDistance => _interactionDistance;
        public bool CanBeInterrupted => _canBeInterrupted;
        public float ResponseDelay => _responseDelay;
        public string WorldContext => _worldContext;
        public TimeOfDayBehavior TimeOfDayBehavior => _timeOfDayBehavior;
        public Vector2 AvailableHours => _availableHours;
        
        #endregion
        
        #region Odin Buttons
        
#if ODIN_INSPECTOR
        [Title("Quick Actions")]
        [ButtonGroup("Actions")]
        [Button("Create Personality", ButtonHeight = 30)]
        [GUIColor(0.3f, 0.8f, 0.3f)]
        private void CreatePersonalityData()
        {
            UnityEngine.Debug.Log("Use the NPC Setup Wizard (Tools > Interactive NPC > NPC Setup Wizard) to create assets");
        }
        
        [ButtonGroup("Actions")]
        [Button("Create Memory", ButtonHeight = 30)]
        [GUIColor(0.3f, 0.6f, 0.9f)]
        private void CreateMemoryData()
        {
            UnityEngine.Debug.Log("Use the NPC Setup Wizard (Tools > Interactive NPC > NPC Setup Wizard) to create assets");
        }
        
        [ButtonGroup("Actions")]
        [Button("Validate Config", ButtonHeight = 30)]
        [GUIColor(0.9f, 0.7f, 0.3f)]
        private void ValidateConfiguration()
        {
            List<string> issues = new List<string>();
            List<string> warnings = new List<string>();
            
            // Errors
            if (string.IsNullOrEmpty(_displayName))
                issues.Add("Display name is empty");
            
            if (_personalityData == null)
                issues.Add("Personality data is not assigned");
            
            // Warnings
            if (_memoryData == null)
                warnings.Add("Memory data not assigned - NPC won't remember conversations");
            
            if (_voiceSettings == null)
                warnings.Add("Voice settings not assigned - NPC will be silent");
            
            if (string.IsNullOrEmpty(_worldContext))
                warnings.Add("World context is empty - consider adding context for better LLM responses");
            
            if (_portrait == null)
                warnings.Add("No portrait assigned");
            
            // Report
            if (issues.Count == 0 && warnings.Count == 0)
            {
                UnityEngine.Debug.Log($"✓ {_displayName} configuration is valid!");
            }
            else
            {
                if (issues.Count > 0)
                    UnityEngine.Debug.LogError($"❌ Configuration errors:\n- " + string.Join("\n- ", issues));
                
                if (warnings.Count > 0)
                    UnityEngine.Debug.LogWarning($"⚠ Configuration warnings:\n- " + string.Join("\n- ", warnings));
            }
        }
        
        [Title("Preview")]
        [Button("Preview System Prompt", ButtonHeight = 35)]
        [GUIColor(0.5f, 0.7f, 0.9f)]
        private void PreviewSystemPrompt()
        {
            if (_personalityData == null)
            {
                UnityEngine.Debug.LogWarning("Personality data not assigned - cannot preview system prompt");
                return;
            }
            
            string prompt = _personalityData.GenerateSystemPrompt();
            
            if (!string.IsNullOrEmpty(_worldContext))
            {
                prompt += $"\n\n[World Context]\n{_worldContext}";
            }
            
            UnityEngine.Debug.Log($"=== System Prompt for {_displayName} ===\n{prompt}");
        }
        
        [Button("Test Greeting")]
        private void TestGreeting()
        {
            UnityEngine.Debug.Log($"Random greeting: {GetGreeting()}");
        }
        
        [Button("Test Farewell")]
        private void TestFarewell()
        {
            UnityEngine.Debug.Log($"Random farewell: {GetFarewell()}");
        }
#endif
        
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
    
    #region MinMaxSlider Attribute (Fallback)
    
#if !ODIN_INSPECTOR
    /// <summary>
    /// Fallback for Odin's [MinMaxSlider] attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    public class MinMaxSliderAttribute : PropertyAttribute
    {
        public float Min { get; private set; }
        public float Max { get; private set; }
        public bool ShowFields { get; set; }
        
        public MinMaxSliderAttribute(float min, float max, bool showFields = false)
        {
            Min = min;
            Max = max;
            ShowFields = showFields;
        }
    }
    
    /// <summary>
    /// Fallback for Odin's [PreviewField] attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    public class PreviewFieldAttribute : PropertyAttribute
    {
        public float Height { get; private set; }
        public ObjectFieldAlignment Alignment { get; set; }
        
        public PreviewFieldAttribute(float height = 64, ObjectFieldAlignment alignment = ObjectFieldAlignment.Left)
        {
            Height = height;
            Alignment = alignment;
        }
    }
    
    public enum ObjectFieldAlignment
    {
        Left,
        Center,
        Right
    }
    
    /// <summary>
    /// Fallback for Odin's [HorizontalGroup] attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public class HorizontalGroupAttribute : PropertyAttribute
    {
        public string GroupName { get; private set; }
        public float Width { get; set; }
        
        public HorizontalGroupAttribute(string groupName, float width = 0)
        {
            GroupName = groupName;
            Width = width;
        }
    }
    
    /// <summary>
    /// Fallback for Odin's [VerticalGroup] attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public class VerticalGroupAttribute : PropertyAttribute
    {
        public string GroupName { get; private set; }
        
        public VerticalGroupAttribute(string groupName)
        {
            GroupName = groupName;
        }
    }
    
    /// <summary>
    /// Fallback for Odin's [ButtonGroup] attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class ButtonGroupAttribute : Attribute
    {
        public string GroupName { get; private set; }
        
        public ButtonGroupAttribute(string groupName)
        {
            GroupName = groupName;
        }
    }
    
    /// <summary>
    /// Fallback for Odin's [HideLabel] attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    public class HideLabelAttribute : PropertyAttribute
    {
    }
#endif
    
    #endregion
}
