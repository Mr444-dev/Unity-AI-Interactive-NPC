using System;
using UnityEngine.Events;
using InteractiveNPC.Core.Events;

namespace InteractiveNPC.NPC
{
    /// <summary>
    /// Custom UnityEvents for NPC system.
    /// </summary>
    
    [Serializable]
    public class ConversationUnityEvent : UnityEvent<ConversationEventArgs> { }
    
    [Serializable]
    public class StringUnityEvent : UnityEvent<string> { }
    
    [Serializable]
    public class MoodUnityEvent : UnityEvent<MoodEventArgs> { }
    
    [Serializable]
    public class RelationshipUnityEvent : UnityEvent<RelationshipEventArgs> { }
}
