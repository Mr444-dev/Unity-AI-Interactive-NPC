using System;
using UnityEngine;
using InteractiveNPC.Core.Events;
using InteractiveNPC.Core.Utilities;
using InteractiveNPC.Personality.Data;

namespace InteractiveNPC.Personality
{
    #region Mood State Data
    
    /// <summary>
    /// Represents the current mood state of an NPC.
    /// </summary>
    [Serializable]
    public class MoodState
    {
        [SerializeField]
        private EmotionalState _currentEmotion = EmotionalState.Neutral;
        
        [SerializeField, Range(0f, 1f)]
        private float _intensity = 0.5f;
        
        [SerializeField]
        private float _duration;
        
        public EmotionalState CurrentEmotion => _currentEmotion;
        public float Intensity => _intensity;
        public float Duration => _duration;
        
        public MoodState()
        {
            _currentEmotion = EmotionalState.Neutral;
            _intensity = 0.5f;
            _duration = 0f;
        }
        
        public MoodState(EmotionalState emotion, float intensity)
        {
            _currentEmotion = emotion;
            _intensity = Mathf.Clamp01(intensity);
            _duration = 0f;
        }
        
        internal void SetState(EmotionalState emotion, float intensity)
        {
            _currentEmotion = emotion;
            _intensity = Mathf.Clamp01(intensity);
            _duration = 0f;
        }
        
        internal void UpdateDuration(float deltaTime)
        {
            _duration += deltaTime;
        }
        
        internal void ModifyIntensity(float delta)
        {
            _intensity = Mathf.Clamp01(_intensity + delta);
        }
    }
    
    #endregion
    
    #region Mood Controller
    
    /// <summary>
    /// Controls the dynamic mood system for an NPC.
    /// Handles mood transitions, decay, and emotional responses.
    /// </summary>
    public class MoodController
    {
        #region Private Fields
        
        private readonly string _npcId;
        private readonly NPCPersonalityData _personality;
        private readonly MoodState _currentMood;
        private EmotionalState _baselineMood;
        
        #endregion
        
        #region Properties
        
        public EmotionalState CurrentEmotion => _currentMood.CurrentEmotion;
        public float Intensity => _currentMood.Intensity;
        public MoodState CurrentMoodState => _currentMood;
        
        #endregion
        
        #region Constructor
        
