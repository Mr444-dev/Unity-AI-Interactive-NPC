using System;
using System.Collections.Generic;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#else
using InteractiveNPC.Core.Attributes;
#endif

namespace InteractiveNPC.Personality.Data
{
    #region Big Five Personality Traits (Enhanced)
    
    /// <summary>
    /// Big Five (OCEAN) personality model traits with Odin Inspector support.
    /// Each trait is measured on a scale of 0 to 1.
    /// </summary>
    [Serializable]
    public class BigFiveTraitsEnhanced
    {
        [Title("Big Five Personality Traits", "OCEAN Model (0-1 scale)")]
        
        [BoxGroup("Traits")]
        [ProgressBar(0, 1, 0.2f, 0.7f, 0.3f)]
        [LabelText("Openness")]
        [Tooltip("Creativity, curiosity, willingness to try new things")]
        [SerializeField]
        private float _openness = 0.5f;
        
        [BoxGroup("Traits")]
        [ProgressBar(0, 1, 0.3f, 0.5f, 0.8f)]
        [LabelText("Conscientiousness")]
        [Tooltip("Organization, dependability, self-discipline")]
        [SerializeField]
        private float _conscientiousness = 0.5f;
        
        [BoxGroup("Traits")]
        [ProgressBar(0, 1, 0.9f, 0.6f, 0.2f)]
        [LabelText("Extraversion")]
        [Tooltip("Sociability, assertiveness, positive emotions")]
        [SerializeField]
        private float _extraversion = 0.5f;
        
        [BoxGroup("Traits")]
        [ProgressBar(0, 1, 0.9f, 0.4f, 0.6f)]
        [LabelText("Agreeableness")]
        [Tooltip("Cooperation, trust, helpfulness")]
        [SerializeField]
        private float _agreeableness = 0.5f;
        
        [BoxGroup("Traits")]
        [ProgressBar(0, 1, 0.8f, 0.2f, 0.2f)]
        [LabelText("Neuroticism")]
        [Tooltip("Emotional instability, anxiety, moodiness")]
        [SerializeField]
        private float _neuroticism = 0.5f;
        
        #region Properties
        
        public float Openness => _openness;
        public float Conscientiousness => _conscientiousness;
        public float Extraversion => _extraversion;
        public float Agreeableness => _agreeableness;
        public float Neuroticism => _neuroticism;
        
        #endregion
        
        #region Constructor
        
        public BigFiveTraitsEnhanced() { }
        
        public BigFiveTraitsEnhanced(float openness, float conscientiousness, float extraversion, float agreeableness, float neuroticism)
        {
            _openness = Mathf.Clamp01(openness);
            _conscientiousness = Mathf.Clamp01(conscientiousness);
            _extraversion = Mathf.Clamp01(extraversion);
            _agreeableness = Mathf.Clamp01(agreeableness);
            _neuroticism = Mathf.Clamp01(neuroticism);
        }
        
        #endregion
        
        #region Public Methods
        
#if ODIN_INSPECTOR
        [Button("Randomize Traits"), PropertyOrder(100)]
        private void RandomizeTraits()
        {
            _openness = UnityEngine.Random.value;
            _conscientiousness = UnityEngine.Random.value;
            _extraversion = UnityEngine.Random.value;
            _agreeableness = UnityEngine.Random.value;
            _neuroticism = UnityEngine.Random.value;
        }
        
        [Button("Reset to Average"), PropertyOrder(101)]
        private void ResetToAverage()
        {
            _openness = 0.5f;
            _conscientiousness = 0.5f;
            _extraversion = 0.5f;
            _agreeableness = 0.5f;
            _neuroticism = 0.5f;
        }
#endif
        
