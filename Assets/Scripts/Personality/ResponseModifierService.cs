using System;
using System.Collections.Generic;
using UnityEngine;
using InteractiveNPC.Core.Utilities;
using InteractiveNPC.Memory;
using InteractiveNPC.Personality.Data;

namespace InteractiveNPC.Personality
{
    #region Response Modifier Service Interface
    
    /// <summary>
    /// Interface for response modification based on personality and mood.
    /// </summary>
    public interface IResponseModifierService
    {
        /// <summary>
        /// Modify a response based on current mood and relationship.
        /// </summary>
        string ModifyResponse(string originalResponse, MoodController mood, RelationshipTracker relationship);
        
        /// <summary>
        /// Get prompt additions based on current state.
        /// </summary>
        string GetPromptAdditions(MoodController mood, RelationshipTracker relationship);
    }
    
    #endregion
    
    #region Response Modifier Service Implementation
    
    /// <summary>
    /// Service that modifies LLM responses based on NPC's current mood,
    /// personality traits, and relationship with the player.
    /// </summary>
    public class ResponseModifierService : IResponseModifierService
    {
        #region Private Fields
        
        private readonly NPCPersonalityData _personality;
        private Dictionary<string, string[]> _emotionalPrefixes;
        private Dictionary<string, string[]> _emotionalSuffixes;
        
        #endregion
        
        #region Constructor
        
