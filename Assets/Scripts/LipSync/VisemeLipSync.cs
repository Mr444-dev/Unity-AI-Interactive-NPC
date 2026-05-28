using System;
using System.Collections.Generic;
using UnityEngine;
using InteractiveNPC.Core.Utilities;
using InteractiveNPC.LipSync.Data;
using InteractiveNPC.Speech.Interfaces;

namespace InteractiveNPC.LipSync
{
    #region Viseme Lip Sync Component
    
    /// <summary>
    /// Real-time viseme-based lip synchronization component.
    /// Analyzes audio and drives blend shapes on a SkinnedMeshRenderer.
    /// </summary>
    public class VisemeLipSync : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("References")]
        [SerializeField, Tooltip("The SkinnedMeshRenderer containing the blend shapes")]
        private SkinnedMeshRenderer _skinnedMeshRenderer;
        
        [SerializeField, Tooltip("The AudioSource to analyze for lip sync")]
        private AudioSource _audioSource;
        
        [SerializeField, Tooltip("Lip sync settings asset")]
        private LipSyncSettings _settings;
        
        [Header("Runtime Options")]
        [SerializeField, Tooltip("Enable/disable lip sync")]
        private bool _enabled = true;
        
        [SerializeField, Tooltip("Use audio analysis (true) or external viseme data (false)")]
        private bool _useAudioAnalysis = true;
        
        [Header("Debug")]
        [SerializeField, Tooltip("Show debug visualization")]
        private bool _showDebug = false;
        
        #endregion
        
        #region Private Fields
        
        private Dictionary<string, int> _blendShapeIndices;
        private float[] _audioSamples;
        private float[] _currentBlendShapeWeights;
        private float[] _targetBlendShapeWeights;
        private VisemeType _currentViseme;
        private float _currentAudioLevel;
        private List<QueuedViseme> _visemeQueue;
        
        #endregion
        
        #region Properties
        
        public bool IsEnabled
        {
            get => _enabled;
            set => _enabled = value;
        }
        
        public VisemeType CurrentViseme => _currentViseme;
        public float CurrentAudioLevel => _currentAudioLevel;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            DebugLogger.LogAwake(this, LogCategory.LipSync);
            
            _visemeQueue = new List<QueuedViseme>();
            
            // Validate references
            if (_skinnedMeshRenderer == null)
            {
                _skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            }
            
            if (_audioSource == null)
            {
                _audioSource = GetComponentInChildren<AudioSource>();
            }
        }
        
        private void Start()
        {
            DebugLogger.LogStart(this, LogCategory.LipSync);
            
            InitializeBlendShapeIndices();
            
            if (_settings != null)
            {
                _audioSamples = new float[_settings.AnalysisWindowSize];
            }
            else
            {
                _audioSamples = new float[512];
            }
        }
        
        private void Update()
        {
            if (!_enabled || _skinnedMeshRenderer == null) return;
            
            if (_useAudioAnalysis && _audioSource != null && _audioSource.isPlaying)
            {
                AnalyzeAudioAndApplyViseme();
            }
            else
            {
                ProcessVisemeQueue();
            }
            
            // Smoothly interpolate blend shape weights
            SmoothBlendShapeTransitions();
        }
        
        private void OnDestroy()
        {
            DebugLogger.LogOnDestroy(this, LogCategory.LipSync);
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Set the audio source to analyze.
        /// </summary>
        public void SetAudioSource(AudioSource audioSource)
        {
            _audioSource = audioSource;
        }
        
        /// <summary>
        /// Queue a viseme to be applied at a specific time.
        /// </summary>
        public void QueueViseme(VisemeData visemeData)
        {
            _visemeQueue.Add(new QueuedViseme
            {
                VisemeId = visemeData.VisemeId,
                Time = visemeData.Time,
                Weight = visemeData.Weight
            });
        }
        
        /// <summary>
        /// Queue multiple visemes.
        /// </summary>
        public void QueueVisemes(VisemeData[] visemes)
        {
            foreach (var viseme in visemes)
            {
                QueueViseme(viseme);
            }
            
            _visemeQueue.Sort((a, b) => a.Time.CompareTo(b.Time));
        }
        
        /// <summary>
        /// Clear the viseme queue.
        /// </summary>
        public void ClearQueue()
        {
            _visemeQueue.Clear();
        }
        
        /// <summary>
        /// Directly set a viseme (bypassing audio analysis).
        /// </summary>
        public void SetViseme(VisemeType viseme, float weight = 1f)
        {
            _currentViseme = viseme;
            ApplyViseme(viseme, weight);
        }
        
        /// <summary>
        /// Reset all blend shapes to zero.
        /// </summary>
        public void ResetBlendShapes()
        {
            if (_skinnedMeshRenderer == null || _currentBlendShapeWeights == null) return;
            
            for (int i = 0; i < _currentBlendShapeWeights.Length; i++)
            {
                _currentBlendShapeWeights[i] = 0f;
                _targetBlendShapeWeights[i] = 0f;
                _skinnedMeshRenderer.SetBlendShapeWeight(i, 0f);
            }
            
            _currentViseme = VisemeType.Silence;
        }
        
        /// <summary>
        /// Handle viseme data from TTS system.
        /// </summary>
        public void OnVisemeReceived(VisemeData visemeData)
        {
            if (!_useAudioAnalysis)
            {
                SetViseme((VisemeType)visemeData.VisemeId, visemeData.Weight);
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void InitializeBlendShapeIndices()
        {
            if (_skinnedMeshRenderer == null || _skinnedMeshRenderer.sharedMesh == null)
            {
                DebugLogger.LogWarning("SkinnedMeshRenderer or mesh not found", LogCategory.LipSync);
                return;
            }
            
            Mesh mesh = _skinnedMeshRenderer.sharedMesh;
            int blendShapeCount = mesh.blendShapeCount;
            
            _blendShapeIndices = new Dictionary<string, int>();
            _currentBlendShapeWeights = new float[blendShapeCount];
            _targetBlendShapeWeights = new float[blendShapeCount];
            
            for (int i = 0; i < blendShapeCount; i++)
            {
                string name = mesh.GetBlendShapeName(i);
                _blendShapeIndices[name] = i;
                _currentBlendShapeWeights[i] = 0f;
                _targetBlendShapeWeights[i] = 0f;
            }
            
            DebugLogger.LogInfo($"Initialized {blendShapeCount} blend shapes for lip sync", LogCategory.LipSync);
        }
        
        private void AnalyzeAudioAndApplyViseme()
        {
            if (_audioSource == null || !_audioSource.isPlaying) return;
            
            // Get audio data
            _audioSource.GetOutputData(_audioSamples, 0);
            
            // Calculate RMS amplitude
            float sum = 0f;
            for (int i = 0; i < _audioSamples.Length; i++)
            {
                sum += _audioSamples[i] * _audioSamples[i];
            }
            float rms = Mathf.Sqrt(sum / _audioSamples.Length);
            _currentAudioLevel = rms;
            
            // Apply audio threshold
            float threshold = _settings?.AudioThreshold ?? 0.01f;
            if (rms < threshold)
            {
                SetViseme(VisemeType.Silence, 0f);
                return;
            }
            
            // Simple amplitude-based viseme selection
            // In production, use FFT analysis for better phoneme detection
            VisemeType viseme = MapAmplitudeToViseme(rms);
            float weight = Mathf.Clamp01(rms * 10f) * (_settings?.IntensityMultiplier ?? 1f);
            
            ApplyViseme(viseme, weight);
        }
        
        private void ProcessVisemeQueue()
        {
            if (_visemeQueue.Count == 0 || _audioSource == null) return;
            
            float currentTime = _audioSource.time;
            
            // Find the current viseme based on audio time
            for (int i = _visemeQueue.Count - 1; i >= 0; i--)
            {
                if (_visemeQueue[i].Time <= currentTime)
                {
                    SetViseme((VisemeType)_visemeQueue[i].VisemeId, _visemeQueue[i].Weight);
                    _visemeQueue.RemoveAt(i);
                    break;
                }
            }
        }
        
        private void ApplyViseme(VisemeType viseme, float weight)
        {
            if (_settings == null || _blendShapeIndices == null) return;
            
            _currentViseme = viseme;
            
            // Reset target weights
            for (int i = 0; i < _targetBlendShapeWeights.Length; i++)
            {
                _targetBlendShapeWeights[i] = 0f;
            }
            
            // Get the mapping for this viseme
            var mapping = _settings.GetMapping(viseme);
            if (mapping == null) return;
            
            // Apply blend shape influences
            foreach (var influence in mapping.BlendShapeInfluences)
            {
                if (_blendShapeIndices.TryGetValue(influence.BlendShapeName, out int index))
                {
                    _targetBlendShapeWeights[index] = influence.Weight * weight;
                }
            }
        }
        
        private void SmoothBlendShapeTransitions()
        {
            if (_skinnedMeshRenderer == null || _currentBlendShapeWeights == null) return;
            
            float smoothing = _settings?.Smoothing ?? 0.3f;
            float lerpSpeed = 1f - smoothing;
            
            for (int i = 0; i < _currentBlendShapeWeights.Length; i++)
            {
                _currentBlendShapeWeights[i] = Mathf.Lerp(
                    _currentBlendShapeWeights[i],
                    _targetBlendShapeWeights[i],
                    Time.deltaTime * (1f / (_settings?.TransitionTime ?? 0.05f)) * lerpSpeed
                );
                
                _skinnedMeshRenderer.SetBlendShapeWeight(i, _currentBlendShapeWeights[i]);
            }
        }
        
        private VisemeType MapAmplitudeToViseme(float amplitude)
        {
            // Simple amplitude-based mapping
            // Higher amplitudes suggest more open mouth shapes
            if (amplitude < 0.02f) return VisemeType.Silence;
            if (amplitude < 0.05f) return VisemeType.PP;
            if (amplitude < 0.1f) return VisemeType.E;
            if (amplitude < 0.15f) return VisemeType.OH;
            if (amplitude < 0.2f) return VisemeType.AA;
            return VisemeType.AA;
        }
        
        #endregion
        
        #region Debug
        
        private void OnDrawGizmosSelected()
        {
            if (!_showDebug) return;
            
            // Draw audio level indicator
            Vector3 pos = transform.position + Vector3.up * 2f;
            Gizmos.color = Color.Lerp(Color.green, Color.red, _currentAudioLevel * 5f);
            Gizmos.DrawSphere(pos, 0.1f + _currentAudioLevel);
        }
        
        #endregion
        
        #region Nested Types
        
        private struct QueuedViseme
        {
            public int VisemeId;
            public float Time;
            public float Weight;
        }
        
        #endregion
    }
    
    #endregion
}
