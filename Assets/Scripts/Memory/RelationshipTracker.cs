using System;
using System.Collections.Generic;
using UnityEngine;
using InteractiveNPC.Core.Events;
using InteractiveNPC.Core.Utilities;

namespace InteractiveNPC.Memory
{
    #region Relationship Data Class
    
    /// <summary>
    /// Stores relationship metrics between the player and an NPC.
    /// </summary>
    [Serializable]
    public class RelationshipData
    {
        #region Serialized Fields
        
        [SerializeField, Tooltip("Trust level (-1 to 1)")]
        [Range(-1f, 1f)]
        private float _trust;
        
        [SerializeField, Tooltip("Respect level (-1 to 1)")]
        [Range(-1f, 1f)]
        private float _respect;
        
        [SerializeField, Tooltip("Fear level (0 to 1)")]
        [Range(0f, 1f)]
        private float _fear;
        
        [SerializeField, Tooltip("Affection level (-1 to 1)")]
        [Range(-1f, 1f)]
        private float _affection;
        
        [SerializeField, Tooltip("Loyalty level (0 to 1)")]
        [Range(0f, 1f)]
        private float _loyalty;
        
        [SerializeField, Tooltip("Number of positive interactions")]
        private int _positiveInteractions;
        
        [SerializeField, Tooltip("Number of negative interactions")]
        private int _negativeInteractions;
        
        [SerializeField, Tooltip("Total number of interactions")]
        private int _totalInteractions;
        
        [SerializeField, Tooltip("First interaction timestamp")]
        private string _firstInteraction;
        
        [SerializeField, Tooltip("Last interaction timestamp")]
        private string _lastInteraction;
        
        #endregion
        
        #region Properties
        
        public float Trust => _trust;
        public float Respect => _respect;
        public float Fear => _fear;
        public float Affection => _affection;
        public float Loyalty => _loyalty;
        public int PositiveInteractions => _positiveInteractions;
        public int NegativeInteractions => _negativeInteractions;
        public int TotalInteractions => _totalInteractions;
        public DateTime FirstInteraction => DateTime.TryParse(_firstInteraction, out DateTime dt) ? dt : DateTime.Now;
        public DateTime LastInteraction => DateTime.TryParse(_lastInteraction, out DateTime dt) ? dt : DateTime.Now;
        
        /// <summary>
        /// Calculate overall relationship score (-1 to 1).
        /// </summary>
        public float OverallScore => (_trust * 0.3f + _respect * 0.25f + _affection * 0.25f + _loyalty * 0.2f) - (_fear * 0.1f);
        
        /// <summary>
        /// Get relationship status based on overall score.
        /// </summary>
        public RelationshipStatus Status
        {
            get
            {
                float score = OverallScore;
                if (score >= 0.7f) return RelationshipStatus.BestFriend;
                if (score >= 0.4f) return RelationshipStatus.Friend;
                if (score >= 0.1f) return RelationshipStatus.Friendly;
                if (score >= -0.1f) return RelationshipStatus.Neutral;
                if (score >= -0.4f) return RelationshipStatus.Unfriendly;
                if (score >= -0.7f) return RelationshipStatus.Hostile;
                return RelationshipStatus.Enemy;
            }
        }
        
        #endregion
        
        #region Constructor
        
        public RelationshipData()
        {
            _trust = 0f;
            _respect = 0f;
            _fear = 0f;
            _affection = 0f;
            _loyalty = 0f;
            _positiveInteractions = 0;
            _negativeInteractions = 0;
            _totalInteractions = 0;
            _firstInteraction = DateTime.Now.ToString("o");
            _lastInteraction = _firstInteraction;
        }
        
        #endregion
        
        #region Internal Methods
        
        internal void SetMetric(RelationshipMetric metric, float value)
        {
            switch (metric)
            {
                case RelationshipMetric.Trust:
                    _trust = Mathf.Clamp(value, -1f, 1f);
                    break;
                case RelationshipMetric.Respect:
                    _respect = Mathf.Clamp(value, -1f, 1f);
                    break;
                case RelationshipMetric.Fear:
                    _fear = Mathf.Clamp01(value);
                    break;
                case RelationshipMetric.Affection:
                    _affection = Mathf.Clamp(value, -1f, 1f);
                    break;
                case RelationshipMetric.Loyalty:
                    _loyalty = Mathf.Clamp01(value);
                    break;
            }
            
            _lastInteraction = DateTime.Now.ToString("o");
        }
        
        internal float GetMetric(RelationshipMetric metric)
        {
            return metric switch
            {
                RelationshipMetric.Trust => _trust,
                RelationshipMetric.Respect => _respect,
                RelationshipMetric.Fear => _fear,
                RelationshipMetric.Affection => _affection,
                RelationshipMetric.Loyalty => _loyalty,
                _ => 0f
            };
        }
        
