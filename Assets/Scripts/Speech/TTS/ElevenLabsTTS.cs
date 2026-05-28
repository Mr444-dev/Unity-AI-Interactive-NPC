using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using InteractiveNPC.Core.Events;
using InteractiveNPC.Core.Utilities;
using InteractiveNPC.Speech.Data;
using InteractiveNPC.Speech.Interfaces;

namespace InteractiveNPC.Speech.TTS
{
    #region ElevenLabs TTS Implementation
    
    /// <summary>
    /// Text-to-Speech implementation using ElevenLabs API.
    /// Provides high-quality, expressive voice synthesis for NPC dialogue.
    /// </summary>
    public class ElevenLabsTTS : ITextToSpeech
    {
        #region Private Fields
        
        private readonly TTSSettings _settings;
        private AudioSource _audioSource;
        private CancellationTokenSource _currentOperationCts;
        private bool _isSpeaking;
        private float _playbackProgress;
        private Dictionary<int, AudioClip> _audioCache;
        
        #endregion
        
        #region Events
        
        public event Action OnSpeechStarted;
        public event Action OnSpeechCompleted;
        public event Action<VisemeData> OnVisemeReceived;
        
        #endregion
        
        #region Properties
        
        public bool IsAvailable => _settings?.IsConfigured ?? false;
        public bool IsSpeaking => _isSpeaking;
        public float PlaybackProgress => _playbackProgress;
        
        #endregion
        
        #region Constructor
        
        public ElevenLabsTTS(TTSSettings settings, AudioSource audioSource = null)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _audioSource = audioSource;
            _audioCache = new Dictionary<int, AudioClip>();
            
            DebugLogger.LogInfo($"ElevenLabsTTS created with voice: {settings.VoiceName}", LogCategory.Speech);
        }
        
        #endregion
        
        #region IService Implementation
        
        public void Initialize()
        {
            if (_audioSource == null)
            {
                var go = new GameObject("ElevenLabs_AudioSource");
                _audioSource = go.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                UnityEngine.Object.DontDestroyOnLoad(go);
            }
            
            DebugLogger.LogInfo("ElevenLabsTTS initialized", LogCategory.Speech);
        }
        
        public void Shutdown()
        {
            Stop();
            ClearCache();
            
            if (_audioSource != null && _audioSource.gameObject.name == "ElevenLabs_AudioSource")
            {
                UnityEngine.Object.Destroy(_audioSource.gameObject);
            }
            
            DebugLogger.LogInfo("ElevenLabsTTS shutdown", LogCategory.Speech);
        }
        
        #endregion
        
        #region ITextToSpeech Implementation
        
        public async UniTask<TTSResult> SpeakAsync(string text, CancellationToken cancellationToken = default)
        {
            var result = await SynthesizeAsync(text, cancellationToken);
            
            if (result.Success && result.AudioClip != null)
            {
                await PlayAudioAsync(result.AudioClip, cancellationToken);
            }
            
            return result;
        }
        
        public async UniTask<TTSResult> SynthesizeAsync(string text, CancellationToken cancellationToken = default)
        {
            if (!IsAvailable)
            {
                return new TTSResult
                {
                    Success = false,
                    ErrorMessage = "ElevenLabs not configured"
                };
            }
            
            if (string.IsNullOrWhiteSpace(text))
            {
                return new TTSResult
                {
                    Success = false,
                    ErrorMessage = "Text cannot be empty"
                };
            }
            
            // Check cache
            int textHash = text.GetHashCode();
            if (_settings.EnableCaching && _audioCache.TryGetValue(textHash, out AudioClip cachedClip))
            {
                DebugLogger.LogVerbose("Using cached audio for text", LogCategory.Speech);
                return new TTSResult
                {
                    Success = true,
                    AudioClip = cachedClip,
                    Duration = cachedClip.length
                };
            }
            
            _currentOperationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            try
            {
                string jsonBody = BuildRequestBody(text);
                
                using var webRequest = CreateWebRequest(jsonBody);
                
                await webRequest.SendWebRequest().WithCancellation(_currentOperationCts.Token);
                
                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    string error = webRequest.error;
                    DebugLogger.LogError($"TTS request failed: {error}", LogCategory.Speech);
                    
                    return new TTSResult
                    {
                        Success = false,
                        ErrorMessage = error
                    };
                }
                
                byte[] audioData = webRequest.downloadHandler.data;
                AudioClip clip = await ConvertToAudioClipAsync(audioData, _currentOperationCts.Token);
                
                if (clip == null)
                {
                    return new TTSResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to decode audio"
                    };
                }
                
                // Cache the result
                if (_settings.EnableCaching)
                {
                    CacheAudioClip(textHash, clip);
                }
                
                DebugLogger.LogInfo($"TTS synthesis complete: {clip.length:F2}s", LogCategory.Speech);
                
