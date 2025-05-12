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
            _httpClient.Timeout = TimeSpan.FromSeconds(60);

            _logger.LogCritical($"LocalLLMTextGenerationService initialized with API URL: {_apiUrl}, Model: {_modelName}");
        }

        public async Task<string> GenerateTextAsync(string topic = "", string difficulty = "medium", int length = 300)
        {
            _logger.LogCritical("!!! OLLAMA TEXT GENERATION SERVICE CALLED !!!");

            try
            {
                try
                {
                    var healthResponse = await _httpClient.GetAsync("http://localhost:11434/");
                    var healthContent = await healthResponse.Content.ReadAsStringAsync();

                    _logger.LogInformation($"Ollama health check: {healthResponse.StatusCode}, Content: {healthContent}");

                    if (!healthResponse.IsSuccessStatusCode)
                    {
                        _logger.LogError($"Ollama server returned error: {healthResponse.StatusCode}");
                        return GenerateFallbackText(topic, difficulty, length);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to connect to Ollama server during health check");
                    return GenerateFallbackText(topic, difficulty, length);
                }

                _logger.LogInformation($"Generating text with Ollama. Topic: '{topic}', Difficulty: {difficulty}, Length: {length}");

                var topicPrompt = string.IsNullOrEmpty(topic)
                    ? "on any topic"
                    : $"on the topic of '{topic}'";

                var prompt = $"Generate a typing practice text {topicPrompt}. " +
                             $"Difficulty level: {difficulty}. " +
                             $"The text should be approximately {length} characters long. " +
                             $"The text should be coherent, interesting, and without complex special characters. " +
                             $"IMPORTANT: Return ONLY the raw text itself. " +
                             $"DO NOT include any headers like 'Text for Typing Practice:' or 'Typing Text:'. " +
                             $"DO NOT include any introduction like 'Here is a text' or 'Sure, here's a text'. " +
                             $"DO NOT include any conclusion like 'Good luck' or 'Happy typing'. " +
                             $"DO NOT wrap the text in quotes. " +
                             $"Just provide the plain text content that the user will type, nothing else.";

                _logger.LogInformation($"Prompt: {prompt}");

                var request = new
                {
                    model = _modelName,
                    prompt = prompt,
                    stream = false
                };

                var jsonRequest = JsonSerializer.Serialize(request);
                _logger.LogInformation($"Request JSON: {jsonRequest}");

                var content = new StringContent(
                    jsonRequest,
                    Encoding.UTF8,
                    "application/json");

                _logger.LogInformation($"Sending request to Ollama API at {_apiUrl}");
                var response = await _httpClient.PostAsync(_apiUrl, content);

                _logger.LogInformation($"Received response. Status code: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Ollama API returned error: {response.StatusCode}. Details: {errorContent}");
                    return GenerateFallbackText(topic, difficulty, length);
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Response body: {responseBody}");

                var responseObject = JsonSerializer.Deserialize<JsonElement>(responseBody);

                if (responseObject.TryGetProperty("response", out var responseProperty))
                {
                    var generatedText = responseProperty.GetString();

                    if (string.IsNullOrEmpty(generatedText))
                    {
                        _logger.LogWarning("Ollama returned empty text. Using fallback.");
                        return GenerateFallbackText(topic, difficulty, length);
                    }

                    var cleanedText = CleanGeneratedText(generatedText);

                    var extractedText = ExtractMainText(cleanedText);

                    _logger.LogInformation($"Successfully generated text with Ollama ({extractedText.Length} characters)");
                    return extractedText.Trim();
                }
                else
                {
                    _logger.LogError("Unexpected response format from Ollama API. Response property not found.");
                    return GenerateFallbackText(topic, difficulty, length);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating text with Ollama");

                _logger.LogInformation("Using fallback text generation due to error");
                return GenerateFallbackText(topic, difficulty, length);
            }
        }

        private string CleanGeneratedText(string text)
        {
            _logger.LogInformation($"Cleaning generated text. Original length: {text.Length}");

            var headerPatterns = new[]
            {
                @"^Text for Typing Practice:[\s\n]*",
                @"^Typing Practice Text:[\s\n]*",
                @"^Practice Text:[\s\n]*",
                @"^Text:[\s\n]*",
                @"^Typing Text:[\s\n]*"
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
                @"^The following is a.*?\n"
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
                @"\n.*?Practice well.*?$"
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

                combinedText = Regex.Replace(combinedText, @"^(Text for Typing Practice:|Typing Practice Text:|Practice Text:|Text:|Typing Text:)\s*", "", RegexOptions.IgnoreCase);

                combinedText = Regex.Replace(combinedText, @"\s*(Good luck|Happy typing|Enjoy your typing|Hope this helps|Practice well).*$", "", RegexOptions.IgnoreCase);

                _logger.LogInformation($"Combined text length: {combinedText.Length}");
                return combinedText.Trim();
            }

            _logger.LogInformation("Returning original text after cleaning");
            return text;
        }

        private string GenerateFallbackText(string topic = "", string difficulty = "medium", int length = 300)
        {
            _logger.LogInformation("Generating fallback text");

            var easyTexts = new[]
            {
                "Быстрая коричневая лиса прыгает через ленивую собаку. Это предложение содержит все буквы алфавита. Оно часто используется для проверки клавиатур и шрифтов. Печатайте этот текст медленно и аккуратно, следя за каждой буквой. Практика делает совершенство.",
                "Сегодня прекрасный день для прогулки в парке. Солнце светит ярко, и птицы поют на деревьях. Многие люди гуляют со своими собаками или просто наслаждаются хорошей погодой. Некоторые читают книги на скамейках, другие играют в спортивные игры на траве.",
                "Кошки - популярные домашние животные во многих странах мира. Они независимы, чистоплотны и не требуют много внимания. Кошки могут спать до 16 часов в день. Они отличные охотники и могут видеть в темноте лучше, чем люди. Многие люди любят кошек за их мягкий мех и успокаивающее мурлыканье."
            };

            var mediumTexts = new[]
            {
                "Программирование - это процесс создания набора инструкций, которые говорят компьютеру, как выполнить задачу. Программирование может быть выполнено с использованием различных языков программирования, таких как JavaScript, Python и C++. Каждый язык имеет свои сильные и слабые стороны и может быть более подходящим для определенных типов задач.",
                "Интернет - глобальная система взаимосвязанных компьютерных сетей, которая использует стандартный набор протоколов для обслуживания миллиардов пользователей по всему миру. Он состоит из миллионов частных, публичных, академических, деловых и правительственных сетей, которые связаны между собой широким спектром электронных, беспроводных и оптических сетевых технологий.",
                "Искусственный интеллект (ИИ) - это область компьютерной науки, которая подчеркивает создание интеллектуальных машин, которые работают и реагируют как люди. Некоторые из видов деятельности, которые компьютеры с искусственным интеллектом предназначены для выполнения, включают распознавание речи, обучение, планирование и решение проблем."
            };

            var hardTexts = new[]
            {
                "В квантовой механике принцип неопределенности Гейзенберга утверждает, что невозможно одновременно точно измерить положение и импульс частицы. Это фундаментальное ограничение, которое имеет глубокие последствия для нашего понимания физического мира. Оно показывает, что на квантовом уровне существует неустранимая неопределенность, которая не является результатом ограничений наших измерительных приборов.",
                "Блокчейн - это распределенная база данных, которая поддерживает постоянно растущий список записей, называемых блоками. Каждый блок содержит временную метку и ссылку на предыдущий блок. Криптография обеспечивает, что пользователи могут редактировать только те части блокчейна, которыми они 'владеют', имея закрытые ключи, необходимые для записи в файл. Кроме того, криптография гарантирует, что все могут прозрачно видеть, что изменения были внесены в блокчейн.",
                "Нейронные сети - это вычислительные системы, вдохновленные биологическими нейронными сетями, составляющими мозг животных. Такие системы учатся выполнять задачи, рассматривая примеры, обычно без программирования с конкретными правилами. Например, при распознавании изображений они могут научиться идентифицировать изображения, содержащие кошек, анализируя примеры изображений, которые были вручную помечены как 'кошка' или 'без кошки', и используя результаты для идентификации кошек в других изображениях."
            };

            string[] textsArray;
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

            var random = new Random();
            var selectedText = textsArray[random.Next(textsArray.Length)];

            if (!string.IsNullOrEmpty(topic))
            {
                selectedText = $"Тема этого текста - {topic}. " + selectedText;
            }

            _logger.LogInformation($"Generated fallback text ({selectedText.Length} characters)");
            return selectedText;
        }
    }
}
