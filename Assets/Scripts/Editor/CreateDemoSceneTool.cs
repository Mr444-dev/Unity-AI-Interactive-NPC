#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections.Generic;
using InteractiveNPC.NPC;
using InteractiveNPC.NPC.Data;
using InteractiveNPC.Personality.Data;
using InteractiveNPC.Memory.Data;
using InteractiveNPC.LLM.Data;
using InteractiveNPC.Speech.Data;
using InteractiveNPC.UI;
using InteractiveNPC.LipSync;

namespace InteractiveNPC.Editor
{
    public class CreateDemoSceneTool : EditorWindow
    {
        [MenuItem("Tools/Interactive NPC/Create Demo Scene")]
        public static void CreateDemoScene()
        {
            // 1. Create Folders
            string dataFolder = "Assets/Data/DemoNPC";
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
            {
                AssetDatabase.CreateFolder("Assets", "Data");
            }
            if (!AssetDatabase.IsValidFolder(dataFolder))
            {
                AssetDatabase.CreateFolder("Assets/Data", "DemoNPC");
            }

            string prefabsFolder = "Assets/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabsFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            // 2. Create ScriptableObjects if they don't exist
            string personalityPath = $"{dataFolder}/Demo_Personality.asset";
            NPCPersonalityData personality = AssetDatabase.LoadAssetAtPath<NPCPersonalityData>(personalityPath);
            if (personality == null)
            {
                personality = ScriptableObject.CreateInstance<NPCPersonalityData>();
                AssetDatabase.CreateAsset(personality, personalityPath);
            }

            string memoryPath = $"{dataFolder}/Demo_Memory.asset";
            NPCMemoryData memory = AssetDatabase.LoadAssetAtPath<NPCMemoryData>(memoryPath);
            if (memory == null)
            {
                memory = ScriptableObject.CreateInstance<NPCMemoryData>();
                AssetDatabase.CreateAsset(memory, memoryPath);
            }

            string llmPath = $"{dataFolder}/Demo_LLM.asset";
            LLMSettings llmSettings = AssetDatabase.LoadAssetAtPath<LLMSettings>(llmPath);
            if (llmSettings == null)
            {
                llmSettings = ScriptableObject.CreateInstance<LLMSettings>();
                // Configure default provider to Ollama or OpenAI
                var serializedLLM = new SerializedObject(llmSettings);
                serializedLLM.FindProperty("_providerType").enumValueIndex = (int)LLMProviderType.Ollama; // Default to Ollama for local play
                serializedLLM.FindProperty("_ollamaUrl").stringValue = "http://localhost:11434";
                serializedLLM.FindProperty("_ollamaModel").stringValue = "llama3.2";
                serializedLLM.ApplyModifiedProperties();
                AssetDatabase.CreateAsset(llmSettings, llmPath);
            }

            string configPath = $"{dataFolder}/Demo_Config.asset";
            NPCConfiguration config = AssetDatabase.LoadAssetAtPath<NPCConfiguration>(configPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<NPCConfiguration>();
                AssetDatabase.CreateAsset(config, configPath);
            }

            // Configure NPCConfiguration
            var serializedConfig = new SerializedObject(config);
            serializedConfig.FindProperty("_npcId").stringValue = "demo_guide";
            serializedConfig.FindProperty("_displayName").stringValue = "Demo Guide";
            serializedConfig.FindProperty("_title").stringValue = "Interactive NPC Demo Guide";
            serializedConfig.FindProperty("_personalityData").objectReferenceValue = personality;
            serializedConfig.FindProperty("_memoryData").objectReferenceValue = memory;
            serializedConfig.FindProperty("_interactionDistance").floatValue = 3.5f;
            serializedConfig.FindProperty("_defaultGreeting").stringValue = "Welcome to the Interactive NPC demo! I'm your guide. What would you like to ask about?";
            serializedConfig.FindProperty("_defaultFarewell").stringValue = "Goodbye! Have fun testing.";
            serializedConfig.ApplyModifiedProperties();

            // 3. Create bubble prefabs if they don't exist
            string playerBubblePath = $"{prefabsFolder}/PlayerBubble.prefab";
            GameObject playerBubblePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerBubblePath);
            if (playerBubblePrefab == null)
            {
                GameObject root = new GameObject("PlayerBubble", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                root.GetComponent<Image>().color = new Color(0.18f, 0.49f, 0.96f, 0.85f); // Beautiful blue bubble
                
                GameObject textObj = new GameObject("Text (TMP)", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(root.transform);
                var text = textObj.GetComponent<TextMeshProUGUI>();
                text.color = Color.white;
                text.fontSize = 14;
                text.text = "Player Message Placeholder";
                
                // Add horizontal layout group or content size fitter for bubbles
                var fitter = root.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                
                var layout = root.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(10, 10, 10, 10);
                
                var textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;

                playerBubblePrefab = PrefabUtility.SaveAsPrefabAsset(root, playerBubblePath);
                DestroyImmediate(root);
            }

            string npcBubblePath = $"{prefabsFolder}/NPCBubble.prefab";
            GameObject npcBubblePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(npcBubblePath);
            if (npcBubblePrefab == null)
            {
                GameObject root = new GameObject("NPCBubble", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                root.GetComponent<Image>().color = new Color(0.24f, 0.24f, 0.24f, 0.85f); // Sleek grey bubble
                
                GameObject textObj = new GameObject("Text (TMP)", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(root.transform);
                var text = textObj.GetComponent<TextMeshProUGUI>();
                text.color = Color.white;
                text.fontSize = 14;
                text.text = "NPC Message Placeholder";
                
                var fitter = root.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                
                var layout = root.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(10, 10, 10, 10);
                
                var textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;

                npcBubblePrefab = PrefabUtility.SaveAsPrefabAsset(root, npcBubblePath);
                DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 4. Create Scene
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Set ground
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "Ground";
            plane.transform.localScale = new Vector3(3f, 1f, 3f);
            Material groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            groundMat.color = new Color(0.3f, 0.3f, 0.3f);
            AssetDatabase.CreateAsset(groundMat, "Assets/Data/DemoNPC/GroundMaterial.mat");
            plane.GetComponent<Renderer>().sharedMaterial = groundMat;

            // 5. Create Player
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 1f, 0f);
            player.AddComponent<CharacterController>();
            player.AddComponent<SimplePlayerMovement>();

            // Setup camera following or simple main camera positioning
            Camera.main.transform.position = new Vector3(0f, 4f, -7f);
            Camera.main.transform.rotation = Quaternion.Euler(20f, 0f, 0f);

            // 6. Create NPC
            GameObject npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npc.name = "InteractiveNPC_Demo";
            npc.transform.position = new Vector3(0f, 1f, 5f);
            
            // Set NPC color to blue/violet
            var renderer = npc.GetComponent<Renderer>();
            Material npcMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            npcMat.color = new Color(0.5f, 0.2f, 0.8f);
            AssetDatabase.CreateAsset(npcMat, "Assets/Data/DemoNPC/NPCMaterial.mat");
            renderer.sharedMaterial = npcMat;

            // Add AudioSource
            var audioSource = npc.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D Sound

            // Add VisemeLipSync
            var lipSync = npc.AddComponent<VisemeLipSync>();

            // Add NPCInteractionController
            var interactionController = npc.AddComponent<NPCInteractionController>();
            var serializedNPCController = new SerializedObject(interactionController);
            serializedNPCController.FindProperty("_configuration").objectReferenceValue = config;
            serializedNPCController.FindProperty("_llmSettings").objectReferenceValue = llmSettings;
            serializedNPCController.FindProperty("_audioSource").objectReferenceValue = audioSource;
            serializedNPCController.FindProperty("_lipSync").objectReferenceValue = lipSync;
            serializedNPCController.ApplyModifiedProperties();

            // Add PlayerInteractionTrigger
            var trigger = npc.AddComponent<PlayerInteractionTrigger>();
            var serializedTrigger = new SerializedObject(trigger);
            serializedTrigger.FindProperty("_npcController").objectReferenceValue = interactionController;
            serializedTrigger.FindProperty("_playerTag").stringValue = "Player";
            serializedTrigger.ApplyModifiedProperties();

            // 7. Create Canvas & ConversationUI
            DefaultControls.Resources uiResources = new DefaultControls.Resources();
            GameObject canvasObj = new GameObject("ConversationCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // Add EventSystem if not exists
            if (GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            }

            // Create main panel
            GameObject panelObj = DefaultControls.CreatePanel(uiResources);
            panelObj.name = "ConversationPanel";
            panelObj.transform.SetParent(canvasObj.transform, false);

            var panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 20f);
            panelRect.sizeDelta = new Vector2(600f, 400f);

            // Change panel look to sleek semi-transparent dark grey
            var panelImage = panelObj.GetComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            // Create NPC Name Text (TMP)
            GameObject npcNameObj = new GameObject("NPCNameText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            npcNameObj.transform.SetParent(panelObj.transform, false);
            var npcNameText = npcNameObj.GetComponent<TextMeshProUGUI>();
            npcNameText.text = "NPC Name";
            npcNameText.fontSize = 20;
            npcNameText.fontStyle = FontStyles.Bold;
            npcNameText.color = Color.cyan;
            var npcNameRect = npcNameObj.GetComponent<RectTransform>();
            npcNameRect.anchorMin = new Vector2(0f, 1f);
            npcNameRect.anchorMax = new Vector2(1f, 1f);
            npcNameRect.pivot = new Vector2(0.5f, 1f);
            npcNameRect.anchoredPosition = new Vector2(10f, -10f);
            npcNameRect.sizeDelta = new Vector2(-20f, 30f);

            // Create ScrollView for history
            GameObject scrollViewObj = DefaultControls.CreateScrollView(uiResources);
            scrollViewObj.name = "DialogueScrollView";
            scrollViewObj.transform.SetParent(panelObj.transform, false);
            var scrollRect = scrollViewObj.GetComponent<ScrollRect>();
            var scrollRectTrans = scrollViewObj.GetComponent<RectTransform>();
            scrollRectTrans.anchorMin = new Vector2(0f, 0f);
            scrollRectTrans.anchorMax = new Vector2(1f, 1f);
            scrollRectTrans.pivot = new Vector2(0.5f, 0.5f);
            scrollRectTrans.anchoredPosition = new Vector2(0f, 20f);
            scrollRectTrans.sizeDelta = new Vector2(-20f, -100f);

            // Get DialogueContainer from scroll view content
            var containerTrans = scrollRect.content;
            containerTrans.gameObject.name = "DialogueContainer";
            
            // Add vertical layout group to container
            var containerLayout = containerTrans.gameObject.AddComponent<VerticalLayoutGroup>();
            containerLayout.childAlignment = TextAnchor.LowerCenter;
            containerLayout.childControlHeight = true;
            containerLayout.childControlWidth = true;
            containerLayout.childForceExpandHeight = false;
            containerLayout.childForceExpandWidth = true;
            containerLayout.spacing = 10;
            containerTrans.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Create InputField (using TMPro)
            GameObject inputFieldObj = new GameObject("PlayerInputField", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
            inputFieldObj.transform.SetParent(panelObj.transform, false);
            var inputImage = inputFieldObj.GetComponent<Image>();
            inputImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            GameObject textareaObj = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            textareaObj.transform.SetParent(inputFieldObj.transform, false);
            var textareaRect = textareaObj.GetComponent<RectTransform>();
            textareaRect.anchorMin = Vector2.zero;
            textareaRect.anchorMax = Vector2.one;
            textareaRect.sizeDelta = new Vector2(-10f, -10f);

            GameObject textPlaceholderObj = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textPlaceholderObj.transform.SetParent(textareaObj.transform, false);
            var placeholderText = textPlaceholderObj.GetComponent<TextMeshProUGUI>();
            placeholderText.text = "Type something...";
            placeholderText.color = Color.grey;
            placeholderText.fontSize = 14;
            placeholderText.fontStyle = FontStyles.Italic;
            var placeholderRect = textPlaceholderObj.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.sizeDelta = Vector2.zero;

            GameObject textDisplayObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textDisplayObj.transform.SetParent(textareaObj.transform, false);
            var textDisplay = textDisplayObj.GetComponent<TextMeshProUGUI>();
            textDisplay.color = Color.white;
            textDisplay.fontSize = 14;
            var textDisplayRect = textDisplayObj.GetComponent<RectTransform>();
            textDisplayRect.anchorMin = Vector2.zero;
            textDisplayRect.anchorMax = Vector2.one;
            textDisplayRect.sizeDelta = Vector2.zero;

            var tmpInputField = inputFieldObj.GetComponent<TMP_InputField>();
            tmpInputField.textViewport = textareaRect;
            tmpInputField.textComponent = textDisplay;
            tmpInputField.placeholder = placeholderText;
            
            var inputFieldRect = inputFieldObj.GetComponent<RectTransform>();
            inputFieldRect.anchorMin = new Vector2(0f, 0f);
            inputFieldRect.anchorMax = new Vector2(0.8f, 0f);
            inputFieldRect.pivot = new Vector2(0f, 0f);
            inputFieldRect.anchoredPosition = new Vector2(10f, 10f);
            inputFieldRect.sizeDelta = new Vector2(0f, 40f);

            // Create Send Button
            GameObject sendButtonObj = DefaultControls.CreateButton(uiResources);
            sendButtonObj.name = "SendButton";
            sendButtonObj.transform.SetParent(panelObj.transform, false);
            
            var sendBtnText = sendButtonObj.GetComponentInChildren<Text>();
            if (sendBtnText != null) sendBtnText.text = "Send";

            var sendButtonRect = sendButtonObj.GetComponent<RectTransform>();
            sendButtonRect.anchorMin = new Vector2(0.8f, 0f);
            sendButtonRect.anchorMax = new Vector2(0.9f, 0f);
            sendButtonRect.pivot = new Vector2(0f, 0f);
            sendButtonRect.anchoredPosition = new Vector2(15f, 10f);
            sendButtonRect.sizeDelta = new Vector2(0f, 40f);

            // Create Close Button
            GameObject closeButtonObj = DefaultControls.CreateButton(uiResources);
            closeButtonObj.name = "CloseButton";
            closeButtonObj.transform.SetParent(panelObj.transform, false);
            
            var closeBtnText = closeButtonObj.GetComponentInChildren<Text>();
            if (closeBtnText != null) closeBtnText.text = "Close";

            var closeButtonRect = closeButtonObj.GetComponent<RectTransform>();
            closeButtonRect.anchorMin = new Vector2(0.9f, 0f);
            closeButtonRect.anchorMax = new Vector2(1f, 0f);
            closeButtonRect.pivot = new Vector2(0f, 0f);
            closeButtonRect.anchoredPosition = new Vector2(20f, 10f);
            closeButtonRect.sizeDelta = new Vector2(-30f, 40f);

            // Setup ConversationUI script on canvas
            var convUI = canvasObj.AddComponent<ConversationUI>();
            var serializedUI = new SerializedObject(convUI);
            serializedUI.FindProperty("_conversationPanel").objectReferenceValue = panelRect;
            serializedUI.FindProperty("_npcNameText").objectReferenceValue = npcNameText;
            serializedUI.FindProperty("_inputField").objectReferenceValue = tmpInputField;
            serializedUI.FindProperty("_sendButton").objectReferenceValue = sendButtonObj.GetComponent<Button>();
            serializedUI.FindProperty("_closeButton").objectReferenceValue = closeButtonObj.GetComponent<Button>();
            serializedUI.FindProperty("_dialogueScrollRect").objectReferenceValue = scrollRect;
            serializedUI.FindProperty("_dialogueContainer").objectReferenceValue = containerTrans;
            serializedUI.FindProperty("_playerBubblePrefab").objectReferenceValue = playerBubblePrefab;
            serializedUI.FindProperty("_npcBubblePrefab").objectReferenceValue = npcBubblePrefab;
            serializedUI.ApplyModifiedProperties();

            // Link trigger to ConversationUI
            var serializedTrigger2 = new SerializedObject(trigger);
            serializedTrigger2.FindProperty("_conversationUI").objectReferenceValue = convUI;
            serializedTrigger2.ApplyModifiedProperties();

            // 8. Save the Scene
            string scenePath = "Assets/Scenes/DemoScene.unity";
            EditorSceneManager.SaveScene(newScene, scenePath);
            
            UnityEngine.Debug.Log($"[Interactive NPC] Success! Demo Scene created at {scenePath}. Set up your LLM Settings asset at '{llmPath}' before playing.");
            EditorUtility.DisplayDialog("Interactive NPC Setup", "Demo Scene created successfully!\n\nOpen Assets/Scenes/DemoScene.unity and play.\n\nMake sure to configure your LLM settings (Ollama / OpenAI API Key) in Assets/Data/DemoNPC/Demo_LLM.asset", "OK");
        }
    }
}
#endif
