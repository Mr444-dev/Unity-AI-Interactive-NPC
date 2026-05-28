using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using InteractiveNPC.Core.Services;

namespace InteractiveNPC.Speech.Interfaces
{
    #region Speech-to-Text Interface
    
    /// <summary>
    /// Interface for Speech-to-Text provider implementations.
    /// </summary>
    public interface ISpeechToText : IService
    {
        #region Properties
        
        /// <summary>
        /// Whether speech recognition is available on this platform.
        /// </summary>
        bool IsAvailable { get; }
        
        /// <summary>
        /// Whether currently recording/recognizing speech.
        /// </summary>
        bool IsListening { get; }
        
        /// <summary>
        /// Current audio input level (0-1).
        /// </summary>
        float AudioLevel { get; }
        
        #endregion
        
        #region Methods
        
        /// <summary>
        /// Start listening for speech input.
        /// </summary>
        UniTask StartListeningAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Stop listening and get the final recognized text.
        /// </summary>
        UniTask<SpeechRecognitionResult> StopListeningAsync();
        
        /// <summary>
        /// Cancel any ongoing recognition.
        /// </summary>
        void Cancel();
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when interim results are available.
        /// </summary>
        event System.Action<string> OnInterimResult;
        
        /// <summary>
        /// Fired when audio level changes.
        /// </summary>
        event System.Action<float> OnAudioLevelChanged;
        
        #endregion
    }
    
    #endregion
    
    #region Text-to-Speech Interface
    
    /// <summary>
    /// Interface for Text-to-Speech provider implementations.
    /// </summary>
    public interface ITextToSpeech : IService
    {
        #region Properties
        
        /// <summary>
        /// Whether TTS is available.
        /// </summary>
        bool IsAvailable { get; }
        
        /// <summary>
        /// Whether currently synthesizing or playing speech.
        /// </summary>
        bool IsSpeaking { get; }
        
        /// <summary>
        /// Current playback progress (0-1).
        /// </summary>
        float PlaybackProgress { get; }
        
        #endregion
        
        #region Methods
        
        /// <summary>
        /// Synthesize and play speech from text.
        /// </summary>
        UniTask<TTSResult> SpeakAsync(string text, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Synthesize speech and return the audio clip without playing.
        /// </summary>
        UniTask<TTSResult> SynthesizeAsync(string text, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Stop any ongoing speech synthesis/playback.
        /// </summary>
        void Stop();
        
        /// <summary>
        /// Pause playback.
        /// </summary>
        void Pause();
        
        /// <summary>
        /// Resume playback.
        /// </summary>
        void Resume();
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when speech synthesis starts.
        /// </summary>
        event System.Action OnSpeechStarted;
        
        /// <summary>
        /// Fired when speech playback completes.
        /// </summary>
        event System.Action OnSpeechCompleted;
        
        /// <summary>
        /// Fired with viseme data for lip sync.
        /// </summary>
        event System.Action<VisemeData> OnVisemeReceived;
        
        #endregion
    }
    
    #endregion
    
    #region Result Classes
    
    /// <summary>
    /// Result of speech recognition.
    /// </summary>
    public class SpeechRecognitionResult
    {
        public bool Success { get; set; }
        public string Text { get; set; }
        public float Confidence { get; set; }
        public string ErrorMessage { get; set; }
        public float Duration { get; set; }
    }
    
    /// <summary>
    /// Result of text-to-speech synthesis.
    /// </summary>
    public class TTSResult
    {
        public bool Success { get; set; }
        public AudioClip AudioClip { get; set; }
        public float Duration { get; set; }
        public string ErrorMessage { get; set; }
        public VisemeData[] Visemes { get; set; }
    }
    
    /// <summary>
    /// Viseme data for lip synchronization.
    /// </summary>
    [System.Serializable]
    public struct VisemeData
    {
        public float Time;
        public int VisemeId;
        public float Weight;
        
        public VisemeData(float time, int visemeId, float weight = 1f)
        {
            Time = time;
            VisemeId = visemeId;
            Weight = weight;
        }
    }
    
    #endregion
}