        /// <summary>
        /// Get personality description for LLM context.
        /// </summary>
        public string GetDescription()
        {
            List<string> traits = new List<string>();
            
            // Openness
            if (_openness > 0.7f)
                traits.Add("very creative and curious");
            else if (_openness > 0.5f)
                traits.Add("somewhat open to new ideas");
            else if (_openness < 0.3f)
                traits.Add("traditional and practical");
            
            // Conscientiousness
            if (_conscientiousness > 0.7f)
                traits.Add("highly organized and disciplined");
            else if (_conscientiousness < 0.3f)
                traits.Add("spontaneous and flexible");
            
            // Extraversion
            if (_extraversion > 0.7f)
                traits.Add("outgoing and talkative");
            else if (_extraversion > 0.5f)
                traits.Add("moderately social");
            else if (_extraversion < 0.3f)
                traits.Add("reserved and quiet");
            
            // Agreeableness
            if (_agreeableness > 0.7f)
                traits.Add("very friendly and helpful");
            else if (_agreeableness < 0.3f)
                traits.Add("direct and sometimes blunt");
            
            // Neuroticism
            if (_neuroticism > 0.7f)
                traits.Add("emotionally sensitive");
            else if (_neuroticism < 0.3f)
                traits.Add("emotionally stable and calm");
            
            return traits.Count > 0 ? string.Join(", ", traits) : "of average temperament";
        }
        
        /// <summary>
        /// Get a simple personality archetype based on dominant traits.
        /// </summary>
        public string GetArchetype()
        {
            float maxTrait = Mathf.Max(_openness, _conscientiousness, _extraversion, _agreeableness, _neuroticism);
            
            if (maxTrait == _openness) return "The Creative";
            if (maxTrait == _conscientiousness) return "The Organizer";
            if (maxTrait == _extraversion) return "The Social Butterfly";
            if (maxTrait == _agreeableness) return "The Peacemaker";
            if (maxTrait == _neuroticism) return "The Sensitive Soul";
            
            return "The Balanced";
        }
        
        #endregion
    }
    
    #endregion
    
    #region Speech Pattern Settings (Enhanced)
    
    /// <summary>
    /// Configuration for NPC speech patterns and mannerisms with Odin support.
    /// </summary>
    [Serializable]
    public class SpeechPatternSettingsEnhanced
    {
        [Title("Speech Style")]
        [BoxGroup("Style")]
        [ProgressBar(0, 1, 0.4f, 0.4f, 0.6f)]
        [LabelText("Formality (Casual ← → Formal)")]
        [SerializeField]
        private float _formality = 0.5f;
        
        [BoxGroup("Style")]
        [ProgressBar(0, 1, 0.5f, 0.6f, 0.4f)]
        [LabelText("Verbosity (Brief ← → Detailed)")]
        [SerializeField]
        private float _verbosity = 0.5f;
        
        [BoxGroup("Style")]
        [ProgressBar(0, 1, 0.3f, 0.7f, 0.5f)]
        [LabelText("Politeness (Rude ← → Polite)")]
        [SerializeField]
        private float _politeness = 0.5f;
        
        [BoxGroup("Style")]
        [ProgressBar(0, 1, 0.9f, 0.7f, 0.2f)]
        [LabelText("Humor Level")]
        [SerializeField]
        private float _humor = 0.3f;
        
        [Title("Vocabulary")]
        [FoldoutGroup("Vocabulary", false)]
        [LabelText("Catch Phrases")]
        [Tooltip("Common phrases this NPC uses")]
        [SerializeField]
        private List<string> _catchPhrases = new List<string>();
        
        [FoldoutGroup("Vocabulary")]
        [LabelText("Avoided Phrases")]
        [Tooltip("Words or phrases to avoid")]
        [SerializeField]
        private List<string> _avoidedPhrases = new List<string>();
        
        [FoldoutGroup("Vocabulary")]
        [LabelText("Dialect Description")]
        [Tooltip("Dialect or accent description")]
        [SerializeField]
        private string _dialectDescription = "";
        
        [Title("Quirks")]
        [FoldoutGroup("Quirks", false)]
        [LabelText("Verbal Tics")]
        [Tooltip("Verbal tics or mannerisms")]
        [SerializeField]
        private List<string> _verbalTics = new List<string>();
        
        [FoldoutGroup("Quirks")]
        [LabelText("Favorite Topics")]
        [Tooltip("Topics this NPC loves to discuss")]
        [SerializeField]
        private List<string> _favoriteTopics = new List<string>();
        
