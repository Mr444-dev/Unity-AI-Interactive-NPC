using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using InteractiveNPC.Core.Events;
using InteractiveNPC.Core.Utilities;
using InteractiveNPC.Speech.Data;
using InteractiveNPC.Speech.Interfaces;

namespace InteractiveNPC.Speech.STT
{
    #region Unity Microphone Speech Recognizer
    
    /// <summary>
    /// Speech-to-Text implementation using Unity's microphone input.
    /// For actual transcription, this can be extended to use cloud services or local models.
    /// Currently provides the audio recording infrastructure.
    /// </summary>
    public class MicrophoneSpeechRecognizer : ISpeechToText
    {
        #region Private Fields
        
        private readonly STTSettings _settings;
        private AudioClip _recordingClip;
        private string _microphoneDevice;
        private bool _isListening;
        private float _audioLevel;
        private float _silenceTimer;
        private CancellationTokenSource _listeningCts;
        
        #endregion
        
        #region Events
        
        public event Action<string> OnInterimResult;
        public event Action<float> OnAudioLevelChanged;
        
        #endregion
        
        #region Properties
        
        public bool IsAvailable => Microphone.devices.Length > 0;
        public bool IsListening => _isListening;
        public float AudioLevel => _audioLevel;
        
        #endregion
        
        #region Constructor
        
        public MicrophoneSpeechRecognizer(STTSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            DebugLogger.LogInfo("MicrophoneSpeechRecognizer created", LogCategory.Speech);
        }
        
        #endregion
        
        #region IService Implementation
        
        public void Initialize()
        {
            if (Microphone.devices.Length > 0)
            {
                _microphoneDevice = Microphone.devices[0];
                DebugLogger.LogInfo($"Microphone initialized: {_microphoneDevice}", LogCategory.Speech);
            }
            else
            {
                DebugLogger.LogWarning("No microphone devices found", LogCategory.Speech);
            }
        }
        
        public void Shutdown()
        {
            Cancel();
            DebugLogger.LogInfo("MicrophoneSpeechRecognizer shutdown", LogCategory.Speech);
        }
        
        #endregion
        
        #region ISpeechToText Implementation
        
        public async UniTask StartListeningAsync(CancellationToken cancellationToken = default)
        {
            if (!IsAvailable)
            {
                DebugLogger.LogError("No microphone available", LogCategory.Speech);
                return;
            }
            
            if (_isListening)
            {
                DebugLogger.LogWarning("Already listening", LogCategory.Speech);
                return;
            }
            
            _listeningCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _isListening = true;
            _silenceTimer = 0f;
            
            // Start recording
            int maxDurationSamples = Mathf.CeilToInt(_settings.MaxRecordingDuration * _settings.SampleRate);
            _recordingClip = Microphone.Start(_microphoneDevice, false, (int)_settings.MaxRecordingDuration + 1, _settings.SampleRate);
            
            GameEvents.RaiseSpeechRecognitionStarted();
            DebugLogger.LogInfo("Started listening", LogCategory.Speech);
            
            // Monitor audio levels
            await MonitorAudioLevelAsync(_listeningCts.Token);
        }
        
        public async UniTask<SpeechRecognitionResult> StopListeningAsync()
        {
            if (!_isListening)
            {
                return new SpeechRecognitionResult
                {
                    Success = false,
                    ErrorMessage = "Not currently listening"
                };
            }
            
            _isListening = false;
            
            int recordPosition = Microphone.GetPosition(_microphoneDevice);
            Microphone.End(_microphoneDevice);
            
            GameEvents.RaiseSpeechRecognitionEnded();
            DebugLogger.LogInfo($"Stopped listening at position: {recordPosition}", LogCategory.Speech);
            
            if (recordPosition == 0 || _recordingClip == null)
            {
                return new SpeechRecognitionResult
                {
                    Success = false,
                    ErrorMessage = "No audio recorded"
                };
            }
            
            // Extract audio data
            float[] audioData = new float[recordPosition];
            _recordingClip.GetData(audioData, 0);
            
            // In a full implementation, this audio would be sent to a transcription service
            // For now, we'll return a placeholder result
            // You would integrate with:
            // - Whisper API (OpenAI)
            // - Google Cloud Speech-to-Text
            // - Azure Speech Services
            // - Local Whisper model
            
            string transcribedText = await TranscribeAudioAsync(audioData, _listeningCts?.Token ?? default);
            
            _listeningCts?.Dispose();
            _listeningCts = null;
            
            if (!string.IsNullOrEmpty(transcribedText))
            {
                GameEvents.RaiseSpeechRecognized(new SpeechRecognitionEventArgs
                {
                    RecognizedText = transcribedText,
                    Confidence = 0.9f,
                    IsFinal = true
                });
            }
            
            return new SpeechRecognitionResult
            {
                Success = !string.IsNullOrEmpty(transcribedText),
                Text = transcribedText,
                Confidence = 0.9f,
                Duration = (float)recordPosition / _settings.SampleRate
            };
        }
        
