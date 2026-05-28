using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using InteractiveNPC.Core.Events;
using InteractiveNPC.Core.Utilities;
using InteractiveNPC.Core.Services;
using InteractiveNPC.Memory.Data;

namespace InteractiveNPC.Memory
{
    #region Memory Bank Interface
    
    /// <summary>
    /// Interface for memory bank implementations.
    /// </summary>
    public interface IMemoryBank : IService
    {
        /// <summary>
        /// Add a new memory to the bank.
        /// </summary>
        void AddMemory(MemoryEntry memory);
        
        /// <summary>
        /// Retrieve memories relevant to a query.
        /// </summary>
        List<MemoryEntry> RetrieveRelevantMemories(string query, int maxResults = 5);
        
        /// <summary>
        /// Get all memories of a specific type.
        /// </summary>
        List<MemoryEntry> GetMemoriesByType(MemoryType type);
        
        /// <summary>
        /// Get conversation context for LLM prompts.
        /// </summary>
        string GetContextSummary(int maxTokens = 500);
        
        /// <summary>
        /// Run memory consolidation (short-term to long-term).
        /// </summary>
        void ConsolidateMemories();
        
        /// <summary>
        /// Clear all memories.
        /// </summary>
        void Clear();
        
        /// <summary>
        /// Get total memory count.
        /// </summary>
        int TotalMemoryCount { get; }
    }
    
    #endregion
    
    #region Memory Bank Implementation
    
    /// <summary>
    /// Core memory management system for NPCs.
    /// Handles short-term and long-term memory storage, retrieval, and consolidation.
    /// </summary>
    public class MemoryBank : IMemoryBank
    {
        #region Private Fields
        
        private readonly string _npcId;
        private readonly NPCMemoryData _config;
        private readonly MemoryCollection _memories;
        private readonly object _lockObject = new object();
        
        #endregion
        
        #region Properties
        
        public int TotalMemoryCount => _memories.TotalCount;
        public int ShortTermCount => _memories.ShortTermMemories.Count;
        public int LongTermCount => _memories.LongTermMemories.Count;
        
        #endregion
        
        #region Constructor
        
        public MemoryBank(string npcId, NPCMemoryData config)
        {
            _npcId = npcId;
            _config = config;
            _memories = new MemoryCollection();
            
            // Load initial memories from config
            if (config != null)
            {
                List<MemoryEntry> initialMemories = config.CreateInitialMemoriesCopy();
                foreach (var memory in initialMemories)
                {
                    _memories.LongTermMemories.Add(memory);
                }
            }
            
            DebugLogger.LogInfo($"MemoryBank created for NPC: {npcId} with {LongTermCount} initial memories", LogCategory.Memory);
        }
        
        #endregion
        
        #region IService Implementation
        
        public void Initialize()
        {
            DebugLogger.LogInfo($"MemoryBank initialized for {_npcId}", LogCategory.Memory);
        }
        
