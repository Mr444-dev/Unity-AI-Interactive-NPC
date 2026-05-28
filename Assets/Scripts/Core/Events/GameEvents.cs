using System;
using UnityEngine;
using UnityEngine.Events;

namespace InteractiveNPC.Core.Events
{
    #region Event Argument Classes
    
    /// <summary>
    /// Base class for all event arguments in the system.
    /// </summary>
    public abstract class GameEventArgs
    {
        public DateTime Timestamp { get; } = DateTime.Now;
    }
    
    /// <summary>
    /// Event arguments for conversation-related events.
    /// </summary>
    public class ConversationEventArgs : GameEventArgs
    {
        public string NPCId { get; set; }
        public string PlayerInput { get; set; }
        public string NPCResponse { get; set; }
        public float EmotionalImpact { get; set; }
    }
    
    /// <summary>
    /// Event arguments for memory-related events.
    /// </summary>
    public class MemoryEventArgs : GameEventArgs
    {
        public string NPCId { get; set; }
        public string MemoryId { get; set; }
        public string MemoryContent { get; set; }
        public MemoryType Type { get; set; }
        public float Importance { get; set; }
    }
    
    /// <summary>
    /// Event arguments for mood/emotion changes.
    /// </summary>
    public class MoodEventArgs : GameEventArgs
    {
        public string NPCId { get; set; }
        public EmotionalState PreviousMood { get; set; }
        public EmotionalState NewMood { get; set; }
        public float Intensity { get; set; }
    }
    
    /// <summary>
    /// Event arguments for relationship changes.
    /// </summary>
    public class RelationshipEventArgs : GameEventArgs
    {
        public string NPCId { get; set; }
        public RelationshipMetric Metric { get; set; }
        public float PreviousValue { get; set; }
        public float NewValue { get; set; }
        public string Reason { get; set; }
    }
    
    /// <summary>
    /// Event arguments for speech recognition events.
    /// </summary>
    public class SpeechRecognitionEventArgs : GameEventArgs
    {
        public string RecognizedText { get; set; }
        public float Confidence { get; set; }
        public bool IsFinal { get; set; }
    }
    
    /// <summary>
    /// Event arguments for text-to-speech events.
    /// </summary>
    public class TTSEventArgs : GameEventArgs
    {
        public string Text { get; set; }
        public AudioClip AudioClip { get; set; }
        public float Duration { get; set; }
    }
    
    #endregion
    
    #region Enums
    
    /// <summary>
    /// Types of memories that can be stored.
    /// </summary>
    public enum MemoryType
    {
        Dialogue,
        Event,
        Action,
        Gift,
        Insult,
        Compliment,
        Transaction,
        Quest,
        Combat
    }
    
    /// <summary>
    /// Emotional states for NPCs.
    /// </summary>
    public enum EmotionalState
    {
        Neutral,
        Happy,
        Angry,
        Sad,
        Fearful,
        Surprised,
        Disgusted,
        Contemptuous
    }
    
    /// <summary>
    /// Relationship metrics between player and NPC.
    /// </summary>
    public enum RelationshipMetric
    {
        Trust,
        Respect,
        Fear,
        Affection,
        Loyalty
    }
    
    #endregion
    
    #region Game Events Static Class
    
    /// <summary>
    /// Central event hub for the Interactive NPC system.
    /// Provides decoupled communication between all systems using C# events.
    /// </summary>
    public static class GameEvents
    {
        #region Conversation Events
        
        /// <summary>
        /// Fired when a conversation with an NPC begins.
        /// </summary>
        public static event Action<ConversationEventArgs> OnConversationStarted;
        
        /// <summary>
        /// Fired when a conversation with an NPC ends.
        /// </summary>
        public static event Action<ConversationEventArgs> OnConversationEnded;
        
        /// <summary>
        /// Fired when the player sends a message to an NPC.
        /// </summary>
        public static event Action<ConversationEventArgs> OnPlayerMessageSent;
        
        /// <summary>
        /// Fired when an NPC responds to the player.
        /// </summary>
        public static event Action<ConversationEventArgs> OnNPCResponseReceived;
        
        /// <summary>
        /// Fired when the NPC starts processing a response.
        /// </summary>
        public static event Action<string> OnNPCProcessingStarted;
        
        /// <summary>
        /// Fired when the NPC finishes processing a response.
        /// </summary>
        public static event Action<string> OnNPCProcessingCompleted;

