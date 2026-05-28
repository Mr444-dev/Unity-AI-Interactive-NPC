using System.Threading;
using Cysharp.Threading.Tasks;
using InteractiveNPC.LLM.Data;
using InteractiveNPC.Core.Services;

namespace InteractiveNPC.LLM.Interfaces
{
    #region LLM Provider Interface
    
    /// <summary>
    /// Interface for LLM provider implementations.
    /// Defines the contract for generating text responses from language models.
    /// </summary>
    public interface ILLMProvider : IService
    {
        #region Properties
        
        /// <summary>
        /// Name of the LLM provider.
        /// </summary>
        string ProviderName { get; }
        
        /// <summary>
        /// Whether the provider is currently available.
        /// </summary>
        bool IsAvailable { get; }
        
        /// <summary>
        /// Whether a generation is currently in progress.
        /// </summary>
        bool IsGenerating { get; }
        
        #endregion
        
        #region Methods
        
        /// <summary>
        /// Generate a response from the LLM.
        /// </summary>
        /// <param name="request">The generation request.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The LLM response.</returns>
        UniTask<LLMResponse> GenerateAsync(LLMRequest request, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Generate a response with streaming, calling the callback for each chunk.
        /// </summary>
        /// <param name="request">The generation request.</param>
        /// <param name="onChunkReceived">Callback for each streamed chunk.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The complete LLM response.</returns>
        UniTask<LLMResponse> GenerateStreamingAsync(
            LLMRequest request, 
            System.Action<string> onChunkReceived,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Test the connection to the LLM provider.
        /// </summary>
        /// <returns>True if connection is successful.</returns>
        UniTask<bool> TestConnectionAsync();
        
        /// <summary>
        /// Cancel any ongoing generation.
        /// </summary>
        void CancelGeneration();
        
        #endregion
    }
    
    #endregion
}
