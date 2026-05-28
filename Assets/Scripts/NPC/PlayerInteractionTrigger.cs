using UnityEngine;
using InteractiveNPC.Core.Events;
using InteractiveNPC.Core.Utilities;

namespace InteractiveNPC.NPC
{
    #region Player Interaction Trigger
    
    /// <summary>
    /// Trigger zone that detects player proximity and enables NPC interaction.
    /// Attach to an NPC with a collider set as trigger.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class PlayerInteractionTrigger : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("References")]
        [SerializeField, Tooltip("NPC interaction controller")]
        private NPCInteractionController _npcController;
        
        [SerializeField, Tooltip("Conversation UI reference")]
        private UI.ConversationUI _conversationUI;
        
        [Header("Detection Settings")]
        [SerializeField, Tooltip("Layer mask for player detection")]
        private LayerMask _playerLayer;
        
        [SerializeField, Tooltip("Tag to identify player")]
        private string _playerTag = "Player";
        
        [Header("Interaction Settings")]
        [SerializeField, Tooltip("Key to start conversation")]
        private KeyCode _interactionKey = KeyCode.E;
        
        [SerializeField, Tooltip("Auto-start conversation on enter")]
        private bool _autoStartConversation = false;
        
        [Header("Visual Feedback")]
        [SerializeField, Tooltip("Interaction prompt object to show/hide")]
        private GameObject _interactionPrompt;
        
        [SerializeField, Tooltip("Highlight effect object")]
        private GameObject _highlightEffect;
        
        #endregion
        
        #region Private Fields
        
        private SphereCollider _triggerCollider;
        private bool _playerInRange;
        private Transform _playerTransform;
        
        #endregion
        
        #region Properties
        
        public bool PlayerInRange => _playerInRange;
        public NPCInteractionController NPCController => _npcController;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            DebugLogger.LogAwake(this, LogCategory.NPC);
            
            // Get or create trigger collider
            _triggerCollider = GetComponent<SphereCollider>();
            if (_triggerCollider == null)
            {
                _triggerCollider = gameObject.AddComponent<SphereCollider>();
            }
            _triggerCollider.isTrigger = true;
            
            // Get NPC controller if not assigned
            if (_npcController == null)
            {
                _npcController = GetComponentInParent<NPCInteractionController>();
            }
            
            // Set radius from configuration
            if (_npcController?.Configuration != null)
            {
                _triggerCollider.radius = _npcController.Configuration.InteractionDistance;
            }
            
            // Hide prompts initially
            SetPromptVisible(false);
            SetHighlightVisible(false);
        }
        
        private void Update()
        {
            if (!_playerInRange) return;
            
            // Check for interaction input
            if (Input.GetKeyDown(_interactionKey))
            {
                StartInteraction();
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other)) return;
            
            _playerInRange = true;
            _playerTransform = other.transform;
            
            SetPromptVisible(true);
            SetHighlightVisible(true);
            
            GameEvents.RaisePlayerEnteredNPCRange(
                _npcController?.Configuration?.NPCId ?? "unknown"
            );
            
            DebugLogger.LogVerbose($"Player entered range of {_npcController?.Configuration?.DisplayName}", LogCategory.NPC);
            
            if (_autoStartConversation)
            {
                StartInteraction();
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other)) return;
            
            _playerInRange = false;
            _playerTransform = null;
            
            SetPromptVisible(false);
            SetHighlightVisible(false);
            
            GameEvents.RaisePlayerExitedNPCRange(
                _npcController?.Configuration?.NPCId ?? "unknown"
            );
            
            DebugLogger.LogVerbose($"Player exited range of {_npcController?.Configuration?.DisplayName}", LogCategory.NPC);
            
            // End conversation if active
            if (_npcController != null && _npcController.IsInConversation)
            {
                _ = _npcController.EndConversationAsync();
                _conversationUI?.Hide();
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Manually trigger interaction.
        /// </summary>
        public void StartInteraction()
        {
            if (_npcController == null) return;
            
            if (!_npcController.IsInConversation)
            {
                // Show UI and start conversation
                _conversationUI?.Show(_npcController);
                _ = _npcController.StartConversationAsync();
            }
        }
        
        /// <summary>
        /// End the current interaction.
        /// </summary>
        public void EndInteraction()
        {
            if (_npcController?.IsInConversation == true)
            {
                _ = _npcController.EndConversationAsync();
                _conversationUI?.Hide();
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private bool IsPlayer(Collider other)
        {
            // Check tag
            if (!string.IsNullOrEmpty(_playerTag) && other.CompareTag(_playerTag))
            {
                return true;
            }
            
            // Check layer
            if (_playerLayer != 0 && (_playerLayer.value & (1 << other.gameObject.layer)) != 0)
            {
                return true;
            }
            
            return false;
        }
        
        private void SetPromptVisible(bool visible)
        {
            if (_interactionPrompt != null)
            {
                _interactionPrompt.SetActive(visible);
            }
        }
        
        private void SetHighlightVisible(bool visible)
        {
            if (_highlightEffect != null)
            {
                _highlightEffect.SetActive(visible);
            }
        }
        
        #endregion
        
        #region Debug
        
        private void OnDrawGizmosSelected()
        {
            // Draw interaction range
            Gizmos.color = _playerInRange ? Color.green : Color.yellow;
            
            float radius = _npcController?.Configuration?.InteractionDistance ?? 3f;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
        
        #endregion
    }
    
    #endregion
}