        public void Shutdown()
        {
            DebugLogger.LogInfo($"MemoryBank shutdown for {_npcId}", LogCategory.Memory);
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Add a new memory to the short-term memory buffer.
        /// </summary>
        public void AddMemory(MemoryEntry memory)
        {
            if (memory == null)
            {
                DebugLogger.LogWarning("Attempted to add null memory", LogCategory.Memory);
                return;
            }
            
            lock (_lockObject)
            {
                _memories.ShortTermMemories.Add(memory);
                
                // Enforce short-term memory limit
                while (_memories.ShortTermMemories.Count > (_config?.MaxShortTermMemories ?? 10))
                {
                    // Remove the oldest, lowest importance memory
                    var toRemove = _memories.ShortTermMemories
                        .OrderBy(m => m.Strength)
                        .First();
                    _memories.ShortTermMemories.Remove(toRemove);
                    
                    DebugLogger.LogVerbose($"Short-term memory overflow, removed: {toRemove.Summary}", LogCategory.Memory);
                }
            }
            
            // Raise event
            GameEvents.RaiseMemoryCreated(new MemoryEventArgs
            {
                NPCId = _npcId,
                MemoryId = memory.Id,
                MemoryContent = memory.Content,
                Type = memory.Type,
                Importance = memory.Importance
            });
            
            DebugLogger.LogInfo($"Memory added: [{memory.Type}] {memory.Summary}", LogCategory.Memory);
        }
        
        /// <summary>
        /// Create and add a memory from a template.
        /// </summary>
        public void AddMemoryFromTemplate(MemoryType type, Dictionary<string, string> variables, 
            float? emotionalImpactOverride = null, float? importanceOverride = null)
        {
            if (_config == null)
            {
                DebugLogger.LogWarning("Cannot create memory from template: config is null", LogCategory.Memory);
                return;
            }
            
            MemoryTemplate template = _config.GetTemplate(type);
            if (template == null)
            {
                DebugLogger.LogWarning($"No template found for memory type: {type}", LogCategory.Memory);
                return;
            }
            
            MemoryEntry memory = template.CreateMemory(variables, emotionalImpactOverride, importanceOverride);
            AddMemory(memory);
        }
        
        /// <summary>
        /// Retrieve memories most relevant to the given query using keyword matching.
        /// </summary>
        public List<MemoryEntry> RetrieveRelevantMemories(string query, int maxResults = 5)
        {
            if (string.IsNullOrEmpty(query))
            {
                return new List<MemoryEntry>();
            }
            
            string[] keywords = query.ToLower().Split(new[] { ' ', ',', '.', '!', '?' }, 
                StringSplitOptions.RemoveEmptyEntries);
            
            lock (_lockObject)
            {
                var allMemories = _memories.ShortTermMemories
                    .Concat(_memories.LongTermMemories)
                    .ToList();
                
                // Score each memory based on keyword matches and strength
                var scored = allMemories
                    .Select(m => new
                    {
                        Memory = m,
                        Score = CalculateRelevanceScore(m, keywords)
                    })
                    .Where(x => x.Score > 0)
                    .OrderByDescending(x => x.Score)
                    .Take(maxResults)
                    .ToList();
                
                // Mark retrieved memories as recalled
                foreach (var item in scored)
                {
                    item.Memory.MarkRecalled();
                    
                    GameEvents.RaiseMemoryRetrieved(new MemoryEventArgs
                    {
                        NPCId = _npcId,
                        MemoryId = item.Memory.Id,
                        MemoryContent = item.Memory.Content,
                        Type = item.Memory.Type,
                        Importance = item.Memory.Importance
                    });
                }
                
                return scored.Select(x => x.Memory).ToList();
            }
        }
        
        /// <summary>
        /// Get all memories of a specific type.
        /// </summary>
        public List<MemoryEntry> GetMemoriesByType(MemoryType type)
        {
            lock (_lockObject)
            {
                return _memories.ShortTermMemories
                    .Concat(_memories.LongTermMemories)
                    .Where(m => m.Type == type)
                    .OrderByDescending(m => m.Strength)
                    .ToList();
            }
        }
        
        /// <summary>
        /// Get recent memories for conversation context.
        /// </summary>
        public List<MemoryEntry> GetRecentMemories(int count = 5)
        {
            lock (_lockObject)
            {
                return _memories.ShortTermMemories
                    .OrderByDescending(m => m.LastAccessedAt)
                    .Take(count)
                    .ToList();
            }
        }
        
        /// <summary>
        /// Get important long-term memories.
        /// </summary>
        public List<MemoryEntry> GetImportantMemories(int count = 5, float minImportance = 0.5f)
        {
            lock (_lockObject)
            {
                return _memories.LongTermMemories
                    .Where(m => m.Importance >= minImportance)
                    .OrderByDescending(m => m.Strength)
                    .Take(count)
                    .ToList();
            }
        }
        
        /// <summary>
        /// Generate a summary of relevant memories for LLM context.
        /// </summary>
        public string GetContextSummary(int maxTokens = 500)
        {
            List<string> summaries = new List<string>();
            int estimatedTokens = 0;
            
            lock (_lockObject)
            {
                // Add important long-term memories first
                var importantMemories = _memories.LongTermMemories
                    .Where(m => m.Importance >= 0.7f)
                    .OrderByDescending(m => m.Strength)
                    .Take(5);
                
                foreach (var memory in importantMemories)
                {
                    string entry = $"- [{memory.Type}] {memory.Summary}";
                    int tokenEstimate = entry.Length / 4; // Rough token estimate
                    
                    if (estimatedTokens + tokenEstimate > maxTokens) break;
                    
                    summaries.Add(entry);
                    estimatedTokens += tokenEstimate;
                }
                
                // Add recent short-term memories
                var recentMemories = _memories.ShortTermMemories
                    .OrderByDescending(m => m.LastAccessedAt)
                    .Take(5);
                
                foreach (var memory in recentMemories)
                {
                    string entry = $"- [Recent] {memory.Summary}";
                    int tokenEstimate = entry.Length / 4;
                    
                    if (estimatedTokens + tokenEstimate > maxTokens) break;
                    
                    summaries.Add(entry);
                    estimatedTokens += tokenEstimate;
                }
            }
            
            if (summaries.Count == 0)
            {
                return "No significant memories.";
            }
            
            return "Memories:\n" + string.Join("\n", summaries);
        }
        
        /// <summary>
        /// Consolidate short-term memories to long-term based on importance.
        /// </summary>
        public void ConsolidateMemories()
        {
            float threshold = _config?.ConsolidationThreshold ?? 0.5f;
            int consolidated = 0;
            int forgotten = 0;
            
            lock (_lockObject)
            {
                var toConsolidate = _memories.ShortTermMemories
                    .Where(m => m.Importance >= threshold)
                    .ToList();
                
                foreach (var memory in toConsolidate)
                {
                    _memories.ShortTermMemories.Remove(memory);
                    _memories.LongTermMemories.Add(memory);
                    consolidated++;
                }
                
                // Remove decayed long-term memories
                float decayThreshold = _config?.DecayThreshold ?? 0.1f;
                var toForget = _memories.LongTermMemories
                    .Where(m => m.ShouldDecay(decayThreshold))
                    .ToList();
                
                foreach (var memory in toForget)
                {
                    _memories.LongTermMemories.Remove(memory);
                    forgotten++;
                    
                    GameEvents.RaiseMemoryForgotten(new MemoryEventArgs
                    {
                        NPCId = _npcId,
                        MemoryId = memory.Id,
                        MemoryContent = memory.Content,
                        Type = memory.Type,
                        Importance = memory.Importance
                    });
                }
                
                // Enforce long-term memory limit
                int maxLongTerm = _config?.MaxLongTermMemories ?? 100;
                while (_memories.LongTermMemories.Count > maxLongTerm)
                {
                    var weakest = _memories.LongTermMemories
                        .OrderBy(m => m.Strength)
                        .First();
                    _memories.LongTermMemories.Remove(weakest);
                    forgotten++;
                }
            }
            
            if (consolidated > 0 || forgotten > 0)
            {
                DebugLogger.LogInfo($"Memory consolidation: {consolidated} consolidated, {forgotten} forgotten", LogCategory.Memory);
                GameEvents.RaiseMemoriesConsolidated(_npcId);
            }
        }
        
        /// <summary>
        /// Clear all memories.
        /// </summary>
        public void Clear()
        {
            lock (_lockObject)
            {
                _memories.ShortTermMemories.Clear();
                _memories.LongTermMemories.Clear();
            }
            
            DebugLogger.LogInfo($"All memories cleared for {_npcId}", LogCategory.Memory);
        }
        
        /// <summary>
        /// Export memories for persistence.
        /// </summary>
        public MemoryCollection ExportMemories()
        {
            lock (_lockObject)
            {
                return _memories;
            }
        }
        
        /// <summary>
        /// Import memories from persistence.
        /// </summary>
        public void ImportMemories(MemoryCollection collection)
        {
            if (collection == null) return;
            
            lock (_lockObject)
            {
                _memories.ShortTermMemories.Clear();
                _memories.LongTermMemories.Clear();
                
                _memories.ShortTermMemories.AddRange(collection.ShortTermMemories);
                _memories.LongTermMemories.AddRange(collection.LongTermMemories);
            }
            
            DebugLogger.LogInfo($"Imported {collection.TotalCount} memories for {_npcId}", LogCategory.Memory);
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Calculate relevance score for a memory based on keyword matching.
        /// </summary>
        private float CalculateRelevanceScore(MemoryEntry memory, string[] keywords)
        {
            string content = memory.Content.ToLower();
            string summary = memory.Summary.ToLower();
            
            int matches = 0;
            foreach (string keyword in keywords)
            {
                if (keyword.Length < 3) continue; // Skip short words
                
                if (content.Contains(keyword) || summary.Contains(keyword))
                {
                    matches++;
                }
                
                // Check tags
                if (memory.Tags.Any(t => t.ToLower().Contains(keyword)))
                {
                    matches++;
                }
            }
            
            if (matches == 0) return 0f;
            
            // Combine keyword matches with memory strength
            float matchScore = (float)matches / keywords.Length;
            return matchScore * 0.6f + memory.Strength * 0.4f;
        }
        
        #endregion
    }
    
    #endregion
}
