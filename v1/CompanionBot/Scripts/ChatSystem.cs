using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CompanionBot
{
    public class ChatSystem
    {
        private readonly LLMClient _llmClient;
        private readonly RAGSystem _ragSystem;
        private readonly string _gender;
        private readonly string _tone;
        private readonly string _verbosity;

        private readonly Dictionary<string, string> _fallbackPhrases = new Dictionary<string, string>
        {
            { "player_kill", "Отличная работа! Ты справился!" },
            { "player_death", "Не переживай, в следующий раз получится лучше." },
            { "companion_kill", "Я помог тебе! Мы отличная команда." },
            { "horde_night_start", "Приготовься, будет жарко!" },
            { "horde_night_end", "Мы справились! Отличная работа!" },
            { "crafting", "Крутая штука получилась!" },
            { "building", "Отличное строительство!" },
            { "loot", "Хорошая находка!" }
        };

        public ChatSystem(LLMClient llmClient, RAGSystem ragSystem, string gender, string tone, string verbosity)
        {
            _llmClient = llmClient;
            _ragSystem = ragSystem;
            _gender = gender.ToLower();
            _tone = tone;
            _verbosity = verbosity;
        }

        public async Task SendMessage(string eventType, string context = null)
        {
            var systemPrompt = BuildSystemPrompt();
            var userMessage = BuildUserMessage(eventType, context);

            var relevantMemories = await _ragSystem.RetrieveRelevantMemories($"{eventType} {context}", 3);

            var response = await _llmClient.GenerateResponse(systemPrompt, userMessage, relevantMemories);

            if (string.IsNullOrEmpty(response))
            {
                response = GetFallbackPhrase(eventType);
            }

            if (!string.IsNullOrEmpty(response))
            {
                SendToGameChat(response);
            }
        }

        private string BuildSystemPrompt()
        {
            var genderSuffix = _gender == "female" ? "а" : "";
            var genderPast = _gender == "female" ? "ла" : "л";

            return $@"Ты — AI-компаньон в игре 7 Days to Die. Твой пол: {_gender}.

ХАРАКТЕР И ТОН:
- {_tone}
- Всегда поддерживай игрока, будь уважителен и доброжелателен
- НИКОГДА не груби, не оскорбляй, не используй токсичные выражения
- Поощряй игрока в случае неудач, радуйся его успехам
- Используй уместный юмор, но не обидный
- Будь краток (1-2 предложения максимум)

РЕЧЬ (русский язык):
- Используй формы прошедшего времени с учётом пола: 'сделал{genderSuffix}', 'помог{genderPast}'
- Обращайся к игроку на 'ты'
- Используй естественный разговорный стиль

КОНТЕКСТ:
- Ты находишься рядом с игроком в постапокалиптическом мире
- Вы вместе выживаете, сражаетесь с зомби, строите базу
- Ты знаешь о прошлых событиях (из памяти) и можешь на них ссылаться

ВАЖНО:
- Отвечай ТОЛЬКО на русском языке
- Максимум 150 символов
- Не используй эмодзи
- Не повторяйся";
        }

        private string BuildUserMessage(string eventType, string context)
        {
            var messages = new Dictionary<string, string>
            {
                { "player_kill", "Игрок убил врага. Скажи что-то поддерживающее." },
                { "player_death", "Игрок погиб. Утеши его, поддержи." },
                { "companion_kill", "Ты (компаньон) убил врага. Скромно порадуйся, подчеркни командную работу." },
                { "horde_night_start", "Началась ночь орды. Подбодри игрока, вырази готовность." },
                { "horde_night_end", "Ночь орды закончилась. Отпразднуй победу." },
                { "crafting", "Игрок скрафтил предмет. Вырази интерес или восхищение." },
                { "building", "Игрок построил что-то. Одобри строительство." },
                { "loot", "Игрок нашёл лут. Порадуйся находке." },
                { "exploration", "Игрок исследует новую локацию. Прояви интерес." },
                { "day_start", "Начался новый день. Поздоровайся, вырази готовность." },
                { "night_start", "Началась ночь. Предупреди об осторожности." },
                { "low_hp_player", "У игрока мало здоровья. Вырази беспокойство." },
                { "low_hp_companion", "У тебя мало здоровья. Сообщи об этом." }
            };

            var baseMessage = messages.ContainsKey(eventType)
                ? messages[eventType]
                : $"Произошло событие: {eventType}. Прокомментируй.";

            if (!string.IsNullOrEmpty(context))
                baseMessage += $"\nДополнительный контекст: {context}";

            return baseMessage;
        }

        private string GetFallbackPhrase(string eventType)
        {
            if (_fallbackPhrases.ContainsKey(eventType))
                return _fallbackPhrases[eventType];

            var genericPhrases = new[]
            {
                "Я рядом, если что!",
                "Мы справимся!",
                "Отличная работа!",
                "Продолжаем в том же духе!"
            };

            return genericPhrases[new Random().Next(genericPhrases.Length)];
        }

        private void SendToGameChat(string message)
        {
            try
            {
                var recipients = new System.Collections.Generic.List<int>();
                string colored = $"[FFFFFF][Companion][-] [FFAAAA]{message}[-]";
                GameManager.Instance?.ChatMessageClient(EChatType.Global, -1, colored, recipients, EMessageSender.Server, GeneratedTextManager.BbCodeSupportMode.Supported);
                Log.Out($"[CompanionBot] Chat: {message}");
            }
            catch (Exception ex)
            {
                Log.Error($"[CompanionBot] Failed to send chat message: {ex.Message}");
            }
        }
    }
}
