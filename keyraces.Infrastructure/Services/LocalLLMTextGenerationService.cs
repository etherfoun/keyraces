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
            _httpClient.Timeout = TimeSpan.FromSeconds(120);

            _logger.LogInformation($"LocalLLMTextGenerationService initialized with API URL: {_apiUrl}, Model: {_modelName}");
        }

        public async Task<string> GenerateTextAsync(string topic = "", string difficulty = "medium", int length = 300, string language = "ru")
        {
            _logger.LogInformation($"Generating text with topic: '{topic}', difficulty: {difficulty}, length: {length}, language: {language}");

            try
            {
                try
                {
                    var healthUrl = _apiUrl.Replace("/api/generate", "/");
                    var healthResponse = await _httpClient.GetAsync(healthUrl);

                    if (!healthResponse.IsSuccessStatusCode)
                    {
                        _logger.LogError($"Ollama server returned error: {healthResponse.StatusCode}");
                        return GenerateFallbackText(topic, difficulty, length, language);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to connect to Ollama server during health check");
                    _logger.LogInformation("Ollama server is not available. Using fallback text generation.");
                    return GenerateFallbackText(topic, difficulty, length, language);
                }

                if (string.IsNullOrEmpty(topic))
                {
                    topic = GetRandomTopic(language);
                    _logger.LogInformation($"Generated random topic: {topic}");
                }

                string generatedText = null!;

                for (int attempt = 1; attempt <= 3; attempt++)
                {
                    _logger.LogInformation($"Attempt {attempt} to generate text");

                    try
                    {
                        await ResetOllamaContext();

                        generatedText = await GenerateTextWithOllama(topic, difficulty, length, language);

                        if (IsQualityText(generatedText, language))
                        {
                            _logger.LogInformation("Generated text passed quality check");
                            return generatedText;
                        }
                        else
                        {
                            _logger.LogWarning("Generated text failed quality check, trying again");
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

            var request = new
            {
                model = _modelName,
                prompt = prompt,
                stream = false,
                temperature = 0.7,
                max_tokens = length * 2,
                options = new
                {
                    num_ctx = 0,
                    seed = language == "ru" ? 42 : 24,
                    num_predict = length * 2,
                    stop = new[] { "###", "END" }
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
            var responseObject = JsonSerializer.Deserialize<JsonElement>(responseBody);

            if (responseObject.TryGetProperty("response", out var responseProperty))
            {
                var rawText = responseProperty.GetString();

                if (string.IsNullOrEmpty(rawText))
                {
                    _logger.LogWarning("Ollama returned empty text");
                    throw new Exception("Ollama returned empty text");
                }

                var cleanedText = CleanGeneratedText(rawText);
                var extractedText = ExtractMainText(cleanedText);
                var finalText = AdjustTextLength(extractedText, length);

                _logger.LogInformation($"Generated text with Ollama ({finalText.Length} characters)");
                return finalText;
            }
            else
            {
                _logger.LogError("Unexpected response format from Ollama API");
                throw new Exception("Unexpected response format from Ollama API");
            }
        }

        private string CreatePrompt(string topic, string difficulty, int length, string language)
        {
            string difficultyDescription;
            string prompt;

            if (language.ToLower() == "en")
            {
                switch (difficulty.ToLower())
                {
                    case "easy":
                        difficultyDescription = "simple text with short sentences and basic vocabulary";
                        break;
                    case "hard":
                        difficultyDescription = "complex text with specialized vocabulary";
                        break;
                    case "medium":
                    default:
                        difficultyDescription = "medium difficulty text with diverse vocabulary";
                        break;
                }

                prompt = $@"###INSTRUCTION###
                            You are a text generator for typing practice. Generate a {difficultyDescription} about '{topic}'.

                            Requirements:
                            1. The text must be EXACTLY {length} characters long
                            2. The text must be in ENGLISH only
                            3. The text must be coherent and make sense
                            4. The text must stay on topic '{topic}' throughout
                            5. Do not include any headers, introductions, or conclusions
                            6. Do not use phrases like 'Here is the text:' or 'Good luck with your practice'
                            7. Return ONLY the raw text itself

                            ###RESPONSE###
                            ";

                return prompt;
            }
            else
            {
                switch (difficulty.ToLower())
                {
                    case "easy":
                        difficultyDescription = "простой текст с короткими предложениями и базовой лексикой";
                        break;
                    case "hard":
                        difficultyDescription = "сложный текст со специализированной лексикой";
                        break;
                    case "medium":
                    default:
                        difficultyDescription = "текст средней сложности с разнообразной лексикой";
                        break;
                }

                prompt = $@"###ИНСТРУКЦИЯ###
                            Ты генератор текстов для тренировки печати. Сгенерируй {difficultyDescription} на тему '{topic}'.

                            Требования:
                            1. Текст должен быть РОВНО {length} символов в длину
                            2. Текст должен быть ТОЛЬКО на РУССКОМ языке
                            3. Текст должен быть связным и осмысленным
                            4. Текст должен строго соответствовать теме '{topic}' на всем протяжении
                            5. Не включай заголовки, вступления или заключения
                            6. Не используй фразы вроде 'Вот текст:' или 'Удачной тренировки'
                            7. Верни ТОЛЬКО сам текст

                            ###ОТВЕТ###
                            ";

                return prompt;
            }
        }

        private bool IsQualityText(string text, string language)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            if (text.Length < 50)
            {
                _logger.LogWarning($"Text too short: {text.Length} characters");
                return false;
            }

            bool isCorrectLanguage = language.ToLower() == "ru"
                ? IsRussianText(text)
                : IsEnglishText(text);

            if (!isCorrectLanguage)
            {
                _logger.LogWarning($"Text language doesn't match requested language ({language})");
                return false;
            }

            if (HasRepeatingFragments(text))
            {
                _logger.LogWarning("Text contains repeating fragments");
                return false;
            }

            if (HasNonsensePatterns(text))
            {
                _logger.LogWarning("Text contains nonsense patterns");
                return false;
            }

            return true;
        }

        private bool HasRepeatingFragments(string text)
        {
            for (int fragmentLength = 10; fragmentLength <= 30; fragmentLength++)
            {
                if (text.Length <= fragmentLength * 2)
                    continue;

                for (int i = 0; i <= text.Length - fragmentLength * 2; i++)
                {
                    string fragment = text.Substring(i, fragmentLength);

                    if (text.IndexOf(fragment, i + fragmentLength) >= 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool HasNonsensePatterns(string text)
        {
            string[] nonsensePatterns = {
                @"\[\s*\]",
                @"\[\s*\d+\s*\]",
                @"Hinweis\]",
                @"How can I's\]",
                @"How to make a\d+",
                @"```",
                @"\{\{.*?\}\}",
                @"<.*?>",
                @"$$\s*$$",
                @"\*\*.*?\*\*"
            };

            foreach (var pattern in nonsensePatterns)
            {
                if (Regex.IsMatch(text, pattern))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsRussianText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            var sampleText = text.Length > 100 ? text.Substring(0, 100) : text;

            int cyrillicCount = sampleText.Count(c => (c >= 'а' && c <= 'я') || (c >= 'А' && c <= 'Я'));

            return (double)cyrillicCount / sampleText.Length > 0.3;
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
            var ruTopics = new[] {
                "технологии", "природа", "история", "наука", "искусство",
                "спорт", "путешествия", "кулинария", "музыка", "литература",
                "космос", "океаны", "животные", "архитектура", "медицина",
                "образование", "экология", "транспорт", "культура", "психология"
            };

            var enTopics = new[] {
                "technology", "nature", "history", "science", "art",
                "sports", "travel", "cooking", "music", "literature",
                "space", "oceans", "animals", "architecture", "medicine",
                "education", "ecology", "transportation", "culture", "psychology"
            };

            var topics = language.ToLower() == "en" ? enTopics : ruTopics;
            return topics[_random.Next(topics.Length)];
        }

        private string CleanGeneratedText(string text)
        {
            _logger.LogInformation($"Cleaning generated text. Original length: {text.Length}");

            text = Regex.Replace(text, @"^###(RESPONSE|ОТВЕТ)###\s*", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"###(END|КОНЕЦ)###\s*$", "", RegexOptions.IgnoreCase);

            var headerPatterns = new[]
            {
                @"^Text for Typing Practice:[\s\n]*",
                @"^Typing Practice Text:[\s\n]*",
                @"^Practice Text:[\s\n]*",
                @"^Text:[\s\n]*",
                @"^Typing Text:[\s\n]*",
                @"^Текст для тренировки печати:[\s\n]*",
                @"^Текст для печати:[\s\n]*",
                @"^Текст:[\s\n]*"
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
                @"^Следующий текст.*?\n"
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
                @"\n.*?Надеюсь, это поможет.*?$"
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

                combinedText = Regex.Replace(combinedText, @"^(Text for Typing Practice:|Typing Practice Text:|Practice Text:|Text:|Typing Text:|Текст для тренировки печати:|Текст для печати:|Текст:)\s*", "", RegexOptions.IgnoreCase);

                combinedText = Regex.Replace(combinedText, @"\s*(Good luck|Happy typing|Enjoy your typing|Hope this helps|Practice well|Удачной тренировки|Успешной практики|Надеюсь, это поможет).*$", "", RegexOptions.IgnoreCase);

                _logger.LogInformation($"Combined text length: {combinedText.Length}");
                return combinedText.Trim();
            }

            _logger.LogInformation("Returning original text after cleaning");
            return text;
        }

        private string AdjustTextLength(string text, int targetLength)
        {
            _logger.LogInformation($"Adjusting text length from {text.Length} to {targetLength} characters");

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
                var lastSentence = sentences.LastOrDefault() ?? "Продолжайте тренировку печати для улучшения навыков.";

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

            if (language.ToLower() == "en")
            {
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
            }
            else
            {
                var easyTexts = new[]
                {
                    "Быстрая коричневая лиса прыгает через ленивую собаку. Это предложение содержит все буквы алфавита. Оно часто используется для проверки клавиатур и шрифтов. Печатайте этот текст медленно и аккуратно, следя за каждой буквой. Практика делает совершенство.",
                    "Сегодня прекрасный день для прогулки в парке. Солнце светит ярко, и птицы поют на деревьях. Многие люди гуляют со своими собаками или просто наслаждаются хорошей погодой. Некоторые читают книги на скамейках, другие играют в спортивные игры на траве.",
                    "Кошки - популярные домашние животные во многих странах мира. Они независимы, чистоплотны и не требуют много внимания. Кошки могут спать до 16 часов в день. Они отличные охотники и могут видеть в темноте лучше, чем люди. Многие люди любят кошек за их мягкий мех и успокаивающее мурлыканье.",
                    "Велосипед - отличный способ передвижения и поддержания физической формы. Езда на велосипеде укрепляет мышцы ног и сердечно-сосудистую систему. Многие города создают специальные велосипедные дорожки для безопасности велосипедистов. Это экологичный вид транспорта, который не загрязняет окружающую среду.",
                    "Чтение книг расширяет кругозор и развивает воображение. Книги могут перенести нас в другие миры и времена. Они помогают нам понять разные точки зрения и культуры. Регулярное чтение улучшает словарный запас и навыки письма. Многие люди читают перед сном, чтобы расслабиться."
                };

                var mediumTexts = new[]
                {
                    "Программирование - это процесс создания набора инструкций, которые говорят компьютеру, как выполнить задачу. Программирование может быть выполнено с использованием различных языков программирования, таких как JavaScript, Python и C++. Каждый язык имеет свои сильные и слабые стороны и может быть более подходящим для определенных типов задач.",
                    "Интернет - глобальная система взаимосвязанных компьютерных сетей, которая использует стандартный набор протоколов для обслуживания миллиардов пользователей по всему миру. Он состоит из миллионов частных, публичных, академических, деловых и правительственных сетей, которые связаны между собой широким спектром электронных, беспроводных и оптических сетевых технологий.",
                    "Искусственный интеллект (ИИ) - это область компьютерной науки, которая подчеркивает создание интеллектуальных машин, которые работают и реагируют как люди. Некоторые из видов деятельности, которые компьютеры с искусственным интеллектом предназначены для выполнения, включают распознавание речи, обучение, планирование и решение проблем.",
                    "Фотография - это искусство, наука и практика создания изображений путем записи света или другого электромагнитного излучения. Фотография используется в науке, производстве и бизнесе, а также для массового развлечения, хобби, и документирования событий. Цифровая фотография произвела революцию в индустрии, сделав фотографию более доступной для широкой публики.",
                    "Климатические изменения представляют собой долгосрочные изменения в температуре и типичных погодных условиях в определенном месте. Глобальное потепление - это долгосрочное повышение средней температуры на Земле. Ученые видят признаки глобального потепления в таянии ледников, повышении уровня моря и сдвигах в цветении растений и миграции животных."
                };

                var hardTexts = new[]
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
