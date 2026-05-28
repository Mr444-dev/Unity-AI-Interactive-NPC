using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using InteractiveNPC.Core.Events;
using InteractiveNPC.Core.Utilities;
using InteractiveNPC.Memory;
using InteractiveNPC.Memory.Data;
using InteractiveNPC.Personality;
using InteractiveNPC.Personality.Data;
using InteractiveNPC.LLM.Data;

namespace InteractiveNPC.LLM
{
    #region Prompt Builder Class
    
    /// <summary>
    /// Constructs context-aware prompts for LLM generation.
    /// Injects personality, memories, relationship status, and world context.
    /// </summary>
    public class PromptBuilder
    {
        #region Private Fields
        
        private readonly NPCPersonalityData _personality;
        private readonly MemoryBank _memoryBank;
        private readonly RelationshipTracker _relationship;
        private readonly MoodController _mood;
        private readonly ResponseModifierService _responseModifier;
        
        private string _worldContext;
        private List<LLMMessage> _conversationHistory;
        private int _maxHistoryMessages = 10;
        
        #endregion
        
        #region Properties
        
        public List<LLMMessage> ConversationHistory => _conversationHistory;
        
        #endregion
        
        #region Constructor
        
        public PromptBuilder(
            NPCPersonalityData personality,
            MemoryBank memoryBank,
            RelationshipTracker relationship,
            MoodController mood)
        {
            _personality = personality;
            _memoryBank = memoryBank;
            _relationship = relationship;
            _mood = mood;
            _responseModifier = new ResponseModifierService(personality);
            _conversationHistory = new List<LLMMessage>();
            
            DebugLogger.LogInfo($"PromptBuilder created for: {personality?.DisplayName ?? "Unknown"}", LogCategory.LLM);
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Set the world context for the conversation.
        /// </summary>
        public void SetWorldContext(string context)
        {
            _worldContext = context;
            DebugLogger.LogVerbose($"World context set: {context.Substring(0, Math.Min(100, context.Length))}...", LogCategory.LLM);
        }
        
        /// <summary>
        /// Build a complete LLM request for the given player input.
        /// </summary>
        public LLMRequest BuildRequest(string playerInput, LLMRuntimeSettings settings = null)
        {
            if (string.IsNullOrEmpty(playerInput))
            {
                throw new ArgumentException("Player input cannot be null or empty", nameof(playerInput));
            }
            
            // Analyze player input for sentiment
            var sentiment = _responseModifier.AnalyzePlayerInput(playerInput);
            
            // Apply emotional impact based on sentiment
            if (sentiment.IsInsult)
            {
                _mood?.ApplyEmotionalStimulus(-0.4f, "player insult");
                _relationship?.ApplyEmotionalImpact(-0.3f, "player insult");
            }
            else if (sentiment.IsCompliment)
            {
                _mood?.ApplyEmotionalStimulus(0.3f, "player compliment");
                _relationship?.ApplyEmotionalImpact(0.2f, "player compliment");
            }
            else if (sentiment.IsThreat)
            {
                _mood?.ApplyEmotionalStimulus(-0.6f, "player threat");
                _relationship?.ApplyEmotionalImpact(-0.5f, "player threat");
            }
            
            // Build the system prompt
            string systemPrompt = BuildSystemPrompt(playerInput);
            
            // Add player message to history
            _conversationHistory.Add(LLMMessage.User(playerInput));
            TrimHistory();
            
            // Create request
            LLMRequest request = new LLMRequest
            {
                SystemPrompt = systemPrompt,
                Messages = new List<LLMMessage>(_conversationHistory),
                Settings = settings
            };
            
            DebugLogger.LogInfo($"Built request for input: {playerInput.Substring(0, Math.Min(50, playerInput.Length))}...", LogCategory.LLM);
            
            return request;
        }
        
        /// <summary>
        /// Add an assistant response to the conversation history.
        /// </summary>
        public void AddAssistantResponse(string response)
        {
            if (!string.IsNullOrEmpty(response))
            {
                _conversationHistory.Add(LLMMessage.Assistant(response));
                TrimHistory();
            }
        }
        
        /// <summary>
        /// Clear conversation history (start fresh conversation).
        /// </summary>
        public void ClearHistory()
        {
            _conversationHistory.Clear();
            DebugLogger.LogInfo("Conversation history cleared", LogCategory.LLM);
        }
        
        /// <summary>
        /// Set maximum number of history messages to retain.
        /// </summary>
        public void SetMaxHistoryMessages(int max)
        {
            _maxHistoryMessages = Mathf.Max(2, max);
        }
        
        /// <summary>
        /// Create a memory from the current exchange.
        /// </summary>
        public void CreateMemoryFromExchange(string playerInput, string npcResponse, float importance = 0.5f)
        {
            if (_memoryBank == null) return;
            
            // Analyze the exchange for emotional content
            var sentiment = _responseModifier.AnalyzePlayerInput(playerInput);
            
            MemoryType memoryType = MemoryType.Dialogue;
            float emotionalImpact = sentiment.Sentiment;
            
            if (sentiment.IsInsult)
            {
                memoryType = MemoryType.Insult;
                importance = Mathf.Max(importance, 0.7f); // Insults are memorable
            }
            else if (sentiment.IsCompliment)
            {
                memoryType = MemoryType.Compliment;
                importance = Mathf.Max(importance, 0.5f);
            }
            else if (sentiment.IsThreat)
            {
                memoryType = MemoryType.Action;
                importance = Mathf.Max(importance, 0.8f);
            }
            
            string memoryContent = $"The player said: \"{playerInput}\". I responded: \"{TruncateForMemory(npcResponse)}\"";
            
            MemoryEntry memory = new MemoryEntry(
                memoryType,
                memoryContent,
                emotionalImpact,
                importance
            );
            
            _memoryBank.AddMemory(memory);
        }
        
        #endregion
        
        #region Private Methods
        
        private string BuildSystemPrompt(string currentPlayerInput)
        {
            StringBuilder promptBuilder = new StringBuilder();
            
            // Base personality prompt
            if (_personality != null)
            {
                promptBuilder.AppendLine(_personality.GenerateSystemPrompt());
                promptBuilder.AppendLine();
            }
            
            // World context
            if (!string.IsNullOrEmpty(_worldContext))
            {
                promptBuilder.AppendLine("## World Context");
                promptBuilder.AppendLine(_worldContext);
                promptBuilder.AppendLine();
            }
            
            // Current mood
            if (_mood != null)
            {
                promptBuilder.AppendLine("## Current Emotional State");
                promptBuilder.AppendLine(_mood.GetMoodDescription());
                promptBuilder.AppendLine();
            }
            
            // Relationship with player
            if (_relationship != null)
            {
                promptBuilder.AppendLine("## Relationship with Player");
                promptBuilder.AppendLine(_relationship.GetContextString());
                promptBuilder.AppendLine();
            }
            
            // Relevant memories
            if (_memoryBank != null)
            {
                // Get memories relevant to current input
                var relevantMemories = _memoryBank.RetrieveRelevantMemories(currentPlayerInput, 3);
                
                // Also get important memories
                var importantMemories = _memoryBank.GetImportantMemories(3, 0.6f);
                
                // Get recent memories
                var recentMemories = _memoryBank.GetRecentMemories(2);
                
                StringBuilder memorySection = new StringBuilder();
                
                if (relevantMemories.Count > 0)
                {
                    memorySection.AppendLine("Relevant memories about this topic:");
                    foreach (var memory in relevantMemories)
                    {
                        memorySection.AppendLine($"- {memory.Summary}");
                    }
                }
                
                if (importantMemories.Count > 0)
                {
                    memorySection.AppendLine("\nImportant things you remember:");
                    foreach (var memory in importantMemories)
                    {
                        memorySection.AppendLine($"- [{memory.Type}] {memory.Summary}");
                    }
                }
                
                if (recentMemories.Count > 0)
                {
                    memorySection.AppendLine("\nRecent interactions:");
                    foreach (var memory in recentMemories)
                    {
                        memorySection.AppendLine($"- {memory.Summary}");
                    }
                }
                
                if (memorySection.Length > 0)
                {
                    promptBuilder.AppendLine("## Your Memories");
                    promptBuilder.AppendLine(memorySection.ToString());
                    promptBuilder.AppendLine();
                }
            }
            
            // Response modifiers based on mood and relationship
            string modifierInstructions = _responseModifier.GetPromptAdditions(_mood, _relationship);
            if (!string.IsNullOrEmpty(modifierInstructions))
            {
                promptBuilder.AppendLine("## Response Guidelines");
                promptBuilder.AppendLine(modifierInstructions);
                promptBuilder.AppendLine();
            }
            
            // Response format guidelines
            promptBuilder.AppendLine("## Response Format");
            promptBuilder.AppendLine("- Respond in character as yourself (the NPC), not as an AI.");
            promptBuilder.AppendLine("- Keep responses concise but natural (1-3 sentences typically).");
            promptBuilder.AppendLine("- You may use *asterisks* for actions or emotions.");
            promptBuilder.AppendLine("- Do not break character or mention that you are an AI.");
            promptBuilder.AppendLine("- React naturally based on your memories and current mood.");
            
            return promptBuilder.ToString();
        }
        
        private void TrimHistory()
        {
            while (_conversationHistory.Count > _maxHistoryMessages)
            {
                _conversationHistory.RemoveAt(0);
            }
        }
        
        private string TruncateForMemory(string text, int maxLength = 150)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;
            
            return text.Substring(0, maxLength) + "...";
        }
        
        #endregion
    }
    
    #endregion
}