        public void Cancel()
        {
            _listeningCts?.Cancel();
            
            if (_isListening)
            {
                Microphone.End(_microphoneDevice);
                _isListening = false;
                GameEvents.RaiseSpeechRecognitionEnded();
            }
            
            _listeningCts?.Dispose();
            _listeningCts = null;
        }
        
        #endregion
        
        #region Private Methods
        
        private async UniTask MonitorAudioLevelAsync(CancellationToken cancellationToken)
        {
            float[] samples = new float[512];
            int lastPosition = 0;
            
            while (_isListening && !cancellationToken.IsCancellationRequested)
            {
                int position = Microphone.GetPosition(_microphoneDevice);
                
                if (position > lastPosition + samples.Length)
                {
                    _recordingClip.GetData(samples, lastPosition);
                    lastPosition = position - samples.Length;
                    
                    // Calculate RMS for audio level
                    float sum = 0f;
                    for (int i = 0; i < samples.Length; i++)
                    {
                        sum += samples[i] * samples[i];
                    }
                    float rms = Mathf.Sqrt(sum / samples.Length);
                    
                    _audioLevel = rms;
                    OnAudioLevelChanged?.Invoke(_audioLevel);
                    
                    // Check for silence
                    if (rms < _settings.SilenceThreshold)
                    {
                        _silenceTimer += 0.05f; // Approximate update interval
                        
                        if (_silenceTimer >= _settings.SilenceDuration)
                        {
                            DebugLogger.LogVerbose("Silence detected, stopping", LogCategory.Speech);
                            await StopListeningAsync();
                            break;
                        }
                    }
                    else
                    {
                        _silenceTimer = 0f;
                    }
                }
                
                // Check for max duration
                float currentDuration = (float)position / _settings.SampleRate;
                if (currentDuration >= _settings.MaxRecordingDuration)
                {
                    DebugLogger.LogVerbose("Max duration reached", LogCategory.Speech);
                    await StopListeningAsync();
                    break;
                }
                
                await UniTask.Delay(50, cancellationToken: cancellationToken);
            }
        }
        
        private async UniTask<string> TranscribeAudioAsync(float[] audioData, CancellationToken cancellationToken)
        {
            // This is a placeholder for actual transcription
            // In a full implementation, you would:
            // 1. Convert float[] to WAV format
            // 2. Send to a transcription API (Whisper, Google, Azure, etc.)
            // 3. Return the transcribed text
            
            // For demonstration, we'll simulate a delay
            await UniTask.Delay(500, cancellationToken: cancellationToken);
            
            // Return empty - in production, integrate with actual STT service
            DebugLogger.LogInfo("Transcription would happen here - integrate with STT API", LogCategory.Speech);
            
            // Placeholder: You can integrate with OpenAI Whisper API here
            // Example endpoint: https://api.openai.com/v1/audio/transcriptions
            
            return ""; // Replace with actual transcription
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Get available microphone devices.
        /// </summary>
        public string[] GetAvailableMicrophones()
        {
            return Microphone.devices;
        }
        
        /// <summary>
        /// Set the microphone device to use.
        /// </summary>
        public void SetMicrophoneDevice(string deviceName)
        {
            if (Array.Exists(Microphone.devices, d => d == deviceName))
            {
                _microphoneDevice = deviceName;
                DebugLogger.LogInfo($"Microphone set to: {deviceName}", LogCategory.Speech);
            }
            else
            {
                DebugLogger.LogWarning($"Microphone device not found: {deviceName}", LogCategory.Speech);
            }
        }
        
        #endregion
    }
    
    #endregion
}
