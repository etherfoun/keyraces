using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using keyraces.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace keyraces.Infrastructure.Services
{
    public class LocalLLMTextGenerationService : ITextGenerationService
    {
        private readonly ILogger<LocalLLMTextGenerationService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly string _apiUrl;
        private readonly string _modelName;
        private readonly HttpClient _httpClient;
        private readonly Random _random = new Random();

        public LocalLLMTextGenerationService(
            ILogger<LocalLLMTextGenerationService> logger,
            HttpClient httpClient,
            IServiceProvider serviceProvider,
            string apiUrl = "http://localhost:11434/api/generate",
            string modelName = "llama2")
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _apiUrl = apiUrl;
            _modelName = modelName;

            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(300);

            _logger.LogInformation($"LocalLLMTextGenerationService initialized with API URL: {_apiUrl}, Model: {_modelName}");
        }

        public async Task<string> GenerateTextAsync(string topic = "", string difficulty = "medium", int length = 300, string language = "ru")
        {
            _logger.LogInformation($"Generating text with topic: '{topic}', difficulty: {difficulty}, length: {length}, language: {language}");
            _logger.LogInformation($"Using Ollama API URL: {_apiUrl}");

            try
            {
                try
                {
                    var healthUrl = _apiUrl.Replace("/api/generate", "/");
                    _logger.LogInformation($"Checking Ollama health at: {healthUrl}");

                    var healthResponse = await _httpClient.GetAsync(healthUrl);

                    if (!healthResponse.IsSuccessStatusCode)
                    {
                        _logger.LogError($"Ollama server returned error: {healthResponse.StatusCode}");
                        var errorContent = await healthResponse.Content.ReadAsStringAsync();
                        _logger.LogError($"Error details: {errorContent}");
                        return GenerateFallbackText(topic, difficulty, length, language);
                    }

                    _logger.LogInformation("Ollama health check successful");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to connect to Ollama server during health check. API URL: {_apiUrl}");
                    _logger.LogError($"Exception type: {ex.GetType().Name}");
                    _logger.LogError($"Exception message: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                    }
                    _logger.LogInformation("Ollama server is not available. Using fallback text generation.");
                    return GenerateFallbackText(topic, difficulty, length, language);
                }

                if (string.IsNullOrEmpty(topic))
                {
                    topic = GetRandomTopic(language);
                    _logger.LogInformation($"Generated random topic: {topic}");
                }

                for (int attempt = 1; attempt <= 3; attempt++)
                {
                    _logger.LogInformation($"Attempt {attempt} to generate text");

                    try
                    {
                        await ResetOllamaContext();

                        string generatedText = await GenerateTextWithOllama(topic, difficulty, length, language);

                        var cleanedText = ProcessGeneratedText(generatedText, language, length);

                        if (IsValidText(cleanedText, language))
                        {
                            _logger.LogInformation($"Successfully generated and cleaned text ({cleanedText.Length} characters)");
                            return cleanedText;
                        }
                        else
                        {
                            _logger.LogWarning("Cleaned text is not valid (contains only punctuation or too short), trying again");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error during text generation attempt {attempt}");
                    }
                }

                _logger.LogWarning("All generation attempts failed, using fallback text");
                return GenerateFallbackText(topic, difficulty, length, language);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating text");
                return GenerateFallbackText(topic, difficulty, length, language);
            }
        }

        private bool IsValidText(string text, string language)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length < 100)
            {
                _logger.LogWarning($"Text is too short: {text?.Length ?? 0} characters");
                return false;
            }

            int letterCount;
            if (language.ToLower() == "ru" || language.ToLower() == "uk")
            {
                letterCount = text.Count(c => (c >= 'а' && c <= 'я') || (c >= 'А' && c <= 'Я') ||
                                            c == 'є' || c == 'Є' || c == 'і' || c == 'І' ||
                                            c == 'ї' || c == 'Ї' || c == 'ґ' || c == 'Ґ');
            }
            else
            {
                letterCount = text.Count(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'));
            }

            double letterRatio = (double)letterCount / text.Length;
            _logger.LogInformation($"Letter ratio in text: {letterRatio:P2} ({letterCount} letters out of {text.Length} characters)");

            if (letterRatio < 0.3)
            {
                _logger.LogWarning("Text contains too few letters");
                return false;
            }

            return true;
        }

        private string ProcessGeneratedText(string text, string language, int targetLength)
        {
            _logger.LogInformation($"Processing generated text. Original length: {text.Length}");

            var cleanedText = CleanGeneratedText(text);
            _logger.LogInformation($"Basic cleaning completed. Length: {cleanedText.Length}");

            var extractedText = ExtractMainText(cleanedText);
            _logger.LogInformation($"Main text extracted. Length: {extractedText.Length}");

            var purifiedText = PurifyText(extractedText, language);
            _logger.LogInformation($"Text purified. Length: {purifiedText.Length}");

            if (!IsValidText(purifiedText, language))
            {
                _logger.LogWarning("Purified text is not valid, using extracted text instead");
                purifiedText = extractedText;
            }

            var finalText = AdjustTextLength(purifiedText, targetLength);
            _logger.LogInformation($"Final text prepared. Length: {finalText.Length}");

            return finalText;
        }

        private string PurifyText(string text, string language)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            _logger.LogInformation($"Purifying text. Original length: {text.Length}, Target language: {language}");

            string originalText = text;

            text = RemoveNonsensePatterns(text);
            _logger.LogInformation($"Nonsense patterns removed. Length: {text.Length}");

            if (text.Length < 100)
            {
                _logger.LogWarning("Text became too short after removing nonsense patterns, reverting to original");
                text = originalText;
            }

            text = RemoveRepeatingFragments(text);
            _logger.LogInformation($"Repeating fragments removed. Length: {text.Length}");

            text = HandleMixedLanguagesCarefully(text, language);
            _logger.LogInformation($"Mixed languages handled. Length: {text.Length}");

            if (text.Length < 100)
            {
                _logger.LogWarning("Text became too short after handling mixed languages, reverting to original");
                text = originalText;
            }

            text = FinalCleanup(text);
            _logger.LogInformation($"Final cleanup completed. Length: {text.Length}");

            return text;
        }

        private string HandleMixedLanguagesCarefully(string text, string targetLanguage)
        {
            _logger.LogInformation($"Carefully handling mixed languages. Target language: {targetLanguage}");

            var sentences = Regex.Split(text, @"(?<=[.!?])\s+");
            var validSentences = new List<string>();
            var invalidSentences = new List<string>();

            foreach (var sentence in sentences)
            {
                if (string.IsNullOrWhiteSpace(sentence))
                    continue;

                bool isCorrectLanguage;
                switch (targetLanguage.ToLower())
                {
                    case "ru":
                        isCorrectLanguage = IsRussianSentence(sentence);
                        break;
                    case "uk":
                        isCorrectLanguage = IsUkrainianSentence(sentence);
                        break;
                    case "en":
                        isCorrectLanguage = IsEnglishSentence(sentence);
                        break;
                    default:
                        isCorrectLanguage = IsRussianSentence(sentence);
                        break;
                }

                if (isCorrectLanguage)
                {
                    validSentences.Add(sentence);
                }
                else
                {
                    invalidSentences.Add(sentence);
                    _logger.LogInformation($"Identified sentence in wrong language: {sentence}");
                }
            }

            if (validSentences.Count < 2 || string.Join(" ", validSentences).Length < 100)
            {
                _logger.LogWarning("Too few valid sentences found in target language, returning original text");
                return text;
            }

            var result = string.Join(" ", validSentences);
            _logger.LogInformation($"Mixed languages handled. Result length: {result.Length}");
            return result;
        }

        private bool IsRussianSentence(string sentence)
        {
            if (string.IsNullOrEmpty(sentence))
                return false;

            int cyrillicCount = sentence.Count(c => (c >= 'а' && c <= 'я') || (c >= 'А' && c <= 'Я'));

            int latinCount = sentence.Count(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'));

            int ukrainianCount = sentence.Count(c => c == 'є' || c == 'Є' || c == 'і' || c == 'І' || c == 'ї' || c == 'Ї' || c == 'ґ' || c == 'Ґ');

            return cyrillicCount > latinCount && cyrillicCount > ukrainianCount * 3;
        }

        private bool IsUkrainianSentence(string sentence)
        {
            if (string.IsNullOrEmpty(sentence))
                return false;

            int cyrillicCount = sentence.Count(c => (c >= 'а' && c <= 'я') || (c >= 'А' && c <= 'Я'));

            int latinCount = sentence.Count(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'));

            int ukrainianCount = sentence.Count(c => c == 'є' || c == 'Є' || c == 'і' || c == 'І' || c == 'ї' || c == 'Ї' || c == 'ґ' || c == 'Ґ');

            return ukrainianCount > 0 && (cyrillicCount + ukrainianCount) > latinCount;
        }

        private bool IsEnglishSentence(string sentence)
        {
            if (string.IsNullOrEmpty(sentence))
                return false;


            int cyrillicCount = sentence.Count(c => (c >= 'а' && c <= 'я') || (c >= 'А' && c <= 'Я'));

            int ukrainianCount = sentence.Count(c => c == 'є' || c == 'Є' || c == 'і' || c == 'І' || c == 'ї' || c == 'Ї' || c == 'ґ' || c == 'Ґ');

            int latinCount = sentence.Count(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'));

            return latinCount > (cyrillicCount + ukrainianCount);
        }

        private string FinalCleanup(string text)
        {
            string originalText = text;

            text = Regex.Replace(text, @"\s+", " ");

            if (IsRussianText(text) || IsUkrainianText(text))
            {
                text = Regex.Replace(text, @"\b[a-zA-Z]{1,4}\b", " ");
            }

            if (IsEnglishText(text))
            {
                text = Regex.Replace(text, @"\b[а-яА-ЯєЄіІїЇґҐ]{1,4}\b", " ");
            }

            text = Regex.Replace(text, @"\s+", " ");

            text = Regex.Replace(text, @"\s+([,.!?:;])", "$1");

            if (text.Length < 100)
            {
                _logger.LogWarning("Text became too short after final cleanup, reverting to original");
                return originalText;
            }

            return text.Trim();
        }

        private async Task ResetOllamaContext()
        {
            try
            {
                _logger.LogInformation("Resetting Ollama context");

                var resetRequest = new
                {
                    model = _modelName,
                    prompt = "RESET CONTEXT. FORGET ALL PREVIOUS CONVERSATIONS.",
                    stream = false,
                    options = new
                    {
                        num_ctx = 0,
                        seed = _random.Next(1, 10000)
                    }
                };

                var resetContent = new StringContent(
                    JsonSerializer.Serialize(resetRequest),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(_apiUrl, resetContent);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Ollama context reset successful");
                }
                else
                {
                    _logger.LogWarning($"Ollama context reset returned status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting Ollama context");
            }
        }

        private async Task<string> GenerateTextWithOllama(string topic, string difficulty, int length, string language)
        {
            string prompt = CreatePrompt(topic, difficulty, length, language);
            _logger.LogInformation($"Using prompt: {prompt}");

            var request = new
            {
                model = _modelName,
                prompt = prompt,
                stream = false,
                temperature = 0.7,
                max_tokens = length * 2,
                options = new
                {
                    num_ctx = 2048,
                    seed = _random.Next(1, 10000),
                    num_predict = length * 2,
                    stop = new[] { "###", "END", "\n\n\n" }
                }
            };

            var jsonRequest = JsonSerializer.Serialize(request);
            _logger.LogInformation($"Request to Ollama: {jsonRequest}");

            var content = new StringContent(
                jsonRequest,
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(_apiUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Ollama API returned error: {response.StatusCode}. Details: {errorContent}");
                throw new Exception($"Ollama API returned error: {response.StatusCode}");
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"Raw response from Ollama: {responseBody}");

            var responseObject = JsonSerializer.Deserialize<JsonElement>(responseBody);

            if (responseObject.TryGetProperty("response", out var responseProperty))
            {
                var rawText = responseProperty.GetString();

                if (string.IsNullOrEmpty(rawText))
                {
                    _logger.LogWarning("Ollama returned empty text");
                    throw new Exception("Ollama returned empty text");
                }

                return rawText;
            }
            else
            {
                _logger.LogError("Unexpected response format from Ollama API");
                throw new Exception("Unexpected response format from Ollama API");
            }
        }

        private string CreatePrompt(string topic, string difficulty, int length, string language)
        {
            switch (language.ToLower())
            {
                case "en":
                    string enDifficulty = difficulty.ToLower() switch
                    {
                        "easy" => "easy",
                        "hard" => "hard",
                        _ => "medium"
                    };

                    return $@"### INSTRUCTION ###
                            Write a short text in English for typing practice.
                            Topic: ""{topic}"".
                            Difficulty level: {enDifficulty}.
                            Text length: approximately {length} characters.

                            IMPORTANT:
                            1. Write ONLY in English. DO NOT use words or phrases in other languages.
                            2. DO NOT use markers, lists, headings, or formatting.
                            3. The text should be coherent and logical.
                            4. DO NOT add introductory or concluding phrases.
                            5. DO NOT mention that this is a text for typing practice.
                            6. DO NOT use special characters except for regular punctuation.
                            7. Write only plain text without markup.
                            8. Use only Latin characters.

                            ### RESPONSE ###";

                case "uk":
                    string ukDifficulty = difficulty.ToLower() switch
                    {
                        "easy" => "простий",
                        "hard" => "складний",
                        _ => "середній"
                    };

                    return $@"### ІНСТРУКЦІЯ ###
                            Напиши короткий текст українською мовою для тренування друку. 
                            Тема тексту: ""{topic}"".
                            Рівень складності: {ukDifficulty}.
                            Довжина тексту: приблизно {length} символів.

                            ВАЖЛИВО:
                            1. Пиши ТІЛЬКИ українською мовою. НЕ використовуй слова або фрази іншими мовами.
                            2. НЕ використовуй маркери, списки, заголовки або форматування.
                            3. Текст має бути зв'язним та логічним.
                            4. НЕ додавай вступні або заключні фрази.
                            5. НЕ згадуй, що це текст для тренування друку.
                            6. НЕ використовуй спеціальні символи, крім звичайних розділових знаків.
                            7. Пиши тільки звичайний текст без розмітки.
                            8. Використовуй тільки українські символи (кирилицю).
                            9. Обов'язково використовуй символи є, і, ї та ґ, які притаманні українській мові.

                            ### ВІДПОВІДЬ ###";

                default: // Russian
                    string ruDifficulty = difficulty.ToLower() switch
                    {
                        "easy" => "простой",
                        "hard" => "сложный",
                        _ => "средний"
                    };

                    return $@"### ИНСТРУКЦИЯ ###
                            Напиши короткий текст на русском языке для тренировки печати. 
                            Тема текста: ""{topic}"".
                            Уровень сложности: {ruDifficulty}.
                            Длина текста: примерно {length} символов.

                            ВАЖНО:
                            1. Пиши ТОЛЬКО на русском языке. НЕ используй английские слова или фразы.
                            2. НЕ используй маркеры, списки, заголовки или форматирование.
                            3. Текст должен быть связным и логичным.
                            4. НЕ добавляй вступительные или заключительные фразы.
                            5. НЕ упоминай, что это текст для тренировки печати.
                            6. НЕ используй специальные символы, кроме обычных знаков препинания.
                            7. Пиши только обычный текст без разметки.
                            8. Используй только кириллические символы.

                            ### ОТВЕТ ###";
            }
        }

        private string RemoveNonsensePatterns(string text)
        {
            string originalText = text;

            string[] nonsensePatterns = {
                @"\[\s*\]",
                @"\[\s*\d+\s*\]",
                @"Hinweis\]",
                @"How can I's\]",
                @"How to make a\d+",
                @"\`\`\`.*?\`\`\`",
                @"\{\{.*?\}\}",
                @"<.*?>",
                @"$$\s*$$",
                @"\*\*.*?\*\*",
                @"^\`\`\`.*?$",
                @"^#.*?$"
            };

            foreach (var pattern in nonsensePatterns)
            {
                text = Regex.Replace(text, pattern, " ", RegexOptions.Singleline);
            }

            text = Regex.Replace(text, @"\s+", " ");

            if (text.Length < 100)
            {
                _logger.LogWarning("Text became too short after removing nonsense patterns, reverting to original");
                return originalText;
            }

            return text.Trim();
        }

        private string RemoveRepeatingFragments(string text)
        {
            string originalText = text;

            for (int fragmentLength = 15; fragmentLength <= 50; fragmentLength += 5)
            {
                if (text.Length <= fragmentLength * 2)
                    continue;

                for (int i = 0; i <= text.Length - fragmentLength * 2; i++)
                {
                    string fragment = text.Substring(i, fragmentLength);
                    int nextOccurrence = text.IndexOf(fragment, i + fragmentLength);

                    if (nextOccurrence >= 0)
                    {
                        _logger.LogInformation($"Found repeating fragment: '{fragment}' at positions {i} and {nextOccurrence}");
                        text = text.Remove(nextOccurrence, fragmentLength);
                        i = 0;
                    }
                }
            }

            if (text.Length < 100)
            {
                _logger.LogWarning("Text became too short after removing repeating fragments, reverting to original");
                return originalText;
            }

            return text;
        }

        private bool IsRussianText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            var sampleText = text.Length > 100 ? text.Substring(0, 100) : text;

            int cyrillicCount = sampleText.Count(c => (c >= 'а' && c <= 'я') || (c >= 'А' && c <= 'Я'));
            int ukrainianCount = sampleText.Count(c => c == 'є' || c == 'Є' || c == 'і' || c == 'І' || c == 'ї' || c == 'Ї' || c == 'ґ' || c == 'Ґ');

            return (double)cyrillicCount / sampleText.Length > 0.3 && cyrillicCount > ukrainianCount * 3;
        }

        private bool IsUkrainianText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            var sampleText = text.Length > 100 ? text.Substring(0, 100) : text;

            int cyrillicCount = sampleText.Count(c => (c >= 'а' && c <= 'я') || (c >= 'А' && c <= 'Я'));
            int ukrainianCount = sampleText.Count(c => c == 'є' || c == 'Є' || c == 'і' || c == 'І' || c == 'ї' || c == 'Ї' || c == 'ґ' || c == 'Ґ');

            return ukrainianCount > 0 && (double)(cyrillicCount + ukrainianCount) / sampleText.Length > 0.3;
        }

        private bool IsEnglishText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            var sampleText = text.Length > 100 ? text.Substring(0, 100) : text;

            int latinCount = sampleText.Count(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'));

            return (double)latinCount / sampleText.Length > 0.5;
        }

        private string GetRandomTopic(string language)
        {
            switch (language.ToLower())
            {
                case "en":
                    var enTopics = new[] {
                        "technology", "nature", "history", "science", "art",
                        "sports", "travel", "cooking", "music", "literature",
                        "space", "oceans", "animals", "architecture", "medicine",
                        "education", "ecology", "transportation", "culture", "psychology"
                    };
                    return enTopics[_random.Next(enTopics.Length)];

                case "uk":
                    var ukTopics = new[] {
                        "технології", "природа", "історія", "наука", "мистецтво",
                        "спорт", "подорожі", "кулінарія", "музика", "література",
                        "космос", "океани", "тварини", "архітектура", "медицина",
                        "освіта", "екологія", "транспорт", "культура", "психологія"
                    };
                    return ukTopics[_random.Next(ukTopics.Length)];

                default: // Russian
                    var ruTopics = new[] {
                        "технологии", "природа", "история", "наука", "искусство",
                        "спорт", "путешествия", "кулинария", "музыка", "литература",
                        "космос", "океаны", "животные", "архитектура", "медицина",
                        "образование", "экология", "транспорт", "культура", "психология"
                    };
                    return ruTopics[_random.Next(ruTopics.Length)];
            }
        }

        private string CleanGeneratedText(string text)
        {
            _logger.LogInformation($"Cleaning generated text. Original length: {text.Length}");

            text = Regex.Replace(text, @"^###(RESPONSE|ОТВЕТ|ВІДПОВІДЬ)###\s*", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"###(END|КОНЕЦ|КІНЕЦЬ)###\s*$", "", RegexOptions.IgnoreCase);

            var headerPatterns = new[]
            {
                @"^Text for Typing Practice:[\s\n]*",
                @"^Typing Practice Text:[\s\n]*",
                @"^Practice Text:[\s\n]*",
                @"^Text:[\s\n]*",
                @"^Typing Text:[\s\n]*",
                @"^Текст для тренировки печати:[\s\n]*",
                @"^Текст для печати:[\s\n]*",
                @"^Текст:[\s\n]*",
                @"^Текст для тренування друку:[\s\n]*",
                @"^Текст для друку:[\s\n]*"
            };

            foreach (var pattern in headerPatterns)
            {
                text = Regex.Replace(text, pattern, "", RegexOptions.IgnoreCase);
            }

            var introPatterns = new[]
            {
                @"^Sure[!,.].*?\n",
                @"^Here is a.*?\n",
                @"^Here's a.*?\n",
                @"^I'd be happy to.*?\n",
                @"^This is a.*?\n",
                @"^Below is a.*?\n",
                @"^The following is a.*?\n",
                @"^Конечно[!,.].*?\n",
                @"^Вот текст.*?\n",
                @"^Ниже представлен.*?\n",
                @"^Следующий текст.*?\n",
                @"^Звичайно[!,.].*?\n",
                @"^Ось текст.*?\n",
                @"^Нижче представлений.*?\n",
                @"^Наступний текст.*?\n"
            };

            foreach (var pattern in introPatterns)
            {
                text = Regex.Replace(text, pattern, "", RegexOptions.IgnoreCase);
            }

            var outroPatterns = new[]
            {
                @"\n.*?Good luck.*?$",
                @"\n.*?Happy typing.*?$",
                @"\n.*?Enjoy your typing.*?$",
                @"\n.*?Hope this helps.*?$",
                @"\n.*?Practice well.*?$",
                @"\n.*?Удачной тренировки.*?$",
                @"\n.*?Успешной практики.*?$",
                @"\n.*?Надеюсь, это поможет.*?$",
                @"\n.*?Удачного тренування.*?$",
                @"\n.*?Успішної практики.*?$",
                @"\n.*?Сподіваюся, це допоможе.*?$"
            };

            foreach (var pattern in outroPatterns)
            {
                text = Regex.Replace(text, pattern, "", RegexOptions.IgnoreCase);
            }

            text = text.Trim('"', '\'', ' ', '\n', '\r', '\t');

            _logger.LogInformation($"Cleaned text. New length: {text.Length}");

            return text;
        }

        private string ExtractMainText(string text)
        {
            _logger.LogInformation($"Extracting main text. Input length: {text.Length}");

            text = text.Trim();

            var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            if (paragraphs.Length > 1)
            {
                _logger.LogInformation($"Found {paragraphs.Length} paragraphs");

                var filteredParagraphs = paragraphs
                    .Where(p => p.Length > 50)
                    .ToList();

                if (filteredParagraphs.Count > 0)
                {
                    var mainParagraph = filteredParagraphs.OrderByDescending(p => p.Length).First();
                    _logger.LogInformation($"Selected main paragraph with length: {mainParagraph.Length}");

                    mainParagraph = mainParagraph.Trim('"', '\'', ' ', '\n', '\r', '\t');

                    return mainParagraph;
                }
            }

            var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length > 0)
            {
                _logger.LogInformation($"Falling back to line-based extraction. Found {lines.Length} lines");

                var combinedText = string.Join(" ", lines.Select(l => l.Trim()));

                combinedText = Regex.Replace(combinedText, @"^(Text for Typing Practice:|Typing Practice Text:|Practice Text:|Text:|Typing Text:|Текст для тренировки печати:|Текст для печати:|Текст:|Текст для тренування друку:|Текст для друку:)\s*", "", RegexOptions.IgnoreCase);
                combinedText = Regex.Replace(combinedText, @"\s*(Good luck|Happy typing|Enjoy your typing|Hope this helps|Practice well|Удачной тренировки|Успешной практики|Надеюсь, это поможет|Удачного тренування|Успішної практики|Сподіваюся, це допоможе).*$", "", RegexOptions.IgnoreCase);

                _logger.LogInformation($"Combined text length: {combinedText.Length}");
                return combinedText.Trim();
            }

            _logger.LogInformation("Returning original text after cleaning");
            return text;
        }

        private string AdjustTextLength(string text, int targetLength)
        {
            _logger.LogInformation($"Adjusting text length from {text.Length} to {targetLength} characters");

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("Text is empty, cannot adjust length");
                return GenerateFallbackText("", "medium", targetLength, "ru");
            }

            if (!IsValidText(text, "ru") && !IsValidText(text, "en") && !IsValidText(text, "uk"))
            {
                _logger.LogWarning("Text contains only punctuation, using fallback text");
                return GenerateFallbackText("", "medium", targetLength, "ru");
            }

            if (text.Length == targetLength)
            {
                return text;
            }
            else if (text.Length > targetLength)
            {
                int cutIndex = targetLength;
                while (cutIndex > 0 && !char.IsWhiteSpace(text[cutIndex - 1]))
                {
                    cutIndex--;
                }

                if (cutIndex == 0)
                {
                    cutIndex = targetLength;
                }

                var result = text.Substring(0, cutIndex).Trim();
                _logger.LogInformation($"Text was trimmed from {text.Length} to {result.Length} characters");
                return result;
            }
            else
            {
                var sentences = Regex.Split(text, @"(?<=[.!?])\s+");

                string lastSentence;
                if (sentences.Length > 0 && !string.IsNullOrWhiteSpace(sentences.Last()))
                {
                    lastSentence = sentences.Last();
                }
                else
                {
                    if (IsEnglishText(text))
                    {
                        lastSentence = "Continue typing practice to improve your skills.";
                    }
                    else if (IsUkrainianText(text))
                    {
                        lastSentence = "Продовжуйте тренування друку для покращення навичок.";
                    }
                    else
                    {
                        lastSentence = "Продолжайте тренировку печати для улучшения навыков.";
                    }
                }

                var result = text;
                while (result.Length < targetLength)
                {
                    result += " " + lastSentence;
                }

                result = AdjustTextLength(result, targetLength);
                _logger.LogInformation($"Text was extended from {text.Length} to {result.Length} characters");
                return result;
            }
        }

        private string GenerateFallbackText(string topic = "", string difficulty = "medium", int length = 300, string language = "ru")
        {
            _logger.LogInformation("Generating fallback text");

            if (string.IsNullOrEmpty(topic))
            {
                topic = GetRandomTopic(language);
            }

            string[] textsArray;

            switch (language.ToLower())
            {
                case "en":
                    var easyTexts = new[]
                    {
                        "The quick brown fox jumps over the lazy dog. This pangram contains all the letters of the English alphabet. It is often used to test typewriters or computer keyboards, and in other applications involving all of the letters in the English alphabet.",
                        "Today is a beautiful day for a walk in the park. The sun is shining brightly, and birds are singing in the trees. Many people are walking their dogs or just enjoying the nice weather. Some are reading books on benches, others are playing sports games on the grass.",
                        "Cats are popular pets in many countries around the world. They are independent, clean, and do not require much attention. Cats can sleep up to 16 hours a day. They are excellent hunters and can see in the dark better than humans. Many people love cats for their soft fur and soothing purr.",
                        "Bicycling is a great way to get around and stay in shape. Riding a bicycle strengthens your leg muscles and cardiovascular system. Many cities create special bike lanes for cyclist safety. It is an environmentally friendly mode of transportation that does not pollute the environment.",
                        "Reading books broadens your horizons and develops your imagination. Books can transport us to other worlds and times. They help us understand different perspectives and cultures. Regular reading improves vocabulary and writing skills. Many people read before bed to relax."
                    };

                    var mediumTexts = new[]
                    {
                        "Programming is the process of creating a set of instructions that tell a computer how to perform a task. Programming can be done using a variety of computer programming languages, such as JavaScript, Python, and C++. Each language has its strengths and weaknesses and may be better suited for specific types of tasks.",
                        "The Internet is a global system of interconnected computer networks that use the standard Internet protocol suite to serve billions of users worldwide. It consists of millions of private, public, academic, business, and government networks, linked by a broad array of electronic, wireless, and optical networking technologies.",
                        "Artificial Intelligence (AI) is the field of computer science that emphasizes the creation of intelligent machines that work and react like humans. Some of the activities computers with artificial intelligence are designed to include speech recognition, learning, planning, and problem-solving.",
                        "Photography is the art, science, and practice of creating images by recording light or other electromagnetic radiation. Photography is used in science, manufacturing, and business, as well as for mass entertainment, hobbies, and documenting events. Digital photography has revolutionized the industry, making photography more accessible to the general public.",
                        "Climate change refers to long-term shifts in temperatures and weather patterns. Global warming is the long-term heating of Earth's climate system. Scientists see evidence of global warming in melting glaciers, rising sea levels, and shifts in plant flowering and animal migration."
                    };

                    var hardTexts = new[]
                    {
                        "In quantum mechanics, the Heisenberg uncertainty principle states that it is impossible to simultaneously measure the position and momentum of a particle with absolute precision. This fundamental limitation has profound implications for our understanding of the physical world. It shows that at the quantum level, there is an inherent uncertainty that is not a result of the limitations of our measuring instruments.",
                        "Blockchain is a distributed database that maintains a continuously growing list of records, called blocks. Each block contains a timestamp and a link to a previous block. Cryptography ensures that users can only edit the parts of the blockchain that they 'own' by possessing the private keys necessary to write to the file. In addition, cryptography ensures that everyone can transparently see that changes have been made to the blockchain.",
                        "Neural networks are computing systems inspired by the biological neural networks that constitute animal brains. Such systems learn to perform tasks by considering examples, generally without being programmed with task-specific rules. For example, in image recognition, they might learn to identify images that contain cats by analyzing example images that have been manually labeled as 'cat' or 'no cat' and using the results to identify cats in other images.",
                        "Einstein's theory of relativity revolutionized our understanding of space, time, and gravity. The special theory of relativity, published in 1905, showed that space and time are interrelated and that the speed of light is constant for all observers. The general theory of relativity, published in 1915, describes gravity as the curvature of spacetime by mass and energy.",
                        "Bioinformatics is an interdisciplinary field that develops methods and software tools for understanding biological data. As an interdisciplinary field of science, bioinformatics combines computer science, statistics, mathematics, and engineering to analyze and interpret biological data. Bioinformatics is widely used in genomics, proteomics, and other areas of molecular biology."
                    };

                    switch (difficulty.ToLower())
                    {
                        case "easy":
                            textsArray = easyTexts;
                            break;
                        case "hard":
                            textsArray = hardTexts;
                            break;
                        case "medium":
                        default:
                            textsArray = mediumTexts;
                            break;
                    }
                    break;

                case "uk":
                    var ukEasyTexts = new[]
                    {
                        "Швидка коричнева лисиця стрибає через лінивого собаку. Це речення містить багато літер українського алфавіту. Воно часто використовується для перевірки клавіатур та шрифтів. Друкуйте цей текст повільно та акуратно, слідкуючи за кожною літерою. Практика робить досконалість.",
                        "Сьогодні чудовий день для прогулянки в парку. Сонце яскраво світить, і птахи співають на деревах. Багато людей гуляють зі своїми собаками або просто насолоджуються гарною погодою. Деякі читають книги на лавках, інші грають у спортивні ігри на траві.",
                        "Коти - популярні домашні тварини у багатьох країнах світу. Вони незалежні, чисті і не вимагають багато уваги. Коти можуть спати до 16 годин на день. Вони відмінні мисливці і можуть бачити в темряві краще, ніж люди. Багато людей люблять котів за їх м'яке хутро і заспокійливе муркотіння.",
                        "Велосипед - чудовий спосіб пересування та підтримки фізичної форми. Їзда на велосипеді зміцнює м'язи ніг і серцево-судинну систему. Багато міст створюють спеціальні велосипедні доріжки для безпеки велосипедистів. Це екологічний вид транспорту, який не забруднює навколишнє середовище.",
                        "Читання книг розширює кругозір і розвиває уяву. Книги можуть перенести нас в інші світи і часи. Вони допомагають нам зрозуміти різні точки зору і культури. Регулярне читання покращує словниковий запас і навички письма. Багато людей читають перед сном, щоб розслабитися."
                    };

                    var ukMediumTexts = new[]
                    {
                        "Програмування - це процес створення набору інструкцій, які вказують комп'ютеру, як виконати завдання. Програмування може здійснюватися з використанням різних мов програмування, таких як JavaScript, Python і C++. Кожна мова має свої сильні і слабкі сторони і може бути більш підходящою для певних типів завдань.",
                        "Інтернет - глобальна система взаємопов'язаних комп'ютерних мереж, яка використовує стандартний набір протоколів для обслуговування мільярдів користувачів по всьому світу. Він складається з мільйонів приватних, публічних, академічних, ділових і урядових мереж, які пов'язані між собою широким спектром електронних, бездротових і оптичних мережевих технологій.",
                        "Штучний інтелект (ШІ) - це область комп'ютерної науки, яка підкреслює створення інтелектуальних машин, які працюють і реагують як люди. Деякі з видів діяльності, які комп'ютери зі штучним інтелектом призначені для виконання, включають розпізнавання мови, навчання, планування і вирішення проблем.",
                        "Фотографія - це мистецтво, наука і практика створення зображень шляхом запису світла або іншого електромагнітного випромінювання. Фотографія використовується в науці, виробництві і бізнесі, а також для масових розваг, хобі, і документування подій. Цифрова фотографія зробила революцію в індустрії, зробивши фотографію більш доступною для широкої публіки.",
                        "Кліматичні зміни являють собою довгострокові зміни в температурі і типових погодних умовах у певному місці. Глобальне потепління - це довгострокове підвищення середньої температури на Землі. Вчені бачать ознаки глобального потепління в таненні льодовиків, підвищенні рівня моря і зрушеннях у цвітінні рослин і міграції тварин."
                    };

                    var ukHardTexts = new[]
                    {
                        "У квантовій механіці принцип невизначеності Гейзенберга стверджує, що неможливо одночасно точно виміряти положення і імпульс частинки. Це фундаментальне обмеження, яке має глибокі наслідки для нашого розуміння фізичного світу. Воно показує, що на квантовому рівні існує невід'ємна невизначеність, яка не є результатом обмежень наших вимірювальних приладів.",
                        "Блокчейн - це розподілена база даних, яка підтримує постійно зростаючий список записів, званих блоками. Кожен блок містить часову мітку і посилання на попередній блок. Криптографія забезпечує, що користувачі можуть редагувати тільки ті частини блокчейна, якими вони 'володіють', маючи закриті ключі, необхідні для запису в файл. Крім того, криптографія гарантує, що всі можуть прозоро бачити, що зміни були внесені в блокчейн.",
                        "Нейронні мережі - це обчислювальні системи, натхненні біологічними нейронними мережами, що складають мозок тварин. Такі системи вчаться виконувати завдання, розглядаючи приклади, зазвичай без програмування з конкретними правилами. Наприклад, при розпізнаванні зображень вони можуть навчитися ідентифікувати зображення, що містять котів, аналізуючи приклади зображень, які були вручну позначені як 'кіт' або 'без кота', і використовуючи результати для ідентифікації котів в інших зображеннях.",
                        "Теорія відносності Ейнштейна здійснила революцію в нашому розумінні простору, часу і гравітації. Спеціальна теорія відносності, опублікована в 1905 році, показала, що простір і час взаємопов'язані і що швидкість світла постійна для всіх спостерігачів. Загальна теорія відносності, опублікована в 1915 році, описує гравітацію як викривлення простору-часу масою і енергією.",
                        "Біоінформатика - це міждисциплінарна область, яка розробляє методи і програмні інструменти для розуміння біологічних даних. Як міждисциплінарна область науки, біоінформатика поєднує в собі комп'ютерні науки, статистику, математику та інженерію для аналізу та інтерпретації біологічних даних. Біоінформатика широко використовується в геноміці, протеоміці, та інших областях молекулярної біології."
                    };

                    switch (difficulty.ToLower())
                    {
                        case "easy":
                            textsArray = ukEasyTexts;
                            break;
                        case "hard":
                            textsArray = ukHardTexts;
                            break;
                        case "medium":
                        default:
                            textsArray = ukMediumTexts;
                            break;
                    }
                    break;

                default: // Russian
                    var ruEasyTexts = new[]
                    {
                        "Быстрая коричневая лиса прыгает через ленивую собаку. Это предложение содержит все буквы алфавита. Оно часто используется для проверки клавиатур и шрифтов. Печатайте этот текст медленно и аккуратно, следя за каждой буквой. Практика делает совершенство.",
                        "Сегодня прекрасный день для прогулки в парке. Солнце светит ярко, и птицы поют на деревьях. Многие люди гуляют со своими собаками или просто наслаждаются хорошей погодой. Некоторые читают книги на скамейках, другие играют в спортивные игры на траве.",
                        "Кошки - популярные домашние животные во многих странах мира. Они независимы, чистоплотны и не требуют много внимания. Кошки могут спать до 16 часов в день. Они отличные охотники и могут видеть в темноте лучше, чем люди. Многие люди любят кошек за их мягкий мех и успокаивающее мурлыканье.",
                        "Велосипед - отличный способ передвижения и поддержания физической формы. Езда на велосипеде укрепляет мышцы ног и сердечно-сосудистую систему. Многие города создают специальные велосипедные дорожки для безопасности велосипедистов. Это экологичный вид транспорта, который не загрязняет окружающую среду.",
                        "Чтение книг расширяет кругозор и развивает воображение. Книги могут перенести нас в другие миры и времена. Они помогают нам понять разные точки зрения и культуры. Регулярное чтение улучшает словарный запас и навыки письма. Многие люди читают перед сном, чтобы расслабиться."
                    };

                    var ruMediumTexts = new[]
                    {
                        "Программирование - это процесс создания набора инструкций, которые говорят компьютеру, как выполнить задачу. Программирование может быть выполнено с использованием различных языков программирования, таких как JavaScript, Python и C++. Каждый язык имеет свои сильные и слабые стороны и может быть более подходящим для определенных типов задач.",
                        "Интернет - глобальная система взаимосвязанных компьютерных сетей, которая использует стандартный набор протоколов для обслуживания миллиардов пользователей по всему миру. Он состоит из миллионов частных, публичных, академических, деловых и правительственных сетей, которые связаны между собой широким спектром электронных, беспроводных и оптических сетевых технологий.",
                        "Искусственный интеллект (ИИ) - это область компьютерной науки, которая подчеркивает создание интеллектуальных машин, которые работают и реагируют как люди. Некоторые из видов деятельности, которые компьютеры с искусственным интеллектом предназначены для выполнения, включают распознавание речи, обучение, планирование и решение проблем.",
                        "Фотография - это искусство, наука и практика создания изображений путем записи света или другого электромагнитного излучения. Фотография используется в науке, производстве и бизнесе, а также для массового развлечения, хобби, и документирования событий. Цифровая фотография произвела революцию в индустрии, сделав фотографию более доступной для широкой публики.",
                        "Климатические изменения представляют собой долгосрочные изменения в температуре и типичных погодных условиях в определенном месте. Глобальное потепление - это долгосрочное повышение средней температуры на Земле. Ученые видят признаки глобального потепления в таянии ледников, повышении уровня моря и сдвигах в цветении растений и миграции животных."
                    };

                    var ruHardTexts = new[]
                    {
                        "В квантовой механике принцип неопределенности Гейзенберга утверждает, что невозможно одновременно точно измерить положение и импульс частицы. Это фундаментальное ограничение, которое имеет глубокие последствия для нашего понимания физического мира. Оно показывает, что на квантовом уровне существует неустранимая неопределенность, которая не является результатом ограничений наших измерительных приборов.",
                        "Блокчейн - это распределенная база данных, которая поддерживает постоянно растущий список записей, называемых блоками. Каждый блок содержит временную метку и ссылку на предыдущий блок. Криптография обеспечивает, что пользователи могут редактировать только те части блокчейна, которыми они 'владеют', имея закрытые ключи, необходимые для записи в файл. Кроме того, криптография гарантирует, что все могут прозрачно видеть, что изменения были внесены в блокчейн.",
                        "Нейронные сети - это вычислительные системы, вдохновленные биологическими нейронными сетями, составляющими мозг животных. Такие системы учатся выполнять задачи, рассматривая примеры, обычно без программирования с конкретными правилами. Например, при распознавании изображений они могут научиться идентифицировать изображения, содержащие кошек, анализируя примеры изображений, которые были вручную помечены как 'кошка' или 'без кошки', и используя результаты для идентификации кошек в других изображениях.",
                        "Теория относительности Эйнштейна произвела революцию в нашем понимании пространства, времени и гравитации. Специальная теория относительности, опубликованная в 1905 году, показала, что пространство и время взаимосвязаны и что скорость света постоянна для всех наблюдателей. Общая теория относительности, опубликованная в 1915 году, описывает гравитацию как искривление пространства-времени массой и энергией.",
                        "Биоинформатика - это междисциплинарная область, которая разрабатывает методы и программные инструменты для понимания биологических данных. Как междисциплинарная область науки, биоинформатика сочетает в себе компьютерные науки, статистику, математику и инженерию для анализа и интерпретации биологических данных. Биоинформатика широко используется в геномике, протеомике, и других областях молекулярной биологии."
                    };

                    switch (difficulty.ToLower())
                    {
                        case "easy":
                            textsArray = ruEasyTexts;
                            break;
                        case "hard":
                            textsArray = ruHardTexts;
                            break;
                        case "medium":
                        default:
                            textsArray = ruMediumTexts;
                            break;
                    }
                    break;
            }

            string selectedText = null!;

            foreach (var text in textsArray)
            {
                if (text.ToLower().Contains(topic.ToLower()))
                {
                    selectedText = text;
                    break;
                }
            }

            if (selectedText == null)
            {
                selectedText = textsArray[_random.Next(textsArray.Length)];
            }

            selectedText = AdjustTextLength(selectedText, length);

            _logger.LogInformation($"Generated fallback text ({selectedText.Length} characters)");
            return selectedText;
        }
    }
}
