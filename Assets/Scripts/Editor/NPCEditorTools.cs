#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using InteractiveNPC.NPC.Data;
using InteractiveNPC.Personality.Data;
using InteractiveNPC.Memory.Data;

namespace InteractiveNPC.Editor
{
    #region NPC Configuration Editor
    
    /// <summary>
    /// Custom editor for NPCConfiguration ScriptableObject.
    /// Provides a user-friendly interface for configuring NPCs.
    /// </summary>
    [CustomEditor(typeof(NPCConfiguration))]
    public class NPCConfigurationEditor : UnityEditor.Editor
    {
        #region Private Fields
        
        private NPCConfiguration _target;
        
        // Foldout states
        private bool _identityFoldout = true;
        private bool _referencesFoldout = true;
        private bool _visualFoldout = true;
        private bool _interactionFoldout = true;
        private bool _greetingsFoldout = true;
        private bool _previewFoldout = true;
        
        // Styles
        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void OnEnable()
        {
            _target = (NPCConfiguration)target;
        }
        
        public override void OnInspectorGUI()
        {
            InitStyles();
            
            serializedObject.Update();
            
            // Header
            DrawHeader();
            
            EditorGUILayout.Space(10);
            
            // Identity Section
            _identityFoldout = DrawSection("Identity", _identityFoldout, () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_npcId"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_displayName"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_title"));
            });
            
            // References Section
            _referencesFoldout = DrawSection("Data References", _referencesFoldout, () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_personalityData"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_memoryData"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_voiceSettings"));
                
                EditorGUILayout.Space(5);
                
                if (GUILayout.Button("Create New Personality Data"))
                {
                    CreatePersonalityData();
                }
                
                if (GUILayout.Button("Create New Memory Data"))
                {
                    CreateMemoryData();
                }
            });
            