        /// <summary>
        /// Fired when the player enters an NPC interaction range.
        /// </summary>
        public static event Action<string> OnPlayerEnteredNPCRange;

        /// <summary>
        /// Fired when the player exits an NPC interaction range.
        /// </summary>
        public static event Action<string> OnPlayerExitedNPCRange;
        
        #endregion
        
        #region Memory Events
        
        /// <summary>
        /// Fired when a new memory is created.
        /// </summary>
        public static event Action<MemoryEventArgs> OnMemoryCreated;
        
        /// <summary>
        /// Fired when a memory is retrieved.
        /// </summary>
        public static event Action<MemoryEventArgs> OnMemoryRetrieved;
        
        /// <summary>
        /// Fired when a memory decays or is forgotten.
        /// </summary>
        public static event Action<MemoryEventArgs> OnMemoryForgotten;
        
        /// <summary>
        /// Fired when memories are consolidated (short-term to long-term).
        /// </summary>
        public static event Action<string> OnMemoriesConsolidated;
        
        #endregion
        
        #region Mood Events
        
        /// <summary>
        /// Fired when an NPC's mood changes.
        /// </summary>
        public static event Action<MoodEventArgs> OnMoodChanged;
        
        /// <summary>
        /// Fired when an NPC's mood intensity updates.
        /// </summary>
        public static event Action<MoodEventArgs> OnMoodIntensityChanged;
        
        #endregion
        
        #region Relationship Events
        
        /// <summary>
        /// Fired when a relationship metric changes.
        /// </summary>
        public static event Action<RelationshipEventArgs> OnRelationshipChanged;
        
        /// <summary>
        /// Fired when a relationship reaches a threshold (e.g., enemy, friend, ally).
        /// </summary>
        public static event Action<RelationshipEventArgs> OnRelationshipThresholdReached;
        
        #endregion
        
        #region Speech Events
        
        /// <summary>
        /// Fired when speech recognition starts.
        /// </summary>
        public static event Action OnSpeechRecognitionStarted;
        
        /// <summary>
        /// Fired when speech is recognized (interim or final).
        /// </summary>
        public static event Action<SpeechRecognitionEventArgs> OnSpeechRecognized;
        
        /// <summary>
        /// Fired when speech recognition ends.
        /// </summary>
        public static event Action OnSpeechRecognitionEnded;
        
        /// <summary>
        /// Fired when TTS audio starts playing.
        /// </summary>
        public static event Action<TTSEventArgs> OnTTSStarted;
        
        /// <summary>
        /// Fired when TTS audio finishes playing.
        /// </summary>
        public static event Action<TTSEventArgs> OnTTSCompleted;
        
        #endregion
        
        #region LLM Events
        
        /// <summary>
        /// Fired when an LLM request is sent.
        /// </summary>
        public static event Action<string> OnLLMRequestSent;
        
        /// <summary>
        /// Fired when an LLM response is received (streaming chunk).
        /// </summary>
        public static event Action<string> OnLLMStreamChunkReceived;
        
        /// <summary>
        /// Fired when an LLM response is complete.
        /// </summary>
        public static event Action<string> OnLLMResponseComplete;
        
        /// <summary>
        /// Fired when an LLM request fails.
        /// </summary>
        public static event Action<Exception> OnLLMRequestFailed;
        
        #endregion
        
        #region Event Invocation Methods
        
        // Conversation
        public static void RaiseConversationStarted(ConversationEventArgs args) => OnConversationStarted?.Invoke(args);
        public static void RaiseConversationEnded(ConversationEventArgs args) => OnConversationEnded?.Invoke(args);
        public static void RaisePlayerMessageSent(ConversationEventArgs args) => OnPlayerMessageSent?.Invoke(args);
        public static void RaiseNPCResponseReceived(ConversationEventArgs args) => OnNPCResponseReceived?.Invoke(args);
        public static void RaiseNPCProcessingStarted(string npcId) => OnNPCProcessingStarted?.Invoke(npcId);
        public static void RaiseNPCProcessingCompleted(string npcId) => OnNPCProcessingCompleted?.Invoke(npcId);
        public static void RaisePlayerEnteredNPCRange(string npcId) => OnPlayerEnteredNPCRange?.Invoke(npcId);
        public static void RaisePlayerExitedNPCRange(string npcId) => OnPlayerExitedNPCRange?.Invoke(npcId);
        
