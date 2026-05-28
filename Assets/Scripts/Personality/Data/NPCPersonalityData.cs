using System;
using System.Collections.Generic;
using UnityEngine;

namespace InteractiveNPC.Personality.Data
{
    #region Big Five Personality Traits
    
    /// <summary>
    /// Big Five (OCEAN) personality model traits.
    /// Each trait is measured on a scale of 0 to 1.
    /// </summary>
    [Serializable]
    public class BigFiveTraits
    {
        [Header("Big Five Personality Traits (0-1 scale)")]
        
        [SerializeField, Tooltip("Openness to Experience: Creativity, curiosity, willingness to try new things")]
        [Range(0f, 1f)]
        private float _openness = 0.5f;
        
        [SerializeField, Tooltip("Conscientiousness: Organization, dependability, self-discipline")]
        [Range(0f, 1f)]
        private float _conscientiousness = 0.5f;
        
        [SerializeField, Tooltip("Extraversion: Sociability, assertiveness, positive emotions")]
        [Range(0f, 1f)]
        private float _extraversion = 0.5f;
        
        [SerializeField, Tooltip("Agreeableness: Cooperation, trust, helpfulness")]
        [Range(0f, 1f)]
        private float _agreeableness = 0.5f;
        
        [SerializeField, Tooltip("Neuroticism: Emotional instability, anxiety, moodiness")]
        [Range(0f, 1f)]
        private float _neuroticism = 0.5f;
        
        #region Properties
        
        public float Openness => _openness;
        public float Conscientiousness => _conscientiousness;
        public float Extraversion => _extraversion;
        public float Agreeableness => _agreeableness;
        public float Neuroticism => _neuroticism;
        
        #endregion
        
        #region Constructor
        
        public BigFiveTraits() { }
        
        public BigFiveTraits(float openness, float conscientiousness, float extraversion, float agreeableness, float neuroticism)
        {
            _openness = Mathf.Clamp01(openness);
            _conscientiousness = Mathf.Clamp01(conscientiousness);
            _extraversion = Mathf.Clamp01(extraversion);
            _agreeableness = Mathf.Clamp01(agreeableness);
            _neuroticism = Mathf.Clamp01(neuroticism);
        }
        
        #endregion
        
        #region Public Methods
        
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
        
        #endregion
    }
    
    #endregion
    
    #region Speech Pattern Settings
    
    /// <summary>
    /// Configuration for NPC speech patterns and mannerisms.
    /// </summary>
    [Serializable]
    public class SpeechPatternSettings
    {
        [Header("Speech Style")]
        [SerializeField, Tooltip("Formality level: 0 = casual, 1 = very formal")]
        [Range(0f, 1f)]
        private float _formality = 0.5f;
        
        [SerializeField, Tooltip("Verbosity: 0 = brief, 1 = detailed")]
        [Range(0f, 1f)]
        private float _verbosity = 0.5f;
        
        [SerializeField, Tooltip("Politeness: 0 = rude, 1 = very polite")]
        [Range(0f, 1f)]
        private float _politeness = 0.5f;
        
        [SerializeField, Tooltip("Humor level: 0 = serious, 1 = very humorous")]
        [Range(0f, 1f)]
        private float _humor = 0.3f;
        
        [Header("Vocabulary")]
        [SerializeField, Tooltip("Common phrases this NPC uses")]
        private List<string> _catchPhrases = new List<string>();
        
        [SerializeField, Tooltip("Words or phrases to avoid")]
        private List<string> _avoidedPhrases = new List<string>();
        
        [SerializeField, Tooltip("Dialect or accent description")]
        private string _dialectDescription = "";
        
        [Header("Quirks")]
        [SerializeField, Tooltip("Verbal tics or mannerisms")]
        private List<string> _verbalTics = new List<string>();
        
        [SerializeField, Tooltip("Topics this NPC loves to discuss")]
        private List<string> _favoriteTopics = new List<string>();
        
        [SerializeField, Tooltip("Topics this NPC avoids")]
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
    
    #region NPC Background Info
    