        [FoldoutGroup("Quirks")]
        [LabelText("Avoided Topics")]
        [Tooltip("Topics this NPC avoids")]
        [SerializeField]
        private List<string> _avoidedTopics = new List<string>();
        
        #region Properties
        
        public float Formality => _formality;
        public float Verbosity => _verbosity;
        public float Politeness => _politeness;
        public float Humor => _humor;
        public IReadOnlyList<string> CatchPhrases => _catchPhrases;
        public IReadOnlyList<string> AvoidedPhrases => _avoidedPhrases;
        public string DialectDescription => _dialectDescription;
        public IReadOnlyList<string> VerbalTics => _verbalTics;
        public IReadOnlyList<string> FavoriteTopics => _favoriteTopics;
        public IReadOnlyList<string> AvoidedTopics => _avoidedTopics;
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Get speech style description for LLM context.
        /// </summary>
        public string GetStyleDescription()
        {
            List<string> styles = new List<string>();
            
            if (_formality > 0.7f)
                styles.Add("speaks formally and professionally");
            else if (_formality < 0.3f)
                styles.Add("speaks casually and informally");
            
            if (_verbosity > 0.7f)
                styles.Add("tends to be detailed and thorough in explanations");
            else if (_verbosity < 0.3f)
                styles.Add("prefers brief, to-the-point responses");
            
            if (_politeness < 0.3f)
                styles.Add("can be blunt or even rude");
            else if (_politeness > 0.7f)
                styles.Add("always maintains politeness");
            
            if (_humor > 0.6f)
                styles.Add("often makes jokes or uses humor");
            
            if (!string.IsNullOrEmpty(_dialectDescription))
                styles.Add(_dialectDescription);
            
            return string.Join("; ", styles);
        }
        
        /// <summary>
        /// Get a random catch phrase if available.
        /// </summary>
        public string GetRandomCatchPhrase()
        {
            if (_catchPhrases.Count == 0) return "";
            return _catchPhrases[UnityEngine.Random.Range(0, _catchPhrases.Count)];
        }
        
        #endregion
    }
    
    #endregion
    
    #region NPC Background Info (Enhanced)
    
    /// <summary>
    /// Background information and lore for the NPC with Odin support.
    /// </summary>
    [Serializable]
    public class NPCBackgroundInfoEnhanced
    {
        [Title("Basic Information")]
        [BoxGroup("Identity")]
        [Required("Profession is required!")]
        [LabelText("Profession/Role")]
        [SerializeField]
        private string _profession = "Merchant";
        
        [BoxGroup("Identity")]
        [LabelText("Age Description")]
        [ValueDropdown("GetAgeOptions")]
        [SerializeField]
        private string _ageDescription = "middle-aged";
        
        [BoxGroup("Identity")]
        [LabelText("Physical Description")]
        [TextArea(2, 4)]
        [SerializeField]
        private string _physicalDescription = "";
        
        [Title("Background")]
        [FoldoutGroup("Backstory", true)]
        [HideLabel]
        [TextArea(3, 10)]
        [SerializeField]
        private string _backstory = "";
        
        [FoldoutGroup("Motivations", false)]
        [LabelText("Goals & Motivations")]
        [TextArea(2, 5)]
        [SerializeField]
        private string _motivations = "";
        
        [FoldoutGroup("Motivations")]
        [LabelText("Fears & Weaknesses")]
        [TextArea(2, 5)]
        [SerializeField]
        private string _fears = "";
        
        [Title("Knowledge")]
        [FoldoutGroup("Knowledge", false)]
        [LabelText("Areas of Expertise")]
        [SerializeField]
        private List<string> _expertiseAreas = new List<string>();
        
        [FoldoutGroup("Knowledge")]
        [LabelText("Secrets Known")]
        [TextArea(2, 5)]
        [SerializeField]
        private string _secrets = "";
        
        [Title("Social")]
        [FoldoutGroup("Relationships", false)]
        [LabelText("Allies")]
        [SerializeField]
        private List<string> _allies = new List<string>();
        
        [FoldoutGroup("Relationships")]
        [LabelText("Rivals/Enemies")]
        [SerializeField]
        private List<string> _rivals = new List<string>();
        