        internal void RecordInteraction(bool positive)
        {
            _totalInteractions++;
            if (positive)
                _positiveInteractions++;
            else
                _negativeInteractions++;
            
            _lastInteraction = DateTime.Now.ToString("o");
        }
        
        #endregion
    }
    
    #endregion
    
    #region Relationship Status Enum
    
    /// <summary>
    /// Relationship status categories.
    /// </summary>
    public enum RelationshipStatus
    {
        Enemy,
        Hostile,
        Unfriendly,
        Disliked = Unfriendly,
        Neutral,
        Friendly,
        Acquaintance = Friendly,
        Friend,
        BestFriend
    }
    
    #endregion
    
    #region Relationship Tracker Class
    
    /// <summary>
    /// Tracks and manages relationship metrics between the player and an NPC.
    /// Influences NPC behavior, dialogue, and available interactions.
    /// </summary>
    public class RelationshipTracker
    {
        #region Private Fields
        
        private readonly string _npcId;
        private readonly RelationshipData _data;
        private readonly Dictionary<RelationshipMetric, float> _thresholds;
        
        #endregion
        
        #region Properties
        
        public RelationshipData Data => _data;
        public float OverallScore => _data.OverallScore;
        public RelationshipStatus Status => _data.Status;
        public float Trust => _data.Trust;
        public float Respect => _data.Respect;
        public float Fear => _data.Fear;
        public float Affection => _data.Affection;
        public float Loyalty => _data.Loyalty;
        
        #endregion
        
        #region Constructor
        
