using System;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using InteractiveNPC.Core.Events;
using InteractiveNPC.Core.Utilities;
using InteractiveNPC.LLM.Data;
using InteractiveNPC.LLM.Interfaces;

namespace InteractiveNPC.LLM.Providers
{
    #region OpenAI Provider Implementation
    
    /// <summary>
    /// LLM provider implementation for OpenAI's GPT models.
    /// Supports both standard and streaming responses.
    /// </summary>
    public class OpenAIProvider : ILLMProvider
    {
        #region Private Fields
        
        private readonly LLMSettings _settings;
        private CancellationTokenSource _currentGenerationCts;
        private bool _isGenerating;
        
        #endregion
        
        #region Properties
        
        public string ProviderName => "OpenAI";
        public bool IsAvailable => !string.IsNullOrEmpty(_settings?.OpenAIApiKey);
        public bool IsGenerating => _isGenerating;
        
        #endregion
        
        #region Constructor
        
        public OpenAIProvider(LLMSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            DebugLogger.LogInfo($"OpenAIProvider created with model: {settings.OpenAIModelName}", LogCategory.LLM);
        }
        
        #endregion
        
        #region IService Implementation
        
        public void Initialize()
        {
            DebugLogger.LogInfo("OpenAIProvider initialized", LogCategory.LLM);
        }
        
        public void Shutdown()
        {
            CancelGeneration();
            DebugLogger.LogInfo("OpenAIProvider shutdown", LogCategory.LLM);
        }
        
        #endregion
        
        #region ILLMProvider Implementation
        
        public async UniTask<LLMResponse> GenerateAsync(LLMRequest request, CancellationToken cancellationToken = default)
        {
            if (!IsAvailable)
            {
                return new LLMResponse
                {
                    Success = false,
                    ErrorMessage = "OpenAI API key not configured"
                };
            }
            
            _isGenerating = true;
            _currentGenerationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            float startTime = Time.realtimeSinceStartup;
            
            try
            {
                string jsonBody = BuildRequestBody(request, streaming: false);
                
                GameEvents.RaiseLLMRequestSent(request.Messages[^1].Content);
                
                using var webRequest = CreateWebRequest(jsonBody);
                
                await webRequest.SendWebRequest().WithCancellation(_currentGenerationCts.Token);
                
                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    string error = webRequest.error;
                    GameEvents.RaiseLLMRequestFailed(new Exception(error));
                    
                    return new LLMResponse
                    {
                        Success = false,
                        ErrorMessage = error,
                        GenerationTimeSeconds = Time.realtimeSinceStartup - startTime
                    };
                }
                
                var response = ParseResponse(webRequest.downloadHandler.text);
                response.GenerationTimeSeconds = Time.realtimeSinceStartup - startTime;
                
                if (response.Success)
                {
                    GameEvents.RaiseLLMResponseComplete(response.Content);
                }
                
                return response;
            }
            catch (OperationCanceledException)
            {
                return new LLMResponse
                {
                    Success = false,
                    ErrorMessage = "Generation cancelled",
                    GenerationTimeSeconds = Time.realtimeSinceStartup - startTime
                };
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, LogCategory.LLM);
                GameEvents.RaiseLLMRequestFailed(ex);
                
