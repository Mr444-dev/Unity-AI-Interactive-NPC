using System;
using System.IO;
using UnityEngine;
using InteractiveNPC.Core.Utilities;
using InteractiveNPC.Memory.Data;

namespace InteractiveNPC.Memory
{
    #region Save Data Classes
    
    /// <summary>
    /// Serializable data structure for saving NPC memory state.
    /// </summary>
    [Serializable]
    public class NPCMemorySaveData
    {
        public string NPCId;
        public string SaveTimestamp;
        public MemoryCollection Memories;
        public RelationshipData Relationship;
    }
    
    /// <summary>
    /// Container for all NPC save data.
    /// </summary>
    [Serializable]
    public class GameMemorySaveData
    {
        public string GameVersion;
        public string SaveTimestamp;
        public NPCMemorySaveData[] NPCData;
    }
    
    #endregion
    
    #region Memory Persistence Interface
    
    /// <summary>
    /// Interface for memory persistence implementations.
    /// </summary>
    public interface IMemoryPersistence
    {
        /// <summary>
        /// Save memory state for an NPC.
        /// </summary>
        bool SaveNPCMemory(string npcId, MemoryBank memoryBank, RelationshipTracker relationshipTracker);
        
        /// <summary>
        /// Load memory state for an NPC.
        /// </summary>
        NPCMemorySaveData LoadNPCMemory(string npcId);
        
        /// <summary>
        /// Check if save data exists for an NPC.
        /// </summary>
        bool HasSaveData(string npcId);
        
        /// <summary>
        /// Delete save data for an NPC.
        /// </summary>
        bool DeleteNPCMemory(string npcId);
        
        /// <summary>
        /// Delete all save data.
        /// </summary>
        void DeleteAllMemories();
    }
    
    #endregion
    
    #region Memory Persistence Implementation
    
    /// <summary>
    /// Handles saving and loading NPC memory and relationship data to persistent storage.
    /// Uses JSON serialization for cross-platform compatibility.
    /// </summary>
    public class MemoryPersistence : IMemoryPersistence
    {
        #region Private Fields
        
        private readonly string _saveDirectory;
        private readonly string _gameVersion;
        private const string FileExtension = ".json";
        private const string SaveFolderName = "NPCMemories";
        
        #endregion
        
        #region Constructor
        
        public MemoryPersistence(string gameVersion = "1.0.0")
        {
            _gameVersion = gameVersion;
            _saveDirectory = Path.Combine(Application.persistentDataPath, SaveFolderName);
            
            // Ensure save directory exists
            if (!Directory.Exists(_saveDirectory))
            {
                Directory.CreateDirectory(_saveDirectory);
                DebugLogger.LogInfo($"Created save directory: {_saveDirectory}", LogCategory.Memory);
            }
            
            DebugLogger.LogInfo($"MemoryPersistence initialized. Save path: {_saveDirectory}", LogCategory.Memory);
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Save memory and relationship state for an NPC.
        /// </summary>
        public bool SaveNPCMemory(string npcId, MemoryBank memoryBank, RelationshipTracker relationshipTracker)
        {
            if (string.IsNullOrEmpty(npcId))
            {
                DebugLogger.LogError("Cannot save: NPC ID is null or empty", LogCategory.Memory);
                return false;
            }
            
            try
            {
                NPCMemorySaveData saveData = new NPCMemorySaveData
                {
                    NPCId = npcId,
                    SaveTimestamp = DateTime.Now.ToString("o"),
                    Memories = memoryBank?.ExportMemories(),
                    Relationship = relationshipTracker?.ExportData()
                };
                
                string json = JsonUtility.ToJson(saveData, true);
                string filePath = GetSaveFilePath(npcId);
                
                File.WriteAllText(filePath, json);
                
                DebugLogger.LogInfo($"Saved memory for NPC: {npcId}", LogCategory.Memory);
                return true;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, LogCategory.Memory);
                return false;
            }
        }
        
        /// <summary>
        /// Load memory and relationship state for an NPC.
        /// </summary>
        public NPCMemorySaveData LoadNPCMemory(string npcId)
        {
            if (string.IsNullOrEmpty(npcId))
            {
                DebugLogger.LogError("Cannot load: NPC ID is null or empty", LogCategory.Memory);
                return null;
            }
            
            string filePath = GetSaveFilePath(npcId);
            
            if (!File.Exists(filePath))
            {
                DebugLogger.LogVerbose($"No save data found for NPC: {npcId}", LogCategory.Memory);
                return null;
            }
            
            try
            {
                string json = File.ReadAllText(filePath);
                NPCMemorySaveData saveData = JsonUtility.FromJson<NPCMemorySaveData>(json);
                
                DebugLogger.LogInfo($"Loaded memory for NPC: {npcId}", LogCategory.Memory);
                return saveData;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, LogCategory.Memory);
                return null;
            }
        }
        
        /// <summary>
        /// Check if save data exists for an NPC.
        /// </summary>
        public bool HasSaveData(string npcId)
        {
            if (string.IsNullOrEmpty(npcId)) return false;
            return File.Exists(GetSaveFilePath(npcId));
        }
        