        #region Properties
        
        public string Profession => _profession;
        public string AgeDescription => _ageDescription;
        public string PhysicalDescription => _physicalDescription;
        public string Backstory => _backstory;
        public string Motivations => _motivations;
        public string Fears => _fears;
        public IReadOnlyList<string> ExpertiseAreas => _expertiseAreas;
        public string Secrets => _secrets;
        public IReadOnlyList<string> Allies => _allies;
        public IReadOnlyList<string> Rivals => _rivals;
        
        #endregion
        
        #region Value Dropdown
        
#if ODIN_INSPECTOR
        private static IEnumerable<string> GetAgeOptions()
        {
            return new[] { "young", "young adult", "middle-aged", "mature", "elderly", "ancient" };
        }
#endif
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Get background summary for LLM context.
        /// </summary>
        public string GetBackgroundSummary()
        {
            List<string> parts = new List<string>();
            
            parts.Add($"You are a {_ageDescription} {_profession}.");
            
            if (!string.IsNullOrEmpty(_backstory))
                parts.Add(_backstory);
            
            if (!string.IsNullOrEmpty(_motivations))
                parts.Add($"Your motivations: {_motivations}");
            
            if (_expertiseAreas.Count > 0)
                parts.Add($"You are knowledgeable about: {string.Join(", ", _expertiseAreas)}.");
            
            return string.Join(" ", parts);
        }
        
        #endregion
    }
    
    #endregion
    
    #region NPC Personality Data ScriptableObject (Enhanced)
    
    /// <summary>
    /// Enhanced ScriptableObject defining the complete personality profile for an NPC.
    /// Includes Odin Inspector support with graceful fallback.
    /// </summary>
    [CreateAssetMenu(fileName = "NPCPersonalityDataEnhanced", menuName = "Interactive NPC/Personality/NPC Personality Data (Enhanced)")]
    public class NPCPersonalityDataEnhanced : ScriptableObject
    {
        #region Serialized Fields
        
        [Title("NPC Identity", "Core identification information")]
        [BoxGroup("Identity")]
        [Required("Display name is required!")]
        [LabelText("Display Name")]
        [SerializeField]
        private string _displayName = "NPC";
        
        [BoxGroup("Identity")]
        [LabelText("Personality ID")]
        [InfoBox("Leave empty to use asset name as ID")]
        [SerializeField]
        private string _personalityId = "";
        
        [Title("Personality Traits")]
        [TabGroup("Main", "Personality")]
        [HideLabel]
        [SerializeField]
        private BigFiveTraitsEnhanced _bigFiveTraits = new BigFiveTraitsEnhanced();
        
        [Title("Speech Patterns")]
        [TabGroup("Main", "Speech")]
        [HideLabel]
        [SerializeField]
        private SpeechPatternSettingsEnhanced _speechPatterns = new SpeechPatternSettingsEnhanced();
        
        [Title("Background & Lore")]
        [TabGroup("Main", "Background")]
        [HideLabel]
        [SerializeField]
        private NPCBackgroundInfoEnhanced _backgroundInfo = new NPCBackgroundInfoEnhanced();
        
        [Title("Behavioral Settings")]
        [TabGroup("Main", "Behavior")]
        [BoxGroup("Main/Behavior/Emotional")]
        [LabelText("Temperament")]
        [Tooltip("How easily this NPC gets upset (-1 = very patient, 1 = short-tempered)")]
        [Range(-1f, 1f)]
        [SerializeField]
        private float _temperament = 0f;
        
        [BoxGroup("Main/Behavior/Emotional")]
        [LabelText("Mood Recovery Rate")]
        [Tooltip("How quickly mood recovers to neutral")]
        [Range(0.01f, 1f)]
        [SerializeField]
        private float _moodRecoveryRate = 0.1f;
        
        [BoxGroup("Main/Behavior/Emotional")]
        [LabelText("Emotional Sensitivity")]
        [Tooltip("How strongly emotions are affected")]
        [Range(0.1f, 2f)]
        [SerializeField]
        private float _emotionalSensitivity = 1f;
        