            // Visual Section
            _visualFoldout = DrawSection("Visual Settings", _visualFoldout, () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_portrait"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_themeColor"));
            });
            
            // Interaction Section
            _interactionFoldout = DrawSection("Interaction Settings", _interactionFoldout, () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_interactionDistance"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_canBeInterrupted"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_responseDelay"));
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_worldContext"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_timeOfDayBehavior"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_availableHours"));
            });
            
            // Greetings Section
            _greetingsFoldout = DrawSection("Greetings & Farewells", _greetingsFoldout, () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_defaultGreeting"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_alternativeGreetings"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_returnGreeting"));
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_defaultFarewell"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_alternativeFarewells"));
            });
            
            // Preview Section
            _previewFoldout = DrawSection("Preview", _previewFoldout, () =>
            {
                EditorGUI.BeginDisabledGroup(true);
                
                if (_target.PersonalityData != null)
                {
                    EditorGUILayout.LabelField("Personality Summary:", EditorStyles.boldLabel);
                    EditorGUILayout.TextArea(_target.PersonalityData.GetCompactSummary(), GUILayout.Height(40));
                    
                    EditorGUILayout.Space(5);
                    
                    EditorGUILayout.LabelField("Generated System Prompt:", EditorStyles.boldLabel);
                    string prompt = _target.PersonalityData.GenerateSystemPrompt();
                    EditorGUILayout.TextArea(prompt.Substring(0, Mathf.Min(500, prompt.Length)) + "...", GUILayout.Height(100));
                }
                else
                {
                    EditorGUILayout.HelpBox("Assign a Personality Data asset to preview the system prompt.", MessageType.Info);
                }
                
                EditorGUI.EndDisabledGroup();
            });
            
            serializedObject.ApplyModifiedProperties();
        }
        
        #endregion
        
        #region Private Methods
        
        private void InitStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter
                };
            }
            
            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle("box")
                {
                    padding = new RectOffset(10, 10, 10, 10)
                };
            }
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            
            // Portrait preview
            if (_target.Portrait != null)
            {
                GUILayout.Box(_target.Portrait.texture, GUILayout.Width(64), GUILayout.Height(64));
            }
            else
            {
                GUILayout.Box("No Portrait", GUILayout.Width(64), GUILayout.Height(64));
            }
            
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(_target.DisplayName, _headerStyle);
            EditorGUILayout.LabelField(_target.Title, EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.LabelField($"ID: {_target.NPCId}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private bool DrawSection(string title, bool foldout, System.Action drawContent)
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            
            foldout = EditorGUILayout.Foldout(foldout, title, true, EditorStyles.foldoutHeader);
            
            if (foldout)
            {
                EditorGUILayout.Space(5);
                drawContent?.Invoke();
            }
            
            EditorGUILayout.EndVertical();
            
            return foldout;
        }
        
        private void CreatePersonalityData()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Personality Data",
                $"{_target.DisplayName}_Personality",
                "asset",
                "Choose a location for the personality data asset"
            );
            
            if (string.IsNullOrEmpty(path)) return;
            
            var personality = ScriptableObject.CreateInstance<NPCPersonalityData>();
            AssetDatabase.CreateAsset(personality, path);
            AssetDatabase.SaveAssets();
            
            serializedObject.FindProperty("_personalityData").objectReferenceValue = personality;
            serializedObject.ApplyModifiedProperties();
            
            Selection.activeObject = personality;
        }
        
        private void CreateMemoryData()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Memory Data",
                $"{_target.DisplayName}_Memory",
                "asset",
                "Choose a location for the memory data asset"
            );
            
            if (string.IsNullOrEmpty(path)) return;
            
            var memory = ScriptableObject.CreateInstance<NPCMemoryData>();
            AssetDatabase.CreateAsset(memory, path);
            AssetDatabase.SaveAssets();
            
            serializedObject.FindProperty("_memoryData").objectReferenceValue = memory;
            serializedObject.ApplyModifiedProperties();
            
            Selection.activeObject = memory;
        }
        
        #endregion
    }
    
    #endregion
    
    #region NPC Personality Editor
    
    /// <summary>
    /// Custom editor for NPCPersonalityData ScriptableObject.
    /// </summary>
    [CustomEditor(typeof(NPCPersonalityData))]
    public class NPCPersonalityDataEditor : UnityEditor.Editor
    {
        private NPCPersonalityData _target;
        
        private void OnEnable()
        {
            _target = (NPCPersonalityData)target;
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // Draw default inspector
            DrawDefaultInspector();
            
            EditorGUILayout.Space(15);
            
            // Preview section
            EditorGUILayout.LabelField("System Prompt Preview", EditorStyles.boldLabel);
            
            EditorGUI.BeginDisabledGroup(true);
            string prompt = _target.GenerateSystemPrompt();
            EditorGUILayout.TextArea(prompt, GUILayout.Height(150));
            EditorGUI.EndDisabledGroup();
            
            if (GUILayout.Button("Copy System Prompt"))
            {
                GUIUtility.systemCopyBuffer = prompt;
                UnityEngine.Debug.Log("System prompt copied to clipboard!");
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }
    
    #endregion
    
    #region NPC Setup Wizard
    
    /// <summary>
    /// Editor window wizard for quick NPC setup.
    /// </summary>
    public class NPCSetupWizard : EditorWindow
    {
        private string _npcName = "New NPC";
        private string _profession = "Merchant";
        private string _greeting = "Hello there!";
        private float _extraversion = 0.5f;
        private float _agreeableness = 0.5f;
        private Vector2 _scrollPosition;
        
        [MenuItem("Tools/Interactive NPC/NPC Setup Wizard")]
        public static void ShowWindow()
        {
            var window = GetWindow<NPCSetupWizard>("NPC Setup Wizard");
            window.minSize = new Vector2(400, 500);
        }
        
        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            EditorGUILayout.LabelField("NPC Quick Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);
            
            // Basic Info
            EditorGUILayout.LabelField("Basic Information", EditorStyles.boldLabel);
            _npcName = EditorGUILayout.TextField("NPC Name", _npcName);
            _profession = EditorGUILayout.TextField("Profession/Role", _profession);
            _greeting = EditorGUILayout.TextField("Default Greeting", _greeting);
            
            EditorGUILayout.Space(10);
            
            // Quick Personality
            EditorGUILayout.LabelField("Quick Personality", EditorStyles.boldLabel);
            _extraversion = EditorGUILayout.Slider("Extraversion", _extraversion, 0f, 1f);
            _agreeableness = EditorGUILayout.Slider("Agreeableness", _agreeableness, 0f, 1f);
            
            EditorGUILayout.Space(20);
            
            // Create Button
            if (GUILayout.Button("Create NPC Assets", GUILayout.Height(40)))
            {
                CreateNPCAssets();
            }
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "This wizard will create:\n" +
                "• NPCConfiguration asset\n" +
                "• NPCPersonalityData asset\n" +
                "• NPCMemoryData asset\n\n" +
                "Assets will be created in Assets/Data/NPCs/",
                MessageType.Info
            );
            
            EditorGUILayout.EndScrollView();
        }
        
        private void CreateNPCAssets()
        {
            // Create folder structure
            string basePath = "Assets/Data/NPCs";
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
            {
                AssetDatabase.CreateFolder("Assets", "Data");
            }
            if (!AssetDatabase.IsValidFolder(basePath))
            {
                AssetDatabase.CreateFolder("Assets/Data", "NPCs");
            }
            
            string npcFolder = $"{basePath}/{_npcName}";
            if (!AssetDatabase.IsValidFolder(npcFolder))
            {
                AssetDatabase.CreateFolder(basePath, _npcName);
            }
            
            // Create Personality
            var personality = ScriptableObject.CreateInstance<NPCPersonalityData>();
            AssetDatabase.CreateAsset(personality, $"{npcFolder}/{_npcName}_Personality.asset");
            
            // Create Memory
            var memory = ScriptableObject.CreateInstance<NPCMemoryData>();
            AssetDatabase.CreateAsset(memory, $"{npcFolder}/{_npcName}_Memory.asset");
            
            // Create Configuration
            var config = ScriptableObject.CreateInstance<NPCConfiguration>();
            AssetDatabase.CreateAsset(config, $"{npcFolder}/{_npcName}_Config.asset");
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Select the config
            Selection.activeObject = config;
            
            UnityEngine.Debug.Log($"Created NPC assets for '{_npcName}' in {npcFolder}");
        }
    }
    
    #endregion
    
    #region Memory Debug Window
    
    /// <summary>
    /// Editor window for debugging NPC memories at runtime.
    /// </summary>
    public class MemoryDebugWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private NPC.NPCInteractionController _selectedNPC;
        
        [MenuItem("Tools/Interactive NPC/Memory Debug Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<MemoryDebugWindow>("Memory Debug");
            window.minSize = new Vector2(400, 300);
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("NPC Memory Debug", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);
            
            // NPC Selection
            _selectedNPC = (NPC.NPCInteractionController)EditorGUILayout.ObjectField(
                "Select NPC",
                _selectedNPC,
                typeof(NPC.NPCInteractionController),
                true
            );
            
            EditorGUILayout.Space(10);
            
            if (_selectedNPC == null)
            {
                EditorGUILayout.HelpBox("Select an NPC to view its memories.", MessageType.Info);
                return;
            }
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play mode to view runtime memories.", MessageType.Warning);
                return;
            }
            
            var memoryBank = _selectedNPC.MemoryBank;
            var relationship = _selectedNPC.RelationshipTracker;
            
            if (memoryBank == null)
            {
                EditorGUILayout.HelpBox("Memory system not initialized.", MessageType.Warning);
                return;
            }
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            // Memory Stats
            EditorGUILayout.LabelField($"Total Memories: {memoryBank.TotalMemoryCount}", EditorStyles.boldLabel);
            
            // Relationship
            if (relationship != null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Relationship", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Status: {relationship.Status}");
                EditorGUILayout.LabelField($"Overall Score: {relationship.OverallScore:F2}");
            }
            
            // Mood
            var mood = _selectedNPC.MoodController;
            if (mood != null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Current Mood", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"{mood.CurrentEmotion} (Intensity: {mood.Intensity:F2})");
            }
            
            EditorGUILayout.EndScrollView();
            
            // Repaint in play mode
            if (Application.isPlaying)
            {
                Repaint();
            }
        }
    }
    
    #endregion
}
#endif