                return new LLMResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    GenerationTimeSeconds = Time.realtimeSinceStartup - startTime
                };
            }
            finally
            {
                _isGenerating = false;
                _currentGenerationCts?.Dispose();
                _currentGenerationCts = null;
            }
        }
        
        public async UniTask<LLMResponse> GenerateStreamingAsync(
            LLMRequest request,
            Action<string> onChunkReceived,
            CancellationToken cancellationToken = default)
        {
            if (!IsAvailable)
            {
                return new LLMResponse
                {
                    Success = false,
                    ErrorMessage = "OpenAI API key not configured"
                };
            }
            
            _isGenerating = true;
            _currentGenerationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            float startTime = Time.realtimeSinceStartup;
            StringBuilder fullResponse = new StringBuilder();
            
            try
            {
                string jsonBody = BuildRequestBody(request, streaming: true);
                
                GameEvents.RaiseLLMRequestSent(request.Messages[^1].Content);
                
                using var webRequest = CreateWebRequest(jsonBody);
                
                // Start the request
                var operation = webRequest.SendWebRequest();
                
                int lastProcessedLength = 0;
                
                // Process streaming chunks
                while (!operation.isDone)
                {
                    _currentGenerationCts.Token.ThrowIfCancellationRequested();
                    
                    string currentData = webRequest.downloadHandler?.text ?? "";
                    
                    if (currentData.Length > lastProcessedLength)
                    {
                        string newData = currentData.Substring(lastProcessedLength);
                        lastProcessedLength = currentData.Length;
                        
                        // Parse SSE data
                        ProcessStreamingData(newData, chunk =>
                        {
                            fullResponse.Append(chunk);
                            onChunkReceived?.Invoke(chunk);
                            GameEvents.RaiseLLMStreamChunkReceived(chunk);
                        });
                    }
                    
                    await UniTask.Delay(50, cancellationToken: _currentGenerationCts.Token);
                }
                
                // Process any remaining data
                string finalData = webRequest.downloadHandler?.text ?? "";
                if (finalData.Length > lastProcessedLength)
                {
                    string remainingData = finalData.Substring(lastProcessedLength);
                    ProcessStreamingData(remainingData, chunk =>
                    {
                        fullResponse.Append(chunk);
                        onChunkReceived?.Invoke(chunk);
                    });
                }
                
                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    string error = webRequest.error;
                    GameEvents.RaiseLLMRequestFailed(new Exception(error));
                    
                    return new LLMResponse
                    {
                        Success = false,
                        ErrorMessage = error,
                        GenerationTimeSeconds = Time.realtimeSinceStartup - startTime
                    };
                }
                
                string content = fullResponse.ToString();
                GameEvents.RaiseLLMResponseComplete(content);
                
                return new LLMResponse
                {
                    Success = true,
                    Content = content,
                    GenerationTimeSeconds = Time.realtimeSinceStartup - startTime,
                    FinishReason = "stop"
                };
            }
            catch (OperationCanceledException)
            {
                return new LLMResponse
                {
                    Success = false,
                    Content = fullResponse.ToString(),
                    ErrorMessage = "Generation cancelled",
                    GenerationTimeSeconds = Time.realtimeSinceStartup - startTime
                };
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, LogCategory.LLM);
                GameEvents.RaiseLLMRequestFailed(ex);
                
                return new LLMResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    GenerationTimeSeconds = Time.realtimeSinceStartup - startTime
                };
            }
            finally
            {
                _isGenerating = false;
                _currentGenerationCts?.Dispose();
                _currentGenerationCts = null;
            }
        }
        
        public async UniTask<bool> TestConnectionAsync()
        {
            if (!IsAvailable) return false;
            
            try
            {
                var request = new LLMRequest
                {
                    SystemPrompt = "You are a test assistant.",
                    Messages = { LLMMessage.User("Say 'OK' if you can hear me.") },
                    Settings = new LLMRuntimeSettings
                    {
                        MaxTokens = 10,
                        Temperature = 0f
                    }
                };
                
                var response = await GenerateAsync(request);
                return response.Success;
            }
            catch
            {
                return false;
            }
        }
        
        public void CancelGeneration()
        {
            _currentGenerationCts?.Cancel();
        }
        
        #endregion
        
        #region Private Methods
        
        private UnityWebRequest CreateWebRequest(string jsonBody)
        {
            var webRequest = new UnityWebRequest(_settings.GetEndpointUrl(), "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("Authorization", $"Bearer {_settings.OpenAIApiKey}");
            
            webRequest.timeout = (int)_settings.TimeoutSeconds;
            
            return webRequest;
        }
        
        private string BuildRequestBody(LLMRequest request, bool streaming)
        {
            var settings = request.Settings ?? _settings.CreateRuntimeSettings();
            
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            
            // Model
            sb.Append($"\"model\":\"{_settings.OpenAIModelName}\",");
            
            // Messages
            sb.Append("\"messages\":[");
            
            // System message
            if (!string.IsNullOrEmpty(request.SystemPrompt))
            {
                sb.Append($"{{\"role\":\"system\",\"content\":{JsonEscape(request.SystemPrompt)}}},");
            }
            
            // Conversation messages
            for (int i = 0; i < request.Messages.Count; i++)
            {
                var msg = request.Messages[i];
                sb.Append($"{{\"role\":\"{msg.Role}\",\"content\":{JsonEscape(msg.Content)}}}");
                if (i < request.Messages.Count - 1) sb.Append(",");
            }
            
            sb.Append("],");
            
            // Parameters
            sb.Append($"\"temperature\":{settings.Temperature},");
            sb.Append($"\"max_tokens\":{settings.MaxTokens},");
            sb.Append($"\"top_p\":{settings.TopP},");
            sb.Append($"\"frequency_penalty\":{settings.FrequencyPenalty},");
            sb.Append($"\"presence_penalty\":{settings.PresencePenalty},");
            sb.Append($"\"stream\":{streaming.ToString().ToLower()}");
            
            sb.Append("}");
            
            return sb.ToString();
        }
        
        private LLMResponse ParseResponse(string json)
        {
            try
            {
                // Simple JSON parsing for the response
                // In production, use a proper JSON library
                
                int contentStart = json.IndexOf("\"content\":\"");
                if (contentStart < 0)
                {
                    // Check for error
                    if (json.Contains("\"error\""))
                    {
                        int errorStart = json.IndexOf("\"message\":\"") + 11;
                        int errorEnd = json.IndexOf("\"", errorStart);
                        string errorMsg = json.Substring(errorStart, errorEnd - errorStart);
                        
                        return new LLMResponse
                        {
                            Success = false,
                            ErrorMessage = errorMsg
                        };
                    }
                    
                    return new LLMResponse
                    {
                        Success = false,
                        ErrorMessage = "Failed to parse response"
                    };
                }
                
                contentStart += 11;
                int contentEnd = FindJsonStringEnd(json, contentStart);
                string content = json.Substring(contentStart, contentEnd - contentStart);
                content = UnescapeJson(content);
                
                // Parse tokens used
                int tokensUsed = 0;
                int totalTokensIdx = json.IndexOf("\"total_tokens\":");
                if (totalTokensIdx > 0)
                {
                    int start = totalTokensIdx + 15;
                    int end = json.IndexOfAny(new[] { ',', '}' }, start);
                    if (int.TryParse(json.Substring(start, end - start).Trim(), out int tokens))
                    {
                        tokensUsed = tokens;
                    }
                }
                
                return new LLMResponse
                {
                    Success = true,
                    Content = content,
                    TokensUsed = tokensUsed,
                    FinishReason = "stop"
                };
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, LogCategory.LLM);
                return new LLMResponse
                {
                    Success = false,
                    ErrorMessage = $"Parse error: {ex.Message}"
                };
            }
        }
        
        private void ProcessStreamingData(string data, Action<string> onChunk)
        {
            string[] lines = data.Split('\n');
            
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!line.StartsWith("data: ")) continue;
                
                string jsonData = line.Substring(6).Trim();
                
                if (jsonData == "[DONE]") continue;
                
                try
                {
                    // Extract content delta
                    int deltaIdx = jsonData.IndexOf("\"delta\":{");
                    if (deltaIdx < 0) continue;
                    
                    int contentIdx = jsonData.IndexOf("\"content\":\"", deltaIdx);
                    if (contentIdx < 0) continue;
                    
                    contentIdx += 11;
                    int contentEnd = FindJsonStringEnd(jsonData, contentIdx);
                    string content = jsonData.Substring(contentIdx, contentEnd - contentIdx);
                    content = UnescapeJson(content);
                    
                    if (!string.IsNullOrEmpty(content))
                    {
                        onChunk(content);
                    }
                }
                catch
                {
                    // Skip malformed chunks
                }
            }
        }
        
        private int FindJsonStringEnd(string json, int start)
        {
            for (int i = start; i < json.Length; i++)
            {
                if (json[i] == '\\')
                {
                    i++; // Skip escaped character
                    continue;
                }
                if (json[i] == '"')
                {
                    return i;
                }
            }
            return json.Length;
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
        
        private string UnescapeJson(string text)
        {
            return text
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
        }
        
        #endregion
    }
    
    #endregion
}