    /// <summary>
    /// Background information and lore for the NPC.
    /// </summary>
    [Serializable]
    public class NPCBackgroundInfo
    {
        [Header("Basic Information")]
        [SerializeField, Tooltip("NPC's profession or role")]
        private string _profession = "Merchant";
        
        [SerializeField, Tooltip("Age description (young, middle-aged, elderly, etc.)")]
        private string _ageDescription = "middle-aged";
        
        [SerializeField, Tooltip("Brief physical description")]
        [TextArea(2, 4)]
        private string _physicalDescription = "";
        
        [Header("Background")]
        [SerializeField, Tooltip("NPC's backstory")]
        [TextArea(3, 10)]
        private string _backstory = "";
        
        [SerializeField, Tooltip("NPC's motivations and goals")]
        [TextArea(2, 5)]
        private string _motivations = "";
        
        [SerializeField, Tooltip("NPC's fears or weaknesses")]
        [TextArea(2, 5)]
        private string _fears = "";
        
        [Header("Knowledge")]
        [SerializeField, Tooltip("Topics this NPC is knowledgeable about")]
        private List<string> _expertiseAreas = new List<string>();
        
        [SerializeField, Tooltip("Secrets the NPC knows")]
        [TextArea(2, 5)]
        private string _secrets = "";
        
        [Header("Social")]
        [SerializeField, Tooltip("NPCs this character likes")]
        private List<string> _allies = new List<string>();
        
        [SerializeField, Tooltip("NPCs this character dislikes")]
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
    
    #region NPC Personality Data ScriptableObject
    
    /// <summary>
    /// ScriptableObject defining the complete personality profile for an NPC.
    /// Includes personality traits, speech patterns, and background information.
    /// </summary>
    [CreateAssetMenu(fileName = "NPCPersonalityData", menuName = "Interactive NPC/Personality/NPC Personality Data")]
    public class NPCPersonalityData : ScriptableObject
    {
        #region Serialized Fields
        
        [Header("NPC Identity")]
        [SerializeField, Tooltip("Display name of the NPC")]
        private string _displayName = "NPC";
        
        [SerializeField, Tooltip("Unique identifier for this personality")]
        private string _personalityId = "";
        
        [Header("Personality")]
        [SerializeField]
        private BigFiveTraits _bigFiveTraits = new BigFiveTraits();
        
        [Header("Speech")]
        [SerializeField]
        private SpeechPatternSettings _speechPatterns = new SpeechPatternSettings();
        
        [Header("Background")]
        [SerializeField]
        private NPCBackgroundInfo _backgroundInfo = new NPCBackgroundInfo();
        
        [Header("Behavioral Settings")]
        [SerializeField, Tooltip("How easily this NPC gets upset (-1 = very patient, 1 = short-tempered)")]
        [Range(-1f, 1f)]
        private float _temperament = 0f;
        
        [SerializeField, Tooltip("How quickly mood recovers to neutral")]
        [Range(0.01f, 1f)]
        private float _moodRecoveryRate = 0.1f;
        
        [SerializeField, Tooltip("Emotional sensitivity (how strongly emotions are affected)")]
        [Range(0.1f, 2f)]
        private float _emotionalSensitivity = 1f;
        
        [Header("Custom Prompt Instructions")]
        [SerializeField, Tooltip("Additional instructions for the LLM prompt")]
        [TextArea(3, 10)]
        private string _customInstructions = "";
        
        #endregion
        
        #region Properties
        
        public string DisplayName => _displayName;
        public string PersonalityId => string.IsNullOrEmpty(_personalityId) ? name : _personalityId;
        public BigFiveTraits BigFiveTraits => _bigFiveTraits;
        public SpeechPatternSettings SpeechPatterns => _speechPatterns;
        public NPCBackgroundInfo BackgroundInfo => _backgroundInfo;
        public float Temperament => _temperament;
        public float MoodRecoveryRate => _moodRecoveryRate;
        public float EmotionalSensitivity => _emotionalSensitivity;
        public string CustomInstructions => _customInstructions;
        
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
                   $"Personality: {_bigFiveTraits.GetDescription()}.";
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