        /// <summary>
        /// Delete save data for an NPC.
        /// </summary>
        public bool DeleteNPCMemory(string npcId)
        {
            if (string.IsNullOrEmpty(npcId))
            {
                DebugLogger.LogError("Cannot delete: NPC ID is null or empty", LogCategory.Memory);
                return false;
            }
            
            string filePath = GetSaveFilePath(npcId);
            
            if (!File.Exists(filePath))
            {
                return true; // Already doesn't exist
            }
            
            try
            {
                File.Delete(filePath);
                DebugLogger.LogInfo($"Deleted memory for NPC: {npcId}", LogCategory.Memory);
                return true;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, LogCategory.Memory);
                return false;
            }
        }
        
        /// <summary>
        /// Delete all saved NPC memories.
        /// </summary>
        public void DeleteAllMemories()
        {
            try
            {
                if (Directory.Exists(_saveDirectory))
                {
                    string[] files = Directory.GetFiles(_saveDirectory, "*" + FileExtension);
                    foreach (string file in files)
                    {
                        File.Delete(file);
                    }
                    
                    DebugLogger.LogInfo($"Deleted {files.Length} memory save files", LogCategory.Memory);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, LogCategory.Memory);
            }
        }
        
        /// <summary>
        /// Save all NPC memories at once (for game save).
        /// </summary>
        public bool SaveAllMemories(params (string npcId, MemoryBank bank, RelationshipTracker tracker)[] npcs)
        {
            bool allSucceeded = true;
            
            foreach (var npc in npcs)
            {
                if (!SaveNPCMemory(npc.npcId, npc.bank, npc.tracker))
                {
                    allSucceeded = false;
                }
            }
            
            return allSucceeded;
        }
        
        /// <summary>
        /// Get list of all saved NPC IDs.
        /// </summary>
        public string[] GetAllSavedNPCIds()
        {
            if (!Directory.Exists(_saveDirectory))
            {
                return Array.Empty<string>();
            }
            
            string[] files = Directory.GetFiles(_saveDirectory, "*" + FileExtension);
            string[] ids = new string[files.Length];
            
            for (int i = 0; i < files.Length; i++)
            {
                ids[i] = Path.GetFileNameWithoutExtension(files[i]);
            }
            
            return ids;
        }
        
        /// <summary>
        /// Get the save directory path.
        /// </summary>
        public string GetSaveDirectory()
        {
            return _saveDirectory;
        }
        
        #endregion
        
        #region Private Methods
        
        private string GetSaveFilePath(string npcId)
        {
            // Sanitize NPC ID for use as filename
            string sanitized = SanitizeFileName(npcId);
            return Path.Combine(_saveDirectory, sanitized + FileExtension);
        }
        
        private string SanitizeFileName(string fileName)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName;
        }
        
        #endregion
    }
    
    #endregion
    
    #region Auto-Save Manager
    
    /// <summary>
    /// MonoBehaviour that handles automatic saving of NPC memories.
    /// </summary>
    public class MemoryAutoSaveManager : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Auto-Save Configuration")]
        [SerializeField, Tooltip("Enable automatic saving")]
        private bool _enableAutoSave = true;
        
        [SerializeField, Tooltip("Auto-save interval in minutes")]
        [Range(1f, 30f)]
        private float _autoSaveIntervalMinutes = 5f;
        
        [SerializeField, Tooltip("Save on application pause/quit")]
        private bool _saveOnPause = true;
        
        #endregion
        
        #region Private Fields
        
        private MemoryPersistence _persistence;
        private float _timeSinceLastSave;
        
        #endregion
        
        #region Events
        
        public event Action OnAutoSaveStarted;
        public event Action<bool> OnAutoSaveCompleted;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            _persistence = new MemoryPersistence(Application.version);
            DebugLogger.LogAwake(this, LogCategory.Memory);
        }
        
        private void Start()
        {
            DebugLogger.LogStart(this, LogCategory.Memory);
        }
        
        private void Update()
        {
            if (!_enableAutoSave) return;
            
            _timeSinceLastSave += Time.deltaTime;
            
            if (_timeSinceLastSave >= _autoSaveIntervalMinutes * 60f)
            {
                TriggerAutoSave();
                _timeSinceLastSave = 0f;
            }
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (_saveOnPause && pauseStatus)
            {
                TriggerAutoSave();
            }
        }
        
        private void OnApplicationQuit()
        {
            if (_saveOnPause)
            {
                TriggerAutoSave();
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Manually trigger an auto-save.
        /// </summary>
        public void TriggerAutoSave()
        {
            OnAutoSaveStarted?.Invoke();
            
            // Find all NPCs and save their memories
            // This will be implemented when NPCInteractionController is created
            // For now, just log that auto-save was triggered
            
            DebugLogger.LogInfo("Auto-save triggered", LogCategory.Memory);
            
            OnAutoSaveCompleted?.Invoke(true);
        }
        
        /// <summary>
        /// Get the persistence instance.
        /// </summary>
        public MemoryPersistence GetPersistence()
        {
            return _persistence;
        }
        
        #endregion
    }
    
    #endregion
}
