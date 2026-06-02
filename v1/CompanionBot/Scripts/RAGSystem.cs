using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CompanionBot
{
    public class RAGSystem
    {
        private readonly LLMClient _llmClient;
        private readonly string _memoryFilePath;
        private List<MemoryEntry> _memories;

        public RAGSystem(LLMClient llmClient, string memoryFilePath)
        {
            _llmClient = llmClient;
            _memoryFilePath = memoryFilePath;
            _memories = new List<MemoryEntry>();
            LoadMemories();
        }

        public async Task IndexEvent(string eventType, string description, Dictionary<string, string> metadata = null)
        {
            var memory = new MemoryEntry
            {
                Id = Guid.NewGuid().ToString(),
                EventType = eventType,
                Description = description,
                Metadata = metadata ?? new Dictionary<string, string>(),
                Timestamp = DateTime.Now,
                RelevanceScore = 1.0f
            };

            try
            {
                memory.Embedding = await _llmClient.GenerateEmbedding($"{eventType}: {description}");
            }
            catch
            {
            }
            _memories.Add(memory);
            SaveMemories();
            Log.Out($"[CompanionBot] Indexed memory: {eventType} - {description}");
        }

        public async Task<List<string>> RetrieveRelevantMemories(string query, int topK = 3)
        {
            var queryEmbedding = await _llmClient.GenerateEmbedding(query);

            List<MemoryEntry> results;
            if (queryEmbedding != null)
            {
                results = _memories
                    .Where(m => m.Embedding != null)
                    .Select(m => new
                    {
                        Memory = m,
                        Score = CosineSimilarity(queryEmbedding, m.Embedding) * m.RelevanceScore * GetTimeDecay(m.Timestamp)
                    })
                    .OrderByDescending(x => x.Score)
                    .Take(topK)
                    .Select(x => x.Memory)
                    .ToList();
            }
            else
            {
                var keywords = query.ToLower().Split(' ', ',', '.', '!', '?')
                    .Where(w => w.Length > 2)
                    .ToList();

                results = _memories
                    .OrderByDescending(m => GetTimeDecay(m.Timestamp))
                    .Select(m => new
                    {
                        Memory = m,
                        Score = keywords.Count(w => (m.EventType + " " + m.Description).ToLower().Contains(w))
                    })
                    .OrderByDescending(x => x.Score)
                    .Take(topK)
                    .Select(x => x.Memory)
                    .ToList();
            }

            return results.Select(x => $"[{x.EventType}] {x.Description}").ToList();
        }

        public void ApplyMemoryDecay()
        {
            foreach (var memory in _memories)
            {
                var daysSince = (DateTime.Now - memory.Timestamp).TotalDays;
                memory.RelevanceScore = Math.Max(0.1f, (float)(1.0 - daysSince * 0.01));
            }

            _memories = _memories.Where(m => m.RelevanceScore > 0.1f).ToList();
        }

        private float CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                return 0f;

            float dotProduct = 0f;
            float normA = 0f;
            float normB = 0f;

            for (int i = 0; i < a.Length; i++)
            {
                dotProduct += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }

            if (normA == 0 || normB == 0)
                return 0f;

            return dotProduct / (float)(Math.Sqrt(normA) * Math.Sqrt(normB));
        }

        private float GetTimeDecay(DateTime timestamp)
        {
            var hoursSince = (DateTime.Now - timestamp).TotalHours;
            return (float)Math.Max(0.5, 1.0 - hoursSince * 0.001);
        }

        private void LoadMemories()
        {
            try
            {
                if (File.Exists(_memoryFilePath))
                {
                    var json = File.ReadAllText(_memoryFilePath);
                    _memories = JsonConvert.DeserializeObject<List<MemoryEntry>>(json) ?? new List<MemoryEntry>();
                    Log.Out($"[CompanionBot] Loaded {_memories.Count} memories");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[CompanionBot] Failed to load memories: {ex.Message}");
                _memories = new List<MemoryEntry>();
            }
        }

        private void SaveMemories()
        {
            try
            {
                var directory = Path.GetDirectoryName(_memoryFilePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var json = JsonConvert.SerializeObject(_memories, Formatting.Indented);
                File.WriteAllText(_memoryFilePath, json);
            }
            catch (Exception ex)
            {
                Log.Error($"[CompanionBot] Failed to save memories: {ex.Message}");
            }
        }

        private class MemoryEntry
        {
            public string Id { get; set; }
            public string EventType { get; set; }
            public string Description { get; set; }
            public Dictionary<string, string> Metadata { get; set; }
            public DateTime Timestamp { get; set; }
            public float[] Embedding { get; set; }
            public float RelevanceScore { get; set; }
        }
    }
}
