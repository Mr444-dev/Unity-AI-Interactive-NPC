using System;
using System.Collections.Generic;
using UnityEngine;
using InteractiveNPC.Core.Events;

namespace InteractiveNPC.Memory.Data
{
    #region Memory Entry Class
    
    /// <summary>
    /// Represents a single memory entry stored by an NPC.
    /// Contains the content, emotional impact, and metadata for the memory.
    /// </summary>
    [Serializable]
    public class MemoryEntry
    {
        #region Serialized Fields
        
        [SerializeField, Tooltip("Unique identifier for this memory")]
        private string _id;
        
        [SerializeField, Tooltip("Type of memory")]
        private MemoryType _type;
        
        [SerializeField, Tooltip("Content/description of the memory")]
        private string _content;
        
        [SerializeField, Tooltip("Summary for LLM context (shorter version)")]
        private string _summary;
        
        [SerializeField, Tooltip("Emotional impact of this memory (-1 to 1)")]
        [Range(-1f, 1f)]
        private float _emotionalImpact;
        
        [SerializeField, Tooltip("Importance score (0 to 1, affects retention)")]
        [Range(0f, 1f)]
        private float _importance;
        
        [SerializeField, Tooltip("Number of times this memory has been recalled")]
        private int _recallCount;
        
        [SerializeField, Tooltip("Timestamp when the memory was created")]
        private string _createdAt;
        
        [SerializeField, Tooltip("Timestamp when the memory was last accessed")]
        private string _lastAccessedAt;
        
        [SerializeField, Tooltip("Tags for categorizing and searching memories")]
        private List<string> _tags;
        
        [SerializeField, Tooltip("Associated NPC or player name")]
        private string _associatedEntity;
        
        #endregion
        
        #region Properties
        
        public string Id => _id;
        public MemoryType Type => _type;
        public string Content => _content;
        public string Summary => _summary;
        public float EmotionalImpact => _emotionalImpact;
        public float Importance => _importance;
        public int RecallCount => _recallCount;
        public DateTime CreatedAt => DateTime.TryParse(_createdAt, out DateTime dt) ? dt : DateTime.Now;
        public DateTime LastAccessedAt => DateTime.TryParse(_lastAccessedAt, out DateTime dt) ? dt : DateTime.Now;
        public List<string> Tags => _tags;
        public string AssociatedEntity => _associatedEntity;
        
        /// <summary>
        /// Calculate memory strength based on importance, recency, and recall count.
        /// </summary>
        public float Strength
        {
            get
            {
                float recencyFactor = Mathf.Clamp01(1f - (float)(DateTime.Now - LastAccessedAt).TotalDays / 30f);
                float recallFactor = Mathf.Clamp01(_recallCount / 10f);
                return (_importance * 0.4f + recencyFactor * 0.3f + recallFactor * 0.3f);
            }
        }
        
        #endregion
        
        #region Constructors
        
        public MemoryEntry()
        {
            _id = Guid.NewGuid().ToString();
            _tags = new List<string>();
            _createdAt = DateTime.Now.ToString("o");
            _lastAccessedAt = _createdAt;
        }
        
        public MemoryEntry(MemoryType type, string content, float emotionalImpact, float importance, string associatedEntity = "Player")
            : this()
        {
            _type = type;
            _content = content;
            _summary = GenerateSummary(content);
            _emotionalImpact = Mathf.Clamp(emotionalImpact, -1f, 1f);
            _importance = Mathf.Clamp01(importance);
            _associatedEntity = associatedEntity;
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Mark this memory as recalled, updating access time and recall count.
        /// </summary>
        public void MarkRecalled()
        {
            _recallCount++;
            _lastAccessedAt = DateTime.Now.ToString("o");
        }

        // Backward-compatible alias
        public void Recall()
        {
            MarkRecalled();
        }
        
        /// <summary>
        /// Add a tag to this memory.
        /// </summary>
        public void AddTag(string tag)
        {
            if (!_tags.Contains(tag))
            {
                _tags.Add(tag);
            }
        }
        
        /// <summary>
        /// Update the importance of this memory.
        /// </summary>
        public void UpdateImportance(float newImportance)
        {
            _importance = Mathf.Clamp01(newImportance);
        }
        
        /// <summary>
        /// Check if this memory should be forgotten based on decay.
        /// </summary>
        public bool ShouldDecay(float decayThreshold = 0.1f)
        {
            return Strength < decayThreshold && _importance < 0.5f;
        }
        
        #endregion
        
        #region Private Methods
        
        private string GenerateSummary(string content)
        {
            if (string.IsNullOrEmpty(content)) return "";
            
            // Simple summary: first 100 characters or first sentence
            int endIndex = content.IndexOf('.');
            if (endIndex > 0 && endIndex < 100)
            {
                return content.Substring(0, endIndex + 1);
            }
            return content.Length > 100 ? content.Substring(0, 100) + "..." : content;
        }
        
        #endregion
    }
    
    #endregion
    
    #region Memory Collection Class
    
    /// <summary>
    /// Container for short-term and long-term memories.
    /// </summary>
    [Serializable]
    public class MemoryCollection
    {
        [SerializeField]
        private List<MemoryEntry> _shortTermMemories = new List<MemoryEntry>();
        