        public MoodController(string npcId, NPCPersonalityData personality)
        {
            _npcId = npcId;
            _personality = personality;
            _currentMood = new MoodState();
            _baselineMood = DetermineBaselineMood();
            
            // Set initial mood to baseline
            _currentMood.SetState(_baselineMood, 0.5f);
            
            DebugLogger.LogInfo($"MoodController created for NPC: {npcId}", LogCategory.Personality);
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Update mood over time (call in Update loop).
        /// </summary>
        public void Update(float deltaTime)
        {
            _currentMood.UpdateDuration(deltaTime);
            
            // Decay mood toward baseline based on recovery rate
            float recoveryRate = _personality?.MoodRecoveryRate ?? 0.1f;
            
            if (_currentMood.CurrentEmotion != _baselineMood || Mathf.Abs(_currentMood.Intensity - 0.5f) > 0.01f)
            {
                // Decay intensity toward neutral
                float decayAmount = recoveryRate * deltaTime;
                
                if (_currentMood.Intensity > 0.5f)
                {
                    _currentMood.ModifyIntensity(-decayAmount);
                }
                else if (_currentMood.Intensity < 0.5f)
                {
                    _currentMood.ModifyIntensity(decayAmount);
                }
                
                // If intensity is near neutral, return to baseline emotion
                if (Mathf.Abs(_currentMood.Intensity - 0.5f) < 0.05f)
                {
                    if (_currentMood.CurrentEmotion != _baselineMood)
                    {
                        SetMood(_baselineMood, 0.5f);
                    }
                }
            }
        }
        
        /// <summary>
        /// Apply an emotional stimulus to the NPC.
        /// </summary>
        /// <param name="impact">Emotional impact from -1 (very negative) to 1 (very positive)</param>
        /// <param name="source">Description of what caused the emotion</param>
        public void ApplyEmotionalStimulus(float impact, string source = "")
        {
            float sensitivity = _personality?.EmotionalSensitivity ?? 1f;
            float temperament = _personality?.Temperament ?? 0f;
            
            // Adjust impact based on neuroticism (more neurotic = stronger reactions)
            float neuroticism = _personality?.BigFiveTraits.Neuroticism ?? 0.5f;
            impact *= sensitivity * (0.5f + neuroticism);
            
            // Temperament affects how easily angered
            if (impact < 0)
            {
                impact *= (1f + temperament * 0.5f); // Negative temperament = more easily upset
            }
            
            EmotionalState newEmotion = DetermineEmotionFromImpact(impact);
            float newIntensity = Mathf.Clamp01(0.5f + Mathf.Abs(impact));
            
            EmotionalState previousEmotion = _currentMood.CurrentEmotion;
            float previousIntensity = _currentMood.Intensity;
            
            // Blend with current mood if similar emotion
            if (newEmotion == _currentMood.CurrentEmotion)
            {
                newIntensity = Mathf.Lerp(_currentMood.Intensity, newIntensity, 0.5f);
            }
            
            SetMood(newEmotion, newIntensity);
            
            DebugLogger.LogInfo($"Emotional stimulus applied: {source} -> {newEmotion} (intensity: {newIntensity:F2})", LogCategory.Personality);
            
            // Raise event if significant change
            if (previousEmotion != newEmotion || Mathf.Abs(previousIntensity - newIntensity) > 0.1f)
            {
                GameEvents.RaiseMoodChanged(new MoodEventArgs
                {
                    NPCId = _npcId,
                    PreviousMood = previousEmotion,
                    NewMood = newEmotion,
                    Intensity = newIntensity
                });
            }
        }
        
        /// <summary>
        /// Set the mood directly.
        /// </summary>
        public void SetMood(EmotionalState emotion, float intensity)
        {
            EmotionalState previous = _currentMood.CurrentEmotion;
            _currentMood.SetState(emotion, intensity);
            
            if (previous != emotion)
            {
                GameEvents.RaiseMoodChanged(new MoodEventArgs
                {
                    NPCId = _npcId,
                    PreviousMood = previous,
                    NewMood = emotion,
                    Intensity = intensity
                });
            }
        }
        
        /// <summary>
        /// Get mood description for LLM context.
        /// </summary>
        public string GetMoodDescription()
        {
            string intensityWord = _currentMood.Intensity switch
            {
                > 0.8f => "extremely",
                > 0.6f => "very",
                > 0.4f => "somewhat",
                > 0.2f => "slightly",
                _ => "barely"
            };
            
            string emotionWord = _currentMood.CurrentEmotion switch
            {
                EmotionalState.Happy => "happy and pleased",
                EmotionalState.Angry => "angry and frustrated",
                EmotionalState.Sad => "sad and melancholic",
                EmotionalState.Fearful => "afraid and anxious",
                EmotionalState.Surprised => "surprised",
                EmotionalState.Disgusted => "disgusted",
                EmotionalState.Contemptuous => "contemptuous and dismissive",
                _ => "calm and neutral"
            };
            
            if (_currentMood.CurrentEmotion == EmotionalState.Neutral)
            {
                return "You are feeling calm and composed.";
            }
            
            return $"You are currently feeling {intensityWord} {emotionWord}.";
        }
        
        /// <summary>
        /// Get mood modifiers for response generation.
        /// </summary>
        public MoodModifiers GetMoodModifiers()
        {
            return new MoodModifiers
            {
                Friendliness = CalculateFriendlinessModifier(),
                Verbosity = CalculateVerbosityModifier(),
                Helpfulness = CalculateHelpfulnessModifier(),
                Patience = CalculatePatienceModifier()
            };
        }
        
        /// <summary>
        /// Reset mood to baseline.
        /// </summary>
        public void ResetToBaseline()
        {
            SetMood(_baselineMood, 0.5f);
        }
        
        #endregion
        
        #region Private Methods
        
        private EmotionalState DetermineBaselineMood()
        {
            if (_personality == null) return EmotionalState.Neutral;
            
            float extraversion = _personality.BigFiveTraits.Extraversion;
            float neuroticism = _personality.BigFiveTraits.Neuroticism;
            float agreeableness = _personality.BigFiveTraits.Agreeableness;
            
            // Extraverted and agreeable people tend to be happier
            if (extraversion > 0.7f && agreeableness > 0.6f)
                return EmotionalState.Happy;
            
            // High neuroticism leads to anxiety
            if (neuroticism > 0.7f)
                return EmotionalState.Fearful;
            
            return EmotionalState.Neutral;
        }
        
        private EmotionalState DetermineEmotionFromImpact(float impact)
        {
            if (impact > 0.5f) return EmotionalState.Happy;
            if (impact > 0.2f) return EmotionalState.Neutral;
            if (impact > -0.2f) return EmotionalState.Neutral;
            if (impact > -0.5f) return EmotionalState.Sad;
            return EmotionalState.Angry;
        }
        
        private float CalculateFriendlinessModifier()
        {
            return _currentMood.CurrentEmotion switch
            {
                EmotionalState.Happy => 0.3f * _currentMood.Intensity,
                EmotionalState.Angry => -0.5f * _currentMood.Intensity,
                EmotionalState.Sad => -0.2f * _currentMood.Intensity,
                EmotionalState.Contemptuous => -0.4f * _currentMood.Intensity,
                _ => 0f
            };
        }
        
        private float CalculateVerbosityModifier()
        {
            return _currentMood.CurrentEmotion switch
            {
                EmotionalState.Happy => 0.2f * _currentMood.Intensity,
                EmotionalState.Angry => 0.1f * _currentMood.Intensity,
                EmotionalState.Sad => -0.3f * _currentMood.Intensity,
                EmotionalState.Fearful => -0.2f * _currentMood.Intensity,
                _ => 0f
            };
        }
        
        private float CalculateHelpfulnessModifier()
        {
            return _currentMood.CurrentEmotion switch
            {
                EmotionalState.Happy => 0.4f * _currentMood.Intensity,
                EmotionalState.Angry => -0.6f * _currentMood.Intensity,
                EmotionalState.Contemptuous => -0.5f * _currentMood.Intensity,
                _ => 0f
            };
        }
        
        private float CalculatePatienceModifier()
        {
            return _currentMood.CurrentEmotion switch
            {
                EmotionalState.Happy => 0.3f * _currentMood.Intensity,
                EmotionalState.Angry => -0.5f * _currentMood.Intensity,
                EmotionalState.Fearful => -0.2f * _currentMood.Intensity,
                _ => 0f
            };
        }
        
        #endregion
    }
    
    #endregion
    
    #region Mood Modifiers
    
    /// <summary>
    /// Modifiers to apply to NPC responses based on current mood.
    /// </summary>
    [Serializable]
    public struct MoodModifiers
    {
        public float Friendliness;
        public float Verbosity;
        public float Helpfulness;
        public float Patience;
        
        public bool IsPositive => Friendliness > 0 && Helpfulness > 0;
        public bool IsNegative => Friendliness < 0 || Helpfulness < 0;
    }
    
    #endregion
}
