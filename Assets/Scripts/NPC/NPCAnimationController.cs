using System;
using UnityEngine;
using DG.Tweening;
using InteractiveNPC.Core.Events;
using InteractiveNPC.Core.Utilities;
using InteractiveNPC.Personality;
using InteractiveNPC.Personality.Data;

namespace InteractiveNPC.NPC
{
    #region NPC Animation Controller
    
    /// <summary>
    /// DoTween-based animation controller for NPC reactions and expressions.
    /// Handles idle animations, emotional reactions, and conversation gestures.
    /// </summary>
    public class NPCAnimationController : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Transform References")]
        [SerializeField, Tooltip("Head transform for look-at and reactions")]
        private Transform _headTransform;
        
        [SerializeField, Tooltip("Body transform for idle sway and reactions")]
        private Transform _bodyTransform;
        
        [SerializeField, Tooltip("Eyes transform for blink and look")]
        private Transform _eyesTransform;
        
        [Header("Target")]
        [SerializeField, Tooltip("Target to look at during conversation")]
        private Transform _lookAtTarget;
        
        [SerializeField, Tooltip("Look-at weight (0-1)")]
        [Range(0f, 1f)]
        private float _lookAtWeight = 0.5f;
        
        [Header("Idle Animation")]
        [SerializeField, Tooltip("Enable idle animations")]
        private bool _enableIdleAnimations = true;
        
        [SerializeField, Tooltip("Idle body sway amount")]
        [Range(0f, 5f)]
        private float _idleSwayAmount = 1f;
        
        [SerializeField, Tooltip("Idle sway duration")]
        [Range(1f, 5f)]
        private float _idleSwayDuration = 3f;
        
        [SerializeField, Tooltip("Blink interval range (min, max)")]
        private Vector2 _blinkInterval = new Vector2(2f, 6f);
        
        [Header("Reaction Settings")]
        [SerializeField, Tooltip("Reaction animation speed")]
        [Range(0.1f, 1f)]
        private float _reactionSpeed = 0.3f;
        
        [SerializeField, Tooltip("Head nod amount")]
        [Range(0f, 20f)]
        private float _nodAmount = 10f;
        
        [SerializeField, Tooltip("Head shake amount")]
        [Range(0f, 20f)]
        private float _shakeAmount = 15f;
        
        [Header("Emotion Settings")]
        [SerializeField, Tooltip("Configure emotion animations")]
        private EmotionAnimationConfig _emotionConfig;
        
        #endregion
        
        #region Private Fields
        
        private Vector3 _originalHeadRotation;
        private Vector3 _originalBodyRotation;
        private Vector3 _originalEyeScale;
        
        private Sequence _idleSequence;
        private Sequence _currentReactionSequence;
        private Tween _blinkTween;
        
        private bool _isInConversation;
        private EmotionalState _currentEmotion = EmotionalState.Neutral;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            DebugLogger.LogAwake(this, LogCategory.NPC);
            
            // Store original transforms
            if (_headTransform != null)
                _originalHeadRotation = _headTransform.localEulerAngles;
            
            if (_bodyTransform != null)
                _originalBodyRotation = _bodyTransform.localEulerAngles;
            
