using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using InteractiveNPC.Core.Events;
using InteractiveNPC.Core.Utilities;
using InteractiveNPC.Memory;
using InteractiveNPC.Personality;
using InteractiveNPC.NPC;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#else
using InteractiveNPC.Core.Attributes;
#endif

namespace InteractiveNPC.Debug
{
    #region NPC Debug Visualizer
    
    /// <summary>
    /// Runtime debug visualization component for NPC systems.
    /// Displays memory, mood, relationship, and conversation state in real-time.
    /// </summary>
    public class NPCDebugVisualizer : MonoBehaviour
    {
        #region Serialized Fields
        
        [Title("Target")]
        [Required]
        [SerializeField, Tooltip("NPC to monitor")]
        private NPCInteractionController _targetNPC;
        
        [Title("Display Settings")]
        [BoxGroup("Display")]
        [SerializeField]
        private bool _showMemoryPanel = true;
        
        [BoxGroup("Display")]
        [SerializeField]
        private bool _showMoodPanel = true;
        
        [BoxGroup("Display")]
        [SerializeField]
        private bool _showRelationshipPanel = true;
        
        [BoxGroup("Display")]
        [SerializeField]
        private bool _showConversationPanel = true;
        
        [BoxGroup("Display")]
        [SerializeField]
        private bool _showEventLog = true;
        
        [Title("Styling")]
        [BoxGroup("Style")]
        [SerializeField]
        private Vector2 _panelPosition = new Vector2(10, 10);
        
        [BoxGroup("Style")]
        [SerializeField]
        private float _panelWidth = 350;
        
        [BoxGroup("Style")]
        [SerializeField]
        private int _fontSize = 12;
        
        [BoxGroup("Style")]
        [SerializeField]
        private Color _backgroundColor = new Color(0, 0, 0, 0.8f);
        
        [BoxGroup("Style")]
        [SerializeField]
        private Color _headerColor = new Color(0.3f, 0.7f, 0.9f);
        
        #endregion
        
        #region Private Fields
        
        private GUIStyle _panelStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _valueStyle;
        private Texture2D _backgroundTexture;
        
        private List<DebugLogEntry> _eventLog = new List<DebugLogEntry>();
        private const int MaxLogEntries = 20;
        
        private Vector2 _scrollPosition;
        private bool _isMinimized;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_targetNPC == null)
            {
                _targetNPC = GetComponent<NPCInteractionController>();
            }
        }
        
        private void Start()
        {
            SubscribeToEvents();
            InitializeStyles();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            
            if (_backgroundTexture != null)
            {
                Destroy(_backgroundTexture);
            }
        }
        
        private void OnGUI()
        {
            if (_targetNPC == null || !Application.isPlaying) return;
            
            // Toggle minimize with F12
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F12)
            {
                _isMinimized = !_isMinimized;
                Event.current.Use();
            }
            
            if (_isMinimized)
            {
                DrawMinimizedPanel();
                return;
            }
            
            float yOffset = _panelPosition.y;
            
            // Draw each panel
            if (_showMemoryPanel)
                yOffset = DrawMemoryPanel(yOffset);
            
            if (_showMoodPanel)
                yOffset = DrawMoodPanel(yOffset);
            
            if (_showRelationshipPanel)
                yOffset = DrawRelationshipPanel(yOffset);
            
            if (_showConversationPanel)
                yOffset = DrawConversationPanel(yOffset);
            
            if (_showEventLog)
                yOffset = DrawEventLogPanel(yOffset);
        }
        
        #endregion
        
        #region Public Methods
        
#if ODIN_INSPECTOR
        [Button("Clear Event Log")]
#endif
        public void ClearEventLog()
        {
            _eventLog.Clear();
        }
        
#if ODIN_INSPECTOR
        [Button("Log Current State")]