        public RelationshipTracker(string npcId, RelationshipData existingData = null)
        {
            _npcId = npcId;
            _data = existingData ?? new RelationshipData();
            
            // Default thresholds for triggering events
            _thresholds = new Dictionary<RelationshipMetric, float>
            {
                { RelationshipMetric.Trust, 0.5f },
                { RelationshipMetric.Respect, 0.5f },
                { RelationshipMetric.Fear, 0.5f },
                { RelationshipMetric.Affection, 0.5f },
                { RelationshipMetric.Loyalty, 0.5f }
            };
            
            DebugLogger.LogInfo($"RelationshipTracker created for NPC: {npcId}", LogCategory.Memory);
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Modify a relationship metric by a delta value.
        /// </summary>
        public void ModifyMetric(RelationshipMetric metric, float delta, string reason = "")
        {
            float previousValue = _data.GetMetric(metric);
            float newValue = previousValue + delta;
            
            _data.SetMetric(metric, newValue);
            _data.RecordInteraction(delta > 0);
            
            float actualNewValue = _data.GetMetric(metric);
            
            // Raise change event
            var eventArgs = new RelationshipEventArgs
            {
                NPCId = _npcId,
                Metric = metric,
                PreviousValue = previousValue,
                NewValue = actualNewValue,
                Reason = reason
            };
            
            GameEvents.RaiseRelationshipChanged(eventArgs);
            
            // Check for threshold crossings
            if (_thresholds.TryGetValue(metric, out float threshold))
            {
                bool crossedPositive = previousValue < threshold && actualNewValue >= threshold;
                bool crossedNegative = previousValue >= threshold && actualNewValue < threshold;
                bool crossedNegativeThreshold = previousValue > -threshold && actualNewValue <= -threshold;
                bool crossedPositiveFromNegative = previousValue <= -threshold && actualNewValue > -threshold;
                
                if (crossedPositive || crossedNegative || crossedNegativeThreshold || crossedPositiveFromNegative)
                {
                    GameEvents.RaiseRelationshipThresholdReached(eventArgs);
                    DebugLogger.LogInfo($"Relationship threshold crossed: {metric} = {actualNewValue:F2}", LogCategory.Memory);
                }
            }
            
            DebugLogger.LogVerbose($"Relationship {metric} changed: {previousValue:F2} -> {actualNewValue:F2} ({reason})", LogCategory.Memory);
        }
        
        /// <summary>
        /// Set a relationship metric to a specific value.
        /// </summary>
        public void SetMetric(RelationshipMetric metric, float value, string reason = "")
        {
            float previousValue = _data.GetMetric(metric);
            _data.SetMetric(metric, value);
            float actualNewValue = _data.GetMetric(metric);
            
            if (Mathf.Abs(previousValue - actualNewValue) > 0.01f)
            {
                GameEvents.RaiseRelationshipChanged(new RelationshipEventArgs
                {
                    NPCId = _npcId,
                    Metric = metric,
                    PreviousValue = previousValue,
                    NewValue = actualNewValue,
                    Reason = reason
                });
            }
        }
        
        /// <summary>
        /// Get a relationship metric value.
        /// </summary>
        public float GetMetric(RelationshipMetric metric)
        {
            return _data.GetMetric(metric);
        }
        
        /// <summary>
        /// Apply emotional impact from an interaction.
        /// </summary>
        public void ApplyEmotionalImpact(float impact, string reason = "")
        {
            // Positive impact increases trust, affection; negative decreases them
            float multiplier = impact > 0 ? 1f : 1.5f; // Negative experiences have stronger impact
            
            ModifyMetric(RelationshipMetric.Trust, impact * 0.3f * multiplier, reason);
            ModifyMetric(RelationshipMetric.Affection, impact * 0.4f * multiplier, reason);
            ModifyMetric(RelationshipMetric.Respect, impact * 0.2f * multiplier, reason);
            
            if (impact < -0.5f)
            {
                // Very negative experience might cause fear
                ModifyMetric(RelationshipMetric.Fear, Mathf.Abs(impact) * 0.2f, reason);
            }
        }
        
        /// <summary>
        /// Get price modifier based on relationship status.
        /// Returns a multiplier (e.g., 0.9 for 10% discount, 1.2 for 20% markup).
        /// </summary>
        public float GetPriceModifier()
        {
            return Status switch
            {
                RelationshipStatus.Enemy => 2.0f,      // 100% markup
                RelationshipStatus.Hostile => 1.5f,    // 50% markup
                RelationshipStatus.Unfriendly => 1.2f, // 20% markup
                RelationshipStatus.Neutral => 1.0f,    // No change
                RelationshipStatus.Friendly => 0.95f,  // 5% discount
                RelationshipStatus.Friend => 0.85f,    // 15% discount
                RelationshipStatus.BestFriend => 0.75f,// 25% discount
                _ => 1.0f
            };
        }
        
        /// <summary>
        /// Check if NPC is willing to help player with a task.
        /// </summary>
        public bool IsWillingToHelp(float taskDifficulty = 0.5f)
        {
            // Higher difficulty requires better relationship
            float requiredScore = taskDifficulty - 0.5f;
            return OverallScore >= requiredScore;
        }

        // Backward-compatible wrappers for older call sites/tests
        public void ModifyTrust(float delta, string reason = "") => ModifyMetric(RelationshipMetric.Trust, delta, reason);
        public void ModifyRespect(float delta, string reason = "") => ModifyMetric(RelationshipMetric.Respect, delta, reason);
        public void ModifyFear(float delta, string reason = "") => ModifyMetric(RelationshipMetric.Fear, delta, reason);
        public void ModifyAffection(float delta, string reason = "") => ModifyMetric(RelationshipMetric.Affection, delta, reason);
        public void ModifyLoyalty(float delta, string reason = "") => ModifyMetric(RelationshipMetric.Loyalty, delta, reason);
        public bool CheckWillingToHelp(float taskDifficulty = 0.5f) => IsWillingToHelp(taskDifficulty);
        
        /// <summary>
        /// Get relationship context for LLM prompts.
        /// </summary>
        public string GetContextString()
        {
            string statusDescription = Status switch
            {
                RelationshipStatus.Enemy => "You despise the player. They have wronged you deeply.",
                RelationshipStatus.Hostile => "You dislike the player and are hostile toward them.",
                RelationshipStatus.Unfriendly => "You are unfriendly toward the player. They haven't earned your trust.",
                RelationshipStatus.Neutral => "You have a neutral relationship with the player. You treat them like any other customer/person.",
                RelationshipStatus.Friendly => "You are friendly toward the player. They seem like decent folk.",
                RelationshipStatus.Friend => "You consider the player a friend. You enjoy their company.",
                RelationshipStatus.BestFriend => "The player is your best friend. You trust them completely.",
                _ => "You have no strong feelings about the player."
            };
            
            List<string> details = new List<string>();
            
            if (_data.Trust > 0.5f)
                details.Add("You trust them");
            else if (_data.Trust < -0.5f)
                details.Add("You don't trust them at all");
            
            if (_data.Fear > 0.5f)
                details.Add("You are afraid of them");
            
            if (_data.Affection > 0.5f)
                details.Add("You have genuine affection for them");
            else if (_data.Affection < -0.5f)
                details.Add("You actively dislike them");
            
            if (_data.Respect > 0.5f)
                details.Add("You respect them");
            else if (_data.Respect < -0.5f)
                details.Add("You have no respect for them");
            
            string detailString = details.Count > 0 ? " " + string.Join(", ", details) + "." : "";
            
            return statusDescription + detailString;
        }
        
        /// <summary>
        /// Export relationship data for persistence.
        /// </summary>
        public RelationshipData ExportData()
        {
            return _data;
        }
        
        #endregion
    }
    
    #endregion
}