                return new TTSResult
                {
                    Success = true,
                    AudioClip = clip,
                    Duration = clip.length
                };
            }
            catch (OperationCanceledException)
            {
                return new TTSResult
                {
                    Success = false,
                    ErrorMessage = "Operation cancelled"
                };
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, LogCategory.Speech);
                return new TTSResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
            finally
            {
                _currentOperationCts?.Dispose();
                _currentOperationCts = null;
            }
        }
        
        public void Stop()
        {
            _currentOperationCts?.Cancel();
            
            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Stop();
            }
            
            _isSpeaking = false;
            _playbackProgress = 0f;
        }
        
        public void Pause()
        {
            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Pause();
            }
        }
        
        public void Resume()
        {
            if (_audioSource != null && !_audioSource.isPlaying)
            {
                _audioSource.UnPause();
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Set the audio source to use for playback.
        /// </summary>
        public void SetAudioSource(AudioSource audioSource)
        {
            _audioSource = audioSource;
        }
        
        /// <summary>
        /// Clear the audio cache.
        /// </summary>
        public void ClearCache()
        {
            foreach (var clip in _audioCache.Values)
            {
                if (clip != null)
                {
                    UnityEngine.Object.Destroy(clip);
                }
            }
            _audioCache.Clear();
        }
        
        #endregion
        
        #region Private Methods
        
        private UnityWebRequest CreateWebRequest(string jsonBody)
        {
            string url = $"{_settings.GetTTSEndpoint()}?output_format={_settings.OutputFormatString}";
            
            var webRequest = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("xi-api-key", _settings.ApiKey);
            
            webRequest.timeout = (int)_settings.TimeoutSeconds;
            
            return webRequest;
        }
        
        private string BuildRequestBody(string text)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"text\":{JsonEscape(text)},");
            sb.Append($"\"model_id\":\"{_settings.ModelId}\",");
            sb.Append("\"voice_settings\":{");
            sb.Append($"\"stability\":{_settings.Stability},");
            sb.Append($"\"similarity_boost\":{_settings.SimilarityBoost},");
            sb.Append($"\"style\":{_settings.Style},");
            sb.Append($"\"use_speaker_boost\":{_settings.UseSpeakerBoost.ToString().ToLower()}");
            sb.Append("}}");
            
            return sb.ToString();
        }
        
        private async UniTask<AudioClip> ConvertToAudioClipAsync(byte[] audioData, CancellationToken cancellationToken)
        {
            // For MP3 format, we need to use a temporary file approach
            // In production, consider using a proper audio decoding library
            
            string tempPath = System.IO.Path.Combine(Application.temporaryCachePath, $"tts_{Guid.NewGuid()}.mp3");
            
            try
            {
                await System.IO.File.WriteAllBytesAsync(tempPath, audioData, cancellationToken);
                
                using var www = UnityWebRequestMultimedia.GetAudioClip("file://" + tempPath, AudioType.MPEG);
                await www.SendWebRequest().WithCancellation(cancellationToken);
                
                if (www.result == UnityWebRequest.Result.Success)
                {
                    return DownloadHandlerAudioClip.GetContent(www);
                }
                
                DebugLogger.LogWarning($"Failed to decode audio: {www.error}", LogCategory.Speech);
                return null;
            }
            finally
            {
                // Clean up temp file
                if (System.IO.File.Exists(tempPath))
                {
                    try { System.IO.File.Delete(tempPath); } catch { }
                }
            }
        }
        
        private async UniTask PlayAudioAsync(AudioClip clip, CancellationToken cancellationToken)
        {
            if (_audioSource == null || clip == null) return;
            
            _isSpeaking = true;
            _playbackProgress = 0f;
            
            OnSpeechStarted?.Invoke();
            
            GameEvents.RaiseTTSStarted(new TTSEventArgs
            {
                Text = "",
                AudioClip = clip,
                Duration = clip.length
            });
            
            _audioSource.clip = clip;
            _audioSource.Play();
            
            // Simulate viseme data based on audio amplitude
            var visemeTask = SimulateVisemesAsync(clip, cancellationToken);
            
            float startTime = Time.time;
            
            while (_audioSource.isPlaying && !cancellationToken.IsCancellationRequested)
            {
                _playbackProgress = _audioSource.time / clip.length;
                await UniTask.Yield(cancellationToken);
            }
            
            _isSpeaking = false;
            _playbackProgress = 1f;
            
            OnSpeechCompleted?.Invoke();
            
            GameEvents.RaiseTTSCompleted(new TTSEventArgs
            {
                Text = "",
                AudioClip = clip,
                Duration = clip.length
            });
        }
        
        private async UniTask SimulateVisemesAsync(AudioClip clip, CancellationToken cancellationToken)
        {
            // Simple volume-based viseme simulation
            // In production, use proper phoneme analysis or ElevenLabs viseme data
            
            float[] samples = new float[256];
            float interval = 0.05f; // 50ms intervals
            float time = 0f;
            
            while (time < clip.length && !cancellationToken.IsCancellationRequested)
            {
                if (_audioSource != null && _audioSource.isPlaying)
                {
                    _audioSource.GetOutputData(samples, 0);
                    
                    // Calculate RMS amplitude
                    float sum = 0f;
                    for (int i = 0; i < samples.Length; i++)
                    {
                        sum += samples[i] * samples[i];
                    }
                    float rms = Mathf.Sqrt(sum / samples.Length);
                    
                    // Map amplitude to viseme (simplified)
                    int visemeId = Mathf.Clamp(Mathf.RoundToInt(rms * 20), 0, 15);
                    
                    OnVisemeReceived?.Invoke(new VisemeData(time, visemeId, rms));
                }
                
                await UniTask.Delay(TimeSpan.FromSeconds(interval), cancellationToken: cancellationToken);
                time += interval;
            }
        }
        
        private void CacheAudioClip(int hash, AudioClip clip)
        {
            _audioCache[hash] = clip;
            
            // Simple cache size management - remove oldest if too large
            if (_audioCache.Count > 100)
            {
                var enumerator = _audioCache.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    int keyToRemove = enumerator.Current.Key;
                    if (_audioCache.TryGetValue(keyToRemove, out AudioClip oldClip))
                    {
                        UnityEngine.Object.Destroy(oldClip);
                    }
                    _audioCache.Remove(keyToRemove);
                }
            }
        }
        
        private string JsonEscape(string text)
        {
            if (string.IsNullOrEmpty(text)) return "\"\"";
            
            StringBuilder sb = new StringBuilder("\"");
            
            foreach (char c in text)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(c); break;
                }
            }
            
            sb.Append("\"");
            return sb.ToString();
        }
        
        #endregion
    }
    
    #endregion
}