#endif
        public void LogCurrentState()
        {
            if (_targetNPC == null) return;
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"=== NPC Debug State: {_targetNPC.Configuration?.DisplayName} ===");
            
            // Memory
            var mem = _targetNPC.MemoryBank;
            if (mem != null)
            {
                sb.AppendLine($"[Memory] Total: {mem.TotalMemoryCount}");
            }
            
            // Mood
            var mood = _targetNPC.MoodController;
            if (mood != null)
            {
                sb.AppendLine($"[Mood] {mood.CurrentEmotion} (Intensity: {mood.Intensity:F2})");
            }
            
            // Relationship
            var rel = _targetNPC.RelationshipTracker;
            if (rel != null)
            {
                sb.AppendLine($"[Relationship] {rel.Status} (Score: {rel.OverallScore:F2})");
            }
            
            // State
            sb.AppendLine($"[State] {_targetNPC.CurrentState}");
            sb.AppendLine($"[In Conversation] {_targetNPC.IsInConversation}");
            
            UnityEngine.Debug.Log(sb.ToString());
        }
        
        #endregion
        
        #region Private Methods - GUI Drawing
        
        private void InitializeStyles()
        {
            _backgroundTexture = new Texture2D(1, 1);
            _backgroundTexture.SetPixel(0, 0, _backgroundColor);
            _backgroundTexture.Apply();
            
            _panelStyle = new GUIStyle
            {
                normal = { background = _backgroundTexture },
                padding = new RectOffset(10, 10, 10, 10)
            };
            
            _headerStyle = new GUIStyle
            {
                fontSize = _fontSize + 2,
                fontStyle = FontStyle.Bold,
                normal = { textColor = _headerColor },
                margin = new RectOffset(0, 0, 5, 5)
            };
            
            _labelStyle = new GUIStyle
            {
                fontSize = _fontSize,
                normal = { textColor = Color.gray }
            };
            
            _valueStyle = new GUIStyle
            {
                fontSize = _fontSize,
                normal = { textColor = Color.white }
            };
        }
        
        private void DrawMinimizedPanel()
        {
            Rect rect = new Rect(_panelPosition.x, _panelPosition.y, 200, 30);
            GUILayout.BeginArea(rect, _panelStyle);
            GUILayout.Label($"NPC Debug [F12 to expand]", _headerStyle);
            GUILayout.EndArea();
        }
        
        private float DrawMemoryPanel(float yOffset)
        {
            var mem = _targetNPC.MemoryBank;
            if (mem == null) return yOffset;
            
            Rect rect = new Rect(_panelPosition.x, yOffset, _panelWidth, 100);
            GUILayout.BeginArea(rect, _panelStyle);
            
            GUILayout.Label("🧠 Memory System", _headerStyle);
            DrawLabelValue("Total Memories", mem.TotalMemoryCount.ToString());
            DrawLabelValue("Short-term", mem.ShortTermCount.ToString());
            DrawLabelValue("Long-term", mem.LongTermCount.ToString());
            
            GUILayout.EndArea();
            
            return yOffset + 110;
        }
        
        private float DrawMoodPanel(float yOffset)
        {
            var mood = _targetNPC.MoodController;
            if (mood == null) return yOffset;
            
            Rect rect = new Rect(_panelPosition.x, yOffset, _panelWidth, 80);
            GUILayout.BeginArea(rect, _panelStyle);
            
            GUILayout.Label("😊 Mood State", _headerStyle);
            
            Color moodColor = GetMoodColor(mood.CurrentEmotion);
            GUI.color = moodColor;
            DrawLabelValue("Current", mood.CurrentEmotion.ToString());
            GUI.color = Color.white;
            
            DrawProgressBar("Intensity", mood.Intensity, moodColor);
            
            GUILayout.EndArea();
            
            return yOffset + 90;
        }
        
        private float DrawRelationshipPanel(float yOffset)
        {
            var rel = _targetNPC.RelationshipTracker;
            if (rel == null) return yOffset;
            
            Rect rect = new Rect(_panelPosition.x, yOffset, _panelWidth, 150);
            GUILayout.BeginArea(rect, _panelStyle);
            
            GUILayout.Label("🤝 Relationship", _headerStyle);
            
            Color statusColor = GetRelationshipColor(rel.Status);
            GUI.color = statusColor;
            DrawLabelValue("Status", rel.Status.ToString());
            GUI.color = Color.white;
            
            DrawProgressBar("Trust", (rel.Data.Trust + 1f) * 0.5f, Color.green);
            DrawProgressBar("Respect", (rel.Data.Respect + 1f) * 0.5f, Color.blue);
            DrawProgressBar("Affection", (rel.Data.Affection + 1f) * 0.5f, Color.magenta);
            DrawProgressBar("Overall", (rel.OverallScore + 1f) * 0.5f, Color.yellow);
            
            GUILayout.EndArea();
            
            return yOffset + 160;
        }
        
        private float DrawConversationPanel(float yOffset)
        {
            Rect rect = new Rect(_panelPosition.x, yOffset, _panelWidth, 70);
            GUILayout.BeginArea(rect, _panelStyle);
            
            GUILayout.Label("💬 Conversation", _headerStyle);
            
            Color stateColor = _targetNPC.IsInConversation ? Color.green : Color.gray;
            GUI.color = stateColor;
            DrawLabelValue("Active", _targetNPC.IsInConversation.ToString());
            GUI.color = Color.white;
            
            DrawLabelValue("State", _targetNPC.CurrentState.ToString());
            
            GUILayout.EndArea();
            
            return yOffset + 80;
        }
        
        private float DrawEventLogPanel(float yOffset)
        {
            float height = Mathf.Min(_eventLog.Count * 20 + 40, 200);
            Rect rect = new Rect(_panelPosition.x, yOffset, _panelWidth, height);
            GUILayout.BeginArea(rect, _panelStyle);
            
            GUILayout.Label("📋 Event Log", _headerStyle);
            
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(height - 40));
            
            for (int i = _eventLog.Count - 1; i >= 0; i--)
            {
                var entry = _eventLog[i];
                GUI.color = entry.Color;
                GUILayout.Label($"[{entry.Time:F1}s] {entry.Message}", _labelStyle);
            }
            GUI.color = Color.white;
            
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            
            return yOffset + height + 10;
        }
        
        private void DrawLabelValue(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}:", _labelStyle, GUILayout.Width(100));
            GUILayout.Label(value, _valueStyle);
            GUILayout.EndHorizontal();
        }
        
        private void DrawProgressBar(string label, float value, Color color)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}:", _labelStyle, GUILayout.Width(80));
            
            Rect barRect = GUILayoutUtility.GetRect(_panelWidth - 120, 16);
            
            // Background
            GUI.color = new Color(0.2f, 0.2f, 0.2f);
            GUI.DrawTexture(barRect, Texture2D.whiteTexture);
            
            // Fill
            GUI.color = color;
            barRect.width *= Mathf.Clamp01(value);
            GUI.DrawTexture(barRect, Texture2D.whiteTexture);
            
            GUI.color = Color.white;
            GUILayout.Label($"{value:P0}", _valueStyle, GUILayout.Width(50));
            GUILayout.EndHorizontal();
        }
        
        private Color GetMoodColor(EmotionalState mood)
        {
            return mood switch
            {
                EmotionalState.Happy => Color.green,
                EmotionalState.Angry => Color.red,
                EmotionalState.Sad => Color.blue,
                EmotionalState.Fearful => new Color(0.6f, 0.3f, 0.8f),
                EmotionalState.Surprised => Color.yellow,
                _ => Color.gray
            };
        }
        
        private Color GetRelationshipColor(RelationshipStatus status)
        {
            return status switch
            {
                RelationshipStatus.BestFriend => Color.green,
                RelationshipStatus.Friend => new Color(0.5f, 0.8f, 0.5f),
                RelationshipStatus.Friendly => Color.cyan,
                RelationshipStatus.Neutral => Color.gray,
                RelationshipStatus.Unfriendly => new Color(0.8f, 0.5f, 0.3f),
                RelationshipStatus.Hostile => new Color(0.9f, 0.3f, 0.3f),
                RelationshipStatus.Enemy => Color.red,
                _ => Color.white
            };
        }
        
        #endregion
        
        #region Event Handling
        
        private void SubscribeToEvents()
        {
            GameEvents.OnMoodChanged += HandleMoodChanged;
            GameEvents.OnRelationshipChanged += HandleRelationshipChanged;
            GameEvents.OnMemoryCreated += HandleMemoryAdded;
            GameEvents.OnConversationStarted += HandleConversationStarted;
            GameEvents.OnConversationEnded += HandleConversationEnded;
        }
        
        private void UnsubscribeFromEvents()
        {
            GameEvents.OnMoodChanged -= HandleMoodChanged;
            GameEvents.OnRelationshipChanged -= HandleRelationshipChanged;
            GameEvents.OnMemoryCreated -= HandleMemoryAdded;
            GameEvents.OnConversationStarted -= HandleConversationStarted;
            GameEvents.OnConversationEnded -= HandleConversationEnded;
        }
        
        private void AddLogEntry(string message, Color color)
        {
            _eventLog.Add(new DebugLogEntry
            {
                Time = Time.time,
                Message = message,
                Color = color
            });
            
            while (_eventLog.Count > MaxLogEntries)
            {
                _eventLog.RemoveAt(0);
            }
        }
        
        private void HandleMoodChanged(MoodEventArgs args)
        {
            if (args.NPCId != _targetNPC?.Configuration?.NPCId) return;
            AddLogEntry($"Mood: {args.PreviousMood} → {args.NewMood}", GetMoodColor(args.NewMood));
        }
        
        private void HandleRelationshipChanged(RelationshipEventArgs args)
        {
            if (args.NPCId != _targetNPC?.Configuration?.NPCId) return;
            AddLogEntry($"Relationship {args.Metric}: {args.NewValue:F1}", Color.cyan);
        }
        
        private void HandleMemoryAdded(MemoryEventArgs args)
        {
            if (args.NPCId != _targetNPC?.Configuration?.NPCId) return;
            AddLogEntry($"Memory added: {args.Type}", Color.yellow);
        }
        
        private void HandleConversationStarted(ConversationEventArgs args)
        {
            if (args.NPCId != _targetNPC?.Configuration?.NPCId) return;
            AddLogEntry("Conversation started", Color.green);
        }
        
        private void HandleConversationEnded(ConversationEventArgs args)
        {
            if (args.NPCId != _targetNPC?.Configuration?.NPCId) return;
            AddLogEntry("Conversation ended", Color.gray);
        }
        
        #endregion
        
        #region Nested Types
        
        private struct DebugLogEntry
        {
            public float Time;
            public string Message;
            public Color Color;
        }
        
        #endregion
    }
    
    #endregion
}