        [Title("Custom Prompt Instructions")]
        [TabGroup("Main", "Advanced")]
        [TextArea(3, 10)]
        [InfoBox("Additional instructions appended to the LLM prompt", InfoMessageType.Info)]
        [SerializeField]
        private string _customInstructions = "";
        
        #endregion
        
        #region Properties
        
        public string DisplayName => _displayName;
        public string PersonalityId => string.IsNullOrEmpty(_personalityId) ? name : _personalityId;
        public BigFiveTraitsEnhanced BigFiveTraits => _bigFiveTraits;
        public SpeechPatternSettingsEnhanced SpeechPatterns => _speechPatterns;
        public NPCBackgroundInfoEnhanced BackgroundInfo => _backgroundInfo;
        public float Temperament => _temperament;
        public float MoodRecoveryRate => _moodRecoveryRate;
        public float EmotionalSensitivity => _emotionalSensitivity;
        public string CustomInstructions => _customInstructions;
        
        #endregion
        
        #region Odin Buttons
        
#if ODIN_INSPECTOR
        [Title("Actions")]
        [Button("Generate System Prompt Preview", ButtonHeight = 30)]
        [GUIColor(0.3f, 0.8f, 0.3f)]
        private void PreviewSystemPrompt()
        {
            string prompt = GenerateSystemPrompt();
            UnityEngine.Debug.Log($"=== System Prompt for {_displayName} ===\n{prompt}");
        }
        
        [Button("Validate Configuration")]
        private void ValidateConfiguration()
        {
            List<string> issues = new List<string>();
            
            if (string.IsNullOrEmpty(_displayName))
                issues.Add("Display name is empty");
            
            if (string.IsNullOrEmpty(_backgroundInfo.Profession))
                issues.Add("Profession is empty");
            
            if (issues.Count == 0)
            {
                UnityEngine.Debug.Log($"✓ {_displayName} configuration is valid!");
            }
            else
            {
                UnityEngine.Debug.LogWarning($"Configuration issues for {_displayName}:\n- " + string.Join("\n- ", issues));
            }
        }
        
        [Button("Copy Prompt to Clipboard")]
        private void CopyPromptToClipboard()
        {
            GUIUtility.systemCopyBuffer = GenerateSystemPrompt();
            UnityEngine.Debug.Log("System prompt copied to clipboard!");
        }
#endif
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Generate the full system prompt for this NPC personality.
        /// </summary>
        public string GenerateSystemPrompt()
        {
            List<string> promptParts = new List<string>();
            
            // Identity
            promptParts.Add($"You are {_displayName}.");
            
            // Background
            promptParts.Add(_backgroundInfo.GetBackgroundSummary());
            
            // Personality traits
            string traitDesc = _bigFiveTraits.GetDescription();
            if (!string.IsNullOrEmpty(traitDesc))
                promptParts.Add($"Your personality: {traitDesc}.");
            
            // Archetype
            promptParts.Add($"Your archetype: {_bigFiveTraits.GetArchetype()}.");
            
            // Speech style
            string speechDesc = _speechPatterns.GetStyleDescription();
            if (!string.IsNullOrEmpty(speechDesc))
                promptParts.Add($"Your speech style: {speechDesc}.");
            
            // Custom instructions
            if (!string.IsNullOrEmpty(_customInstructions))
                promptParts.Add(_customInstructions);
            
            // General guidelines
            promptParts.Add("Stay in character at all times. Respond naturally as this character would.");
            promptParts.Add("Base your responses on your personality, memories, and current mood.");
            
            return string.Join("\n\n", promptParts);
        }
        
        /// <summary>
        /// Get a compact personality summary for short contexts.
        /// </summary>
        public string GetCompactSummary()
        {
            return $"{_displayName}, {_backgroundInfo.AgeDescription} {_backgroundInfo.Profession}. " +
                   $"Personality: {_bigFiveTraits.GetDescription()}. " +
                   $"Archetype: {_bigFiveTraits.GetArchetype()}.";
        }
        
        #endregion
        
        #region Validation
        
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_personalityId))
            {
                _personalityId = name;
            }
        }
        
        #endregion
    }
    
    #endregion
}