        // Memory
        public static void RaiseMemoryCreated(MemoryEventArgs args) => OnMemoryCreated?.Invoke(args);
        public static void RaiseMemoryRetrieved(MemoryEventArgs args) => OnMemoryRetrieved?.Invoke(args);
        public static void RaiseMemoryForgotten(MemoryEventArgs args) => OnMemoryForgotten?.Invoke(args);
        public static void RaiseMemoriesConsolidated(string npcId) => OnMemoriesConsolidated?.Invoke(npcId);
        
        // Mood
        public static void RaiseMoodChanged(MoodEventArgs args) => OnMoodChanged?.Invoke(args);
        public static void RaiseMoodIntensityChanged(MoodEventArgs args) => OnMoodIntensityChanged?.Invoke(args);
        
        // Relationship
        public static void RaiseRelationshipChanged(RelationshipEventArgs args) => OnRelationshipChanged?.Invoke(args);
        public static void RaiseRelationshipThresholdReached(RelationshipEventArgs args) => OnRelationshipThresholdReached?.Invoke(args);
        
        // Speech
        public static void RaiseSpeechRecognitionStarted() => OnSpeechRecognitionStarted?.Invoke();
        public static void RaiseSpeechRecognized(SpeechRecognitionEventArgs args) => OnSpeechRecognized?.Invoke(args);
        public static void RaiseSpeechRecognitionEnded() => OnSpeechRecognitionEnded?.Invoke();
        public static void RaiseTTSStarted(TTSEventArgs args) => OnTTSStarted?.Invoke(args);
        public static void RaiseTTSCompleted(TTSEventArgs args) => OnTTSCompleted?.Invoke(args);
        
        // LLM
        public static void RaiseLLMRequestSent(string prompt) => OnLLMRequestSent?.Invoke(prompt);
        public static void RaiseLLMStreamChunkReceived(string chunk) => OnLLMStreamChunkReceived?.Invoke(chunk);
        public static void RaiseLLMResponseComplete(string response) => OnLLMResponseComplete?.Invoke(response);
        public static void RaiseLLMRequestFailed(Exception exception) => OnLLMRequestFailed?.Invoke(exception);
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Clear all event subscriptions. Useful for cleanup during scene transitions.
        /// </summary>
        public static void ClearAllSubscriptions()
        {
            OnConversationStarted = null;
            OnConversationEnded = null;
            OnPlayerMessageSent = null;
            OnNPCResponseReceived = null;
            OnNPCProcessingStarted = null;
            OnNPCProcessingCompleted = null;
            OnPlayerEnteredNPCRange = null;
            OnPlayerExitedNPCRange = null;
            
            OnMemoryCreated = null;
            OnMemoryRetrieved = null;
            OnMemoryForgotten = null;
            OnMemoriesConsolidated = null;
            
            OnMoodChanged = null;
            OnMoodIntensityChanged = null;
            
            OnRelationshipChanged = null;
            OnRelationshipThresholdReached = null;
            
            OnSpeechRecognitionStarted = null;
            OnSpeechRecognized = null;
            OnSpeechRecognitionEnded = null;
            OnTTSStarted = null;
            OnTTSCompleted = null;
            
            OnLLMRequestSent = null;
            OnLLMStreamChunkReceived = null;
            OnLLMResponseComplete = null;
            OnLLMRequestFailed = null;
        }
        
        #endregion
    }
    
    #endregion
    
    #region Unity Event Wrappers
    
    /// <summary>
    /// Serializable UnityEvent for conversation events (for Inspector binding).
    /// </summary>
    [Serializable]
    public class ConversationUnityEvent : UnityEvent<ConversationEventArgs> { }
    
    /// <summary>
    /// Serializable UnityEvent for memory events.
    /// </summary>
    [Serializable]
    public class MemoryUnityEvent : UnityEvent<MemoryEventArgs> { }
    
    /// <summary>
    /// Serializable UnityEvent for mood events.
    /// </summary>
    [Serializable]
    public class MoodUnityEvent : UnityEvent<MoodEventArgs> { }
    
    /// <summary>
    /// Serializable UnityEvent for relationship events.
    /// </summary>
    [Serializable]
    public class RelationshipUnityEvent : UnityEvent<RelationshipEventArgs> { }
    
    /// <summary>
    /// Serializable UnityEvent for string messages.
    /// </summary>
    [Serializable]
    public class StringUnityEvent : UnityEvent<string> { }
    
    #endregion
}