            if (_eyesTransform != null)
                _originalEyeScale = _eyesTransform.localScale;
        }
        
        private void Start()
        {
            DebugLogger.LogStart(this, LogCategory.NPC);
            
            StartIdleAnimations();
            
            // Subscribe to events
            GameEvents.OnMoodChanged += HandleMoodChanged;
            GameEvents.OnConversationStarted += HandleConversationStarted;
            GameEvents.OnConversationEnded += HandleConversationEnded;
        }
        
        private void Update()
        {
            // Look at target during conversation
            if (_isInConversation && _lookAtTarget != null && _headTransform != null)
            {
                UpdateLookAt();
            }
        }
        
        private void OnDestroy()
        {
            DebugLogger.LogOnDestroy(this, LogCategory.NPC);
            
            GameEvents.OnMoodChanged -= HandleMoodChanged;
            GameEvents.OnConversationStarted -= HandleConversationStarted;
            GameEvents.OnConversationEnded -= HandleConversationEnded;
            
            KillAllTweens();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Set the look-at target.
        /// </summary>
        public void SetLookAtTarget(Transform target)
        {
            _lookAtTarget = target;
        }
        
        /// <summary>
        /// Play a head nod animation (agreement).
        /// </summary>
        public void PlayNod()
        {
            if (_headTransform == null) return;
            
            _currentReactionSequence?.Kill();
            
            _currentReactionSequence = DOTween.Sequence()
                .Append(_headTransform.DOLocalRotate(
                    _originalHeadRotation + new Vector3(_nodAmount, 0, 0),
                    _reactionSpeed * 0.5f))
                .Append(_headTransform.DOLocalRotate(
                    _originalHeadRotation - new Vector3(_nodAmount * 0.5f, 0, 0),
                    _reactionSpeed * 0.3f))
                .Append(_headTransform.DOLocalRotate(
                    _originalHeadRotation,
                    _reactionSpeed * 0.2f))
                .SetEase(Ease.InOutSine);
        }
        
        /// <summary>
        /// Play a head shake animation (disagreement).
        /// </summary>
        public void PlayShake()
        {
            if (_headTransform == null) return;
            
            _currentReactionSequence?.Kill();
            
            _currentReactionSequence = DOTween.Sequence()
                .Append(_headTransform.DOLocalRotate(
                    _originalHeadRotation + new Vector3(0, _shakeAmount, 0),
                    _reactionSpeed * 0.2f))
                .Append(_headTransform.DOLocalRotate(
                    _originalHeadRotation - new Vector3(0, _shakeAmount, 0),
                    _reactionSpeed * 0.4f))
                .Append(_headTransform.DOLocalRotate(
                    _originalHeadRotation + new Vector3(0, _shakeAmount * 0.5f, 0),
                    _reactionSpeed * 0.3f))
                .Append(_headTransform.DOLocalRotate(
                    _originalHeadRotation,
                    _reactionSpeed * 0.2f))
                .SetEase(Ease.InOutSine);
        }
        
        /// <summary>
        /// Play a surprised reaction.
        /// </summary>
        public void PlaySurprise()
        {
            _currentReactionSequence?.Kill();
            
            _currentReactionSequence = DOTween.Sequence();
            
            if (_bodyTransform != null)
            {
                _currentReactionSequence.Append(_bodyTransform.DOLocalMoveY(
                    _bodyTransform.localPosition.y + 0.1f,
                    _reactionSpeed * 0.3f));
                
                _currentReactionSequence.Append(_bodyTransform.DOLocalMoveY(
                    _bodyTransform.localPosition.y,
                    _reactionSpeed * 0.5f));
            }
            
            // Wide eyes
            if (_eyesTransform != null)
            {
                _currentReactionSequence.Join(_eyesTransform.DOScale(
                    _originalEyeScale * 1.2f,
                    _reactionSpeed * 0.2f));
                
                _currentReactionSequence.Append(_eyesTransform.DOScale(
                    _originalEyeScale,
                    _reactionSpeed * 0.5f));
            }
        }
        
        /// <summary>
        /// Play an angry reaction.
        /// </summary>
        public void PlayAngry()
        {
            _currentReactionSequence?.Kill();
            
            _currentReactionSequence = DOTween.Sequence();
            
            if (_headTransform != null)
            {
                // Forward aggressive lean
                _currentReactionSequence.Append(_headTransform.DOLocalRotate(
                    _originalHeadRotation + new Vector3(15f, 0, 0),
                    _reactionSpeed * 0.3f))
                    .SetEase(Ease.OutQuad);
            }
            
            if (_bodyTransform != null)
            {
                // Tense body
                _currentReactionSequence.Join(_bodyTransform.DOShakeRotation(
                    _reactionSpeed * 0.5f,
                    new Vector3(2f, 2f, 0),
                    10, 90));
            }
        }
        
        /// <summary>
        /// Play a happy reaction.
        /// </summary>
        public void PlayHappy()
        {
            _currentReactionSequence?.Kill();
            
            _currentReactionSequence = DOTween.Sequence();
            
            if (_headTransform != null)
            {
                // Head tilt
                _currentReactionSequence.Append(_headTransform.DOLocalRotate(
                    _originalHeadRotation + new Vector3(-5f, 0, 5f),
                    _reactionSpeed * 0.3f));
            }
            
            if (_bodyTransform != null)
            {
                // Slight bounce
                _currentReactionSequence.Join(_bodyTransform.DOLocalMoveY(
                    _bodyTransform.localPosition.y + 0.05f,
                    _reactionSpeed * 0.2f)
                    .SetLoops(2, LoopType.Yoyo));
            }
        }
        
        /// <summary>
        /// Play a sad reaction.
        /// </summary>
        public void PlaySad()
        {
            _currentReactionSequence?.Kill();
            
            _currentReactionSequence = DOTween.Sequence();
            
            if (_headTransform != null)
            {
                // Head down
                _currentReactionSequence.Append(_headTransform.DOLocalRotate(
                    _originalHeadRotation + new Vector3(20f, 0, 0),
                    _reactionSpeed * 0.5f))
                    .SetEase(Ease.InOutSine);
            }
            
            if (_bodyTransform != null)
            {
                // Slump
                _currentReactionSequence.Join(_bodyTransform.DOLocalRotate(
                    _originalBodyRotation + new Vector3(5f, 0, 0),
                    _reactionSpeed * 0.5f));
            }
        }
        
        /// <summary>
        /// Reset all animations to default pose.
        /// </summary>
        public void ResetToDefault()
        {
            KillAllTweens();
            
            if (_headTransform != null)
                _headTransform.DOLocalRotate(_originalHeadRotation, _reactionSpeed);
            
            if (_bodyTransform != null)
                _bodyTransform.DOLocalRotate(_originalBodyRotation, _reactionSpeed);
            
            if (_eyesTransform != null)
                _eyesTransform.DOScale(_originalEyeScale, _reactionSpeed);
        }
        
        /// <summary>
        /// Play emotion animation based on emotional state.
        /// </summary>
        public void PlayEmotionAnimation(EmotionalState emotion)
        {
            switch (emotion)
            {
                case EmotionalState.Happy:
                    PlayHappy();
                    break;
                case EmotionalState.Angry:
                    PlayAngry();
                    break;
                case EmotionalState.Sad:
                    PlaySad();
                    break;
                case EmotionalState.Surprised:
                    PlaySurprise();
                    break;
                case EmotionalState.Fearful:
                    PlayShake();
                    break;
                default:
                    ResetToDefault();
                    break;
            }
            
            _currentEmotion = emotion;
        }
        
        #endregion
        
        #region Private Methods
        
        private void StartIdleAnimations()
        {
            if (!_enableIdleAnimations) return;
            
            _idleSequence?.Kill();
            
            // Body sway
            if (_bodyTransform != null && _idleSwayAmount > 0)
            {
                _idleSequence = DOTween.Sequence()
                    .Append(_bodyTransform.DOLocalRotate(
                        _originalBodyRotation + new Vector3(0, 0, _idleSwayAmount),
                        _idleSwayDuration * 0.5f))
                    .Append(_bodyTransform.DOLocalRotate(
                        _originalBodyRotation + new Vector3(0, 0, -_idleSwayAmount),
                        _idleSwayDuration))
                    .Append(_bodyTransform.DOLocalRotate(
                        _originalBodyRotation,
                        _idleSwayDuration * 0.5f))
                    .SetLoops(-1)
                    .SetEase(Ease.InOutSine);
            }
            
            // Start blinking
            StartBlinking();
        }
        
        private void StartBlinking()
        {
            if (_eyesTransform == null) return;
            
            float nextBlink = UnityEngine.Random.Range(_blinkInterval.x, _blinkInterval.y);
            
            _blinkTween?.Kill();
            _blinkTween = DOVirtual.DelayedCall(nextBlink, () =>
            {
                // Quick blink
                _eyesTransform.DOScaleY(_originalEyeScale.y * 0.1f, 0.05f)
                    .OnComplete(() =>
                    {
                        _eyesTransform.DOScaleY(_originalEyeScale.y, 0.1f)
                            .OnComplete(() => StartBlinking());
                    });
            });
        }
        
        private void UpdateLookAt()
        {
            if (_headTransform == null || _lookAtTarget == null) return;
            
            Vector3 directionToTarget = _lookAtTarget.position - _headTransform.position;
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            
            // Blend with original rotation based on weight
            Quaternion originalRotation = Quaternion.Euler(_originalHeadRotation);
            Quaternion blendedRotation = Quaternion.Slerp(
                _headTransform.localRotation,
                Quaternion.Inverse(_headTransform.parent.rotation) * targetRotation,
                _lookAtWeight * Time.deltaTime * 5f
            );
            
            _headTransform.localRotation = blendedRotation;
        }
        
        private void KillAllTweens()
        {
            _idleSequence?.Kill();
            _currentReactionSequence?.Kill();
            _blinkTween?.Kill();
        }
        
        #endregion
        
        #region Event Handlers
        
        private void HandleMoodChanged(MoodEventArgs args)
        {
            // Check if this is our NPC
            NPCInteractionController npc = GetComponent<NPCInteractionController>();
            if (npc?.Configuration?.NPCId != args.NPCId) return;
            
            PlayEmotionAnimation(args.NewMood);
        }
        
        private void HandleConversationStarted(ConversationEventArgs args)
        {
            NPCInteractionController npc = GetComponent<NPCInteractionController>();
            if (npc?.Configuration?.NPCId != args.NPCId) return;
            
            _isInConversation = true;
        }
        
        private void HandleConversationEnded(ConversationEventArgs args)
        {
            NPCInteractionController npc = GetComponent<NPCInteractionController>();
            if (npc?.Configuration?.NPCId != args.NPCId) return;
            
            _isInConversation = false;
            ResetToDefault();
        }
        
        #endregion
    }
    
    #endregion
    
    #region Emotion Animation Config
    
    /// <summary>
    /// Configuration for emotion-based animations.
    /// </summary>
    [Serializable]
    public class EmotionAnimationConfig
    {
        [Header("Happy")]
        public float HappyHeadTilt = 5f;
        public float HappyBounceHeight = 0.05f;
        
        [Header("Angry")]
        public float AngryLeanForward = 15f;
        public float AngryShakeIntensity = 2f;
        
        [Header("Sad")]
        public float SadHeadDrop = 20f;
        public float SadSlump = 5f;
        
        [Header("Surprised")]
        public float SurpriseJumpHeight = 0.1f;
        public float SurpriseEyeScale = 1.2f;
        
        [Header("Fearful")]
        public float FearShakeSpeed = 0.2f;
        public float FearShakeAmount = 10f;
    }
    
    #endregion
}