        public ResponseModifierService(NPCPersonalityData personality)
        {
            _personality = personality;
            InitializeEmotionalModifiers();
            
            DebugLogger.LogInfo($"ResponseModifierService created for: {personality?.DisplayName ?? "Unknown"}", LogCategory.Personality);
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Modify a response based on current mood and relationship.
        /// Returns the original response if no significant modifications needed.
        /// </summary>
        public string ModifyResponse(string originalResponse, MoodController mood, RelationshipTracker relationship)
        {
            if (string.IsNullOrEmpty(originalResponse))
                return originalResponse;
            
            string modified = originalResponse;
            
            // Apply mood-based modifications
            if (mood != null)
            {
                modified = ApplyMoodModifications(modified, mood);
            }
            
            // Apply relationship-based modifications
            if (relationship != null)
            {
                modified = ApplyRelationshipModifications(modified, relationship);
            }
            
            // Apply personality quirks
            modified = ApplyPersonalityQuirks(modified);
            
            return modified;
        }
        
        /// <summary>
        /// Get additional prompt instructions based on current state.
        /// </summary>
        public string GetPromptAdditions(MoodController mood, RelationshipTracker relationship)
        {
            List<string> additions = new List<string>();
            
            // Mood-based instructions
            if (mood != null)
            {
                additions.Add(mood.GetMoodDescription());
                
                MoodModifiers modifiers = mood.GetMoodModifiers();
                
                if (modifiers.IsNegative)
                {
                    additions.Add("Your responses should reflect your current negative emotional state.");
                }
                else if (modifiers.IsPositive)
                {
                    additions.Add("Your responses should be warm and positive.");
                }
                
                if (modifiers.Helpfulness < -0.3f)
                {
                    additions.Add("You are less inclined to be helpful right now.");
                }
            }
            
            // Relationship-based instructions
            if (relationship != null)
            {
                additions.Add(relationship.GetContextString());
                
                // Price-related behavior for merchants
                if (_personality?.BackgroundInfo.Profession.ToLower().Contains("merchant") == true ||
                    _personality?.BackgroundInfo.Profession.ToLower().Contains("shop") == true)
                {
                    float priceModifier = relationship.GetPriceModifier();
                    if (priceModifier > 1.1f)
                    {
                        additions.Add($"Because of your relationship, you charge {(priceModifier - 1) * 100:F0}% more than usual.");
                    }
                    else if (priceModifier < 0.95f)
                    {
                        additions.Add($"As a friend, you offer a {(1 - priceModifier) * 100:F0}% discount.");
                    }
                }
            }
            
            return string.Join("\n", additions);
        }
        
        /// <summary>
        /// Get context for sentiment analysis of player input.
        /// </summary>
        public SentimentContext AnalyzePlayerInput(string playerInput)
        {
            if (string.IsNullOrEmpty(playerInput))
            {
                return new SentimentContext { Sentiment = 0f, IsInsult = false, IsCompliment = false };
            }
            
            string lowerInput = playerInput.ToLower();
            
            SentimentContext context = new SentimentContext();
            
            // Check for insults
            string[] insultWords = { "stupid", "idiot", "hate", "ugly", "worthless", "useless", 
                "fool", "dumb", "moron", "terrible", "awful", "worst", "suck", "hate you" };
            
            foreach (string insult in insultWords)
            {
                if (lowerInput.Contains(insult))
                {
                    context.IsInsult = true;
                    context.Sentiment -= 0.3f;
                }
            }
            
            // Check for compliments
            string[] complimentWords = { "thank", "beautiful", "great", "amazing", "wonderful", 
                "love", "best", "excellent", "awesome", "appreciate", "kind", "helpful", "friend" };
            
            foreach (string compliment in complimentWords)
            {
                if (lowerInput.Contains(compliment))
                {
                    context.IsCompliment = true;
                    context.Sentiment += 0.3f;
                }
            }
            
            // Check for threats
            string[] threatWords = { "kill", "hurt", "destroy", "attack", "fight", "die" };
            
            foreach (string threat in threatWords)
            {
                if (lowerInput.Contains(threat))
                {
                    context.IsThreat = true;
                    context.Sentiment -= 0.5f;
                }
            }
            
            // Clamp sentiment
            context.Sentiment = Mathf.Clamp(context.Sentiment, -1f, 1f);
            
            return context;
        }
        
        #endregion
        
        #region Private Methods
        
        private void InitializeEmotionalModifiers()
        {
            _emotionalPrefixes = new Dictionary<string, string[]>
            {
                { "Happy", new[] { "*smiles warmly* ", "*chuckles* ", "" } },
                { "Angry", new[] { "*frowns* ", "*crosses arms* ", "*glares* " } },
                { "Sad", new[] { "*sighs* ", "*looks down* ", "" } },
                { "Fearful", new[] { "*nervously* ", "*looks around* ", "" } },
                { "Neutral", new[] { "" } }
            };
            
            _emotionalSuffixes = new Dictionary<string, string[]>
            {
                { "Happy", new[] { "", " *smiles*", "" } },
                { "Angry", new[] { "", " *mutters*", "" } },
                { "Sad", new[] { "", " *trails off*", "" } },
                { "Fearful", new[] { "", " *glances nervously*", "" } },
                { "Neutral", new[] { "" } }
            };
        }
        
        private string ApplyMoodModifications(string response, MoodController mood)
        {
            string emotionKey = mood.CurrentEmotion.ToString();
            
            // Only add emotional markers if intensity is high enough
            if (mood.Intensity < 0.4f)
                return response;
            
            // Add prefix based on emotion
            if (_emotionalPrefixes.TryGetValue(emotionKey, out string[] prefixes))
            {
                string prefix = prefixes[UnityEngine.Random.Range(0, prefixes.Length)];
                if (!string.IsNullOrEmpty(prefix))
                {
                    response = prefix + response;
                }
            }
            
            // Occasionally add suffix
            if (UnityEngine.Random.value > 0.7f && _emotionalSuffixes.TryGetValue(emotionKey, out string[] suffixes))
            {
                string suffix = suffixes[UnityEngine.Random.Range(0, suffixes.Length)];
                if (!string.IsNullOrEmpty(suffix))
                {
                    response = response + suffix;
                }
            }
            
            return response;
        }
        
        private string ApplyRelationshipModifications(string response, RelationshipTracker relationship)
        {
            RelationshipStatus status = relationship.Status;
            
            // Hostile NPCs might add dismissive remarks
            if (status == RelationshipStatus.Hostile || status == RelationshipStatus.Enemy)
            {
                if (UnityEngine.Random.value > 0.7f)
                {
                    string[] hostileAdditions = { " Now leave me alone.", " I don't have time for you.", "" };
                    response += hostileAdditions[UnityEngine.Random.Range(0, hostileAdditions.Length)];
                }
            }
            
            // Friendly NPCs might add warm remarks
            if (status == RelationshipStatus.Friend || status == RelationshipStatus.BestFriend)
            {
                if (UnityEngine.Random.value > 0.8f)
                {
                    string[] friendlyAdditions = { " Good to see you, friend.", " Always a pleasure.", "" };
                    response += friendlyAdditions[UnityEngine.Random.Range(0, friendlyAdditions.Length)];
                }
            }
            
            return response;
        }
        
        private string ApplyPersonalityQuirks(string response)
        {
            if (_personality == null) return response;
            
            // Add verbal tics occasionally
            var verbalTics = _personality.SpeechPatterns.VerbalTics;
            if (verbalTics.Count > 0 && UnityEngine.Random.value > 0.85f)
            {
                string tic = verbalTics[UnityEngine.Random.Range(0, verbalTics.Count)];
                // Insert tic at a natural break point
                int commaIndex = response.IndexOf(',');
                if (commaIndex > 0 && commaIndex < response.Length - 1)
                {
                    response = response.Insert(commaIndex + 1, $" {tic}");
                }
            }
            
            return response;
        }
        
        #endregion
    }
    
    #endregion
    
    #region Sentiment Context
    
    /// <summary>
    /// Result of sentiment analysis on player input.
    /// </summary>
    [Serializable]
    public struct SentimentContext
    {
        public float Sentiment;
        public bool IsInsult;
        public bool IsCompliment;
        public bool IsThreat;
        public bool IsQuestion;
        
        public bool IsPositive => Sentiment > 0.2f;
        public bool IsNegative => Sentiment < -0.2f;
        public bool IsNeutral => !IsPositive && !IsNegative;
    }
    
    #endregion
}
