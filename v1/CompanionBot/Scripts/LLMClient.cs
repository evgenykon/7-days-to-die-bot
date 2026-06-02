using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CompanionBot
{
    public class LLMClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _endpoint;
        private readonly string _model;
        private readonly string _embeddingModel;
        private readonly float _temperature;
        private readonly int _maxTokens;

        private DateTime _lastRequestTime = DateTime.MinValue;
        private readonly TimeSpan _cooldown;

        public LLMClient(string endpoint, string model, string embeddingModel, float temperature, int maxTokens, int cooldownSeconds)
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _endpoint = endpoint.TrimEnd('/');
            _model = model;
            _embeddingModel = embeddingModel ?? model;
            _temperature = temperature;
            _maxTokens = maxTokens;
            _cooldown = TimeSpan.FromSeconds(cooldownSeconds);
        }

        public async Task<string> GenerateResponse(string systemPrompt, string userMessage, List<string> relevantMemories = null)
        {
            if (DateTime.Now - _lastRequestTime < _cooldown)
            {
                Log.Out("[CompanionBot] LLM request skipped (cooldown)");
                return null;
            }

            try
            {
                var messages = new List<ChatMessage>
                {
                    new ChatMessage { Role = "system", Content = systemPrompt }
                };

                if (relevantMemories != null && relevantMemories.Count > 0)
                {
                    var memoryContext = "Relevant memories:\n" + string.Join("\n", relevantMemories);
                    messages.Add(new ChatMessage { Role = "system", Content = memoryContext });
                }

                messages.Add(new ChatMessage { Role = "user", Content = userMessage });

                var request = new ChatCompletionRequest
                {
                    Model = _model,
                    Messages = messages,
                    Temperature = _temperature,
                    MaxTokens = _maxTokens
                };

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_endpoint}/chat/completions", content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var completion = JsonConvert.DeserializeObject<ChatCompletionResponse>(responseJson);

                _lastRequestTime = DateTime.Now;

                return completion?.Choices?[0]?.Message?.Content;
            }
            catch (Exception ex)
            {
                Log.Error($"[CompanionBot] LLM request failed: {ex.Message}");
                return null;
            }
        }

        public async Task<float[]> GenerateEmbedding(string text)
        {
            try
            {
                var request = new EmbeddingRequest
                {
                    Model = _embeddingModel,
                    Input = text
                };

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_endpoint}/embeddings", content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var embedding = JsonConvert.DeserializeObject<EmbeddingResponse>(responseJson);

                return embedding?.Data?[0]?.Embedding;
            }
            catch (Exception ex)
            {
                Log.Error($"[CompanionBot] Embedding request failed: {ex.Message}");
                return null;
            }
        }

        private class ChatMessage
        {
            [JsonProperty("role")]
            public string Role { get; set; }

            [JsonProperty("content")]
            public string Content { get; set; }
        }

        private class ChatCompletionRequest
        {
            [JsonProperty("model")]
            public string Model { get; set; }

            [JsonProperty("messages")]
            public List<ChatMessage> Messages { get; set; }

            [JsonProperty("temperature")]
            public float Temperature { get; set; }

            [JsonProperty("max_tokens")]
            public int MaxTokens { get; set; }
        }

        private class ChatCompletionResponse
        {
            [JsonProperty("choices")]
            public List<Choice> Choices { get; set; }
        }

        private class Choice
        {
            [JsonProperty("message")]
            public ChatMessage Message { get; set; }
        }

        private class EmbeddingRequest
        {
            [JsonProperty("model")]
            public string Model { get; set; }

            [JsonProperty("input")]
            public string Input { get; set; }
        }

        private class EmbeddingResponse
        {
            [JsonProperty("data")]
            public List<EmbeddingData> Data { get; set; }
        }

        private class EmbeddingData
        {
            [JsonProperty("embedding")]
            public float[] Embedding { get; set; }
        }
    }
}