        [SerializeField]
        private List<MemoryEntry> _longTermMemories = new List<MemoryEntry>();
        
        public List<MemoryEntry> ShortTermMemories => _shortTermMemories;
        public List<MemoryEntry> LongTermMemories => _longTermMemories;
        
        public int TotalCount => _shortTermMemories.Count + _longTermMemories.Count;
    }
    
    #endregion
    
    #region NPC Memory Data ScriptableObject
    
    /// <summary>
    /// ScriptableObject for defining initial memories and memory configuration for an NPC.
    /// Used to pre-populate NPC memories with backstory and important events.
    /// </summary>
    [CreateAssetMenu(fileName = "NPCMemoryData", menuName = "Interactive NPC/Memory/NPC Memory Data")]
    public class NPCMemoryData : ScriptableObject
    {
        #region Serialized Fields
        
        [Header("Memory Configuration")]
        [SerializeField, Tooltip("Maximum number of short-term memories")]
        private int _maxShortTermMemories = 10;
        
        [SerializeField, Tooltip("Maximum number of long-term memories")]
        private int _maxLongTermMemories = 100;
        
        [SerializeField, Tooltip("Importance threshold for consolidating to long-term memory")]
        [Range(0f, 1f)]
        private float _consolidationThreshold = 0.5f;
        
        [SerializeField, Tooltip("Decay threshold for forgetting memories")]
        [Range(0f, 0.5f)]
        private float _decayThreshold = 0.1f;
        
        [SerializeField, Tooltip("How often to run memory consolidation (in game minutes)")]
        private float _consolidationIntervalMinutes = 5f;
        
        [Header("Initial Memories")]
        [SerializeField, Tooltip("Pre-defined memories for this NPC's backstory")]
        private List<MemoryEntry> _initialMemories = new List<MemoryEntry>();
        
        [Header("Memory Templates")]
        [SerializeField, Tooltip("Templates for generating memories from events")]
        private List<MemoryTemplate> _memoryTemplates = new List<MemoryTemplate>();
        
        #endregion
        
        #region Properties
        
        public int MaxShortTermMemories => _maxShortTermMemories;
        public int MaxLongTermMemories => _maxLongTermMemories;
        public float ConsolidationThreshold => _consolidationThreshold;
        public float DecayThreshold => _decayThreshold;
        public float ConsolidationIntervalMinutes => _consolidationIntervalMinutes;
        public IReadOnlyList<MemoryEntry> InitialMemories => _initialMemories;
        public IReadOnlyList<MemoryTemplate> MemoryTemplates => _memoryTemplates;
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Create a copy of initial memories for a new NPC instance.
        /// </summary>
        public List<MemoryEntry> CreateInitialMemoriesCopy()
        {
            List<MemoryEntry> copies = new List<MemoryEntry>();
            
            foreach (var memory in _initialMemories)
            {
                copies.Add(new MemoryEntry(
                    memory.Type,
                    memory.Content,
                    memory.EmotionalImpact,
                    memory.Importance,
                    memory.AssociatedEntity
                ));
            }
            
            return copies;
        }
        
        /// <summary>
        /// Get a memory template by type.
        /// </summary>
        public MemoryTemplate GetTemplate(MemoryType type)
        {
            return _memoryTemplates.Find(t => t.Type == type);
        }
        
        #endregion
    }
    
    #endregion
    
    #region Memory Template Class
    
    /// <summary>
    /// Template for generating memories from game events.
    /// </summary>
    [Serializable]
    public class MemoryTemplate
    {
        [SerializeField, Tooltip("Type of memory this template creates")]
        private MemoryType _type;
        
        [SerializeField, Tooltip("Template string with placeholders like {player}, {item}, {amount}")]
        private string _contentTemplate;
        
        [SerializeField, Tooltip("Default emotional impact for this type")]
        [Range(-1f, 1f)]
        private float _defaultEmotionalImpact;
        
        [SerializeField, Tooltip("Default importance for this type")]
        [Range(0f, 1f)]
        private float _defaultImportance;
        
        [SerializeField, Tooltip("Tags automatically added to memories of this type")]
        private List<string> _defaultTags = new List<string>();
        
        public MemoryType Type => _type;
        public string ContentTemplate => _contentTemplate;
        public float DefaultEmotionalImpact => _defaultEmotionalImpact;
        public float DefaultImportance => _defaultImportance;
        public IReadOnlyList<string> DefaultTags => _defaultTags;
        
        /// <summary>
        /// Create a memory from this template with variable substitution.
        /// </summary>
        public MemoryEntry CreateMemory(Dictionary<string, string> variables, float? emotionalImpactOverride = null, float? importanceOverride = null)
        {
            string content = _contentTemplate;
            
            foreach (var kvp in variables)
            {
                content = content.Replace($"{{{kvp.Key}}}", kvp.Value);
            }
            
            MemoryEntry memory = new MemoryEntry(
                _type,
                content,
                emotionalImpactOverride ?? _defaultEmotionalImpact,
                importanceOverride ?? _defaultImportance
            );
            
            foreach (string tag in _defaultTags)
            {
                memory.AddTag(tag);
            }
            
            return memory;
        }
    }
    
    #endregion
}
