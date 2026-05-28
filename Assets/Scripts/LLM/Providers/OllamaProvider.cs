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
    #region Ollama Provider Implementation
    
    /// <summary>
    /// LLM provider implementation for local Ollama server.
    /// Supports running local LLMs like Llama, Mistral, etc.
    /// </summary>
    public class OllamaProvider : ILLMProvider
    {
        #region Private Fields
        
        private readonly LLMSettings _settings;
        private CancellationTokenSource _currentGenerationCts;
        private bool _isGenerating;
        private bool _isAvailable;
        
        #endregion
        
        #region Properties
        
        public string ProviderName => "Ollama";
        public bool IsAvailable => _isAvailable;
        public bool IsGenerating => _isGenerating;
        
        #endregion
        
        #region Constructor
        
        public OllamaProvider(LLMSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _isAvailable = false;
            
            DebugLogger.LogInfo($"OllamaProvider created with model: {settings.OllamaModel}", LogCategory.LLM);
        }
        
        #endregion
        
        #region IService Implementation
        
        public async void Initialize()
        {
            DebugLogger.LogInfo("OllamaProvider initializing...", LogCategory.LLM);
            _isAvailable = await TestConnectionAsync();
            
            if (_isAvailable)
            {
                DebugLogger.LogInfo("OllamaProvider connected successfully", LogCategory.LLM);
            }
            else
            {
                DebugLogger.LogWarning("OllamaProvider connection failed - ensure Ollama is running", LogCategory.LLM);
            }
        }
        
        public void Shutdown()
        {
            CancelGeneration();
            DebugLogger.LogInfo("OllamaProvider shutdown", LogCategory.LLM);
        }
        
        #endregion
        
        #region ILLMProvider Implementation
        
        public async UniTask<LLMResponse> GenerateAsync(LLMRequest request, CancellationToken cancellationToken = default)
        {
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
            _isGenerating = true;
            _currentGenerationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            float startTime = Time.realtimeSinceStartup;
            StringBuilder fullResponse = new StringBuilder();
            
            try
            {
                string jsonBody = BuildRequestBody(request, streaming: true);
                
                GameEvents.RaiseLLMRequestSent(request.Messages[^1].Content);
                
                using var webRequest = CreateWebRequest(jsonBody);
                
                var operation = webRequest.SendWebRequest();
                
                int lastProcessedLength = 0;
                
                while (!operation.isDone)
                {
                    _currentGenerationCts.Token.ThrowIfCancellationRequested();
                    
                    string currentData = webRequest.downloadHandler?.text ?? "";
                    
                    if (currentData.Length > lastProcessedLength)
                    {
                        string newData = currentData.Substring(lastProcessedLength);
                        lastProcessedLength = currentData.Length;
                        
                        ProcessStreamingData(newData, chunk =>
                        {
                            fullResponse.Append(chunk);
                            onChunkReceived?.Invoke(chunk);
                            GameEvents.RaiseLLMStreamChunkReceived(chunk);
                        });
                    }
                    
                    await UniTask.Delay(50, cancellationToken: _currentGenerationCts.Token);
                }
                
                // Process remaining data
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
            try
            {
                string url = $"{_settings.OllamaUrl}/api/tags";
                
                using var webRequest = UnityWebRequest.Get(url);
                webRequest.timeout = 5;
                
                await webRequest.SendWebRequest();
                
                bool success = webRequest.result == UnityWebRequest.Result.Success;
                _isAvailable = success;
                
                return success;
            }
            catch
            {
                _isAvailable = false;
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
            string url = $"{_settings.OllamaUrl}/api/generate";
            var webRequest = new UnityWebRequest(url, "POST");
            
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.timeout = (int)_settings.TimeoutSeconds;
            
            return webRequest;
        }
        
        private string BuildRequestBody(LLMRequest request, bool streaming)
        {
            var settings = request.Settings ?? _settings.CreateRuntimeSettings();
            
            // Combine system prompt and messages into a single prompt
            StringBuilder promptBuilder = new StringBuilder();
            
            if (!string.IsNullOrEmpty(request.SystemPrompt))
            {
                promptBuilder.AppendLine($"System: {request.SystemPrompt}");
                promptBuilder.AppendLine();
            }
            
            foreach (var msg in request.Messages)
            {
                string role = msg.Role == "user" ? "User" : 
                              msg.Role == "assistant" ? "Assistant" : msg.Role;
                promptBuilder.AppendLine($"{role}: {msg.Content}");
            }
            
            promptBuilder.AppendLine("Assistant:");
            
            string prompt = promptBuilder.ToString();
            
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"model\":\"{_settings.OllamaModel}\",");
            sb.Append($"\"prompt\":{JsonEscape(prompt)},");
            sb.Append($"\"stream\":{streaming.ToString().ToLower()},");
            sb.Append("\"options\":{");
            sb.Append($"\"temperature\":{settings.Temperature},");
            sb.Append($"\"num_predict\":{settings.MaxTokens},");
            sb.Append($"\"top_p\":{settings.TopP}");
            sb.Append("}}");
            
            return sb.ToString();
        }
        
        private LLMResponse ParseResponse(string json)
        {
            try
            {
                // Parse non-streaming response
                int responseIdx = json.IndexOf("\"response\":\"");
                if (responseIdx < 0)
                {
                    if (json.Contains("\"error\""))
                    {
                        return new LLMResponse
                        {
                            Success = false,
                            ErrorMessage = "Ollama returned an error"
                        };
                    }
                    
                    return new LLMResponse
                    {
                        Success = false,
                        ErrorMessage = "Failed to parse Ollama response"
                    };
                }
                
                responseIdx += 12;
                int responseEnd = FindJsonStringEnd(json, responseIdx);
                string content = json.Substring(responseIdx, responseEnd - responseIdx);
                content = UnescapeJson(content);
                
                return new LLMResponse
                {
                    Success = true,
                    Content = content.Trim(),
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
                
                try
                {
                    // Ollama streaming returns JSON objects line by line
                    int responseIdx = line.IndexOf("\"response\":\"");
                    if (responseIdx < 0) continue;
                    
                    responseIdx += 12;
                    int responseEnd = FindJsonStringEnd(line, responseIdx);
                    string content = line.Substring(responseIdx, responseEnd - responseIdx);
                    content = UnescapeJson(content);
                    
                    if (!string.IsNullOrEmpty(content))
                    {
                        onChunk(content);
                    }
                }
                catch
                {
                    // Skip malformed lines
                }
            }
        }
        
        private int FindJsonStringEnd(string json, int start)
        {
            for (int i = start; i < json.Length; i++)
            {
                if (json[i] == '\\')
                {
                    i++;
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
