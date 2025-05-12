using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace KeyRacesTextGenerator
{
    public class TextGeneratorFunction
    {
        private readonly ILogger _logger;
        private readonly TextGenerator _textGenerator;

        public TextGeneratorFunction(ILoggerFactory loggerFactory, TextGenerator textGenerator)
        {
            _logger = loggerFactory.CreateLogger<TextGeneratorFunction>();
            _textGenerator = textGenerator;
        }

        [Function("GenerateText")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request for text generation");

            // Чтение тела запроса
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonSerializer.Deserialize<TextGenerationRequest>(requestBody);

            if (data == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Please provide a valid request body");
                return badResponse;
            }

            try
            {
                // Генерация текста
                string generatedText = await _textGenerator.GenerateAsync(
                    data.Topic ?? "",
                    data.Difficulty ?? "medium",
                    data.Length);

                // Создание ответа
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json");

                await response.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    text = generatedText,
                    metadata = new
                    {
                        topic = data.Topic,
                        difficulty = data.Difficulty,
                        length = data.Length,
                        timestamp = DateTime.UtcNow
                    }
                }));

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating text");

                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error generating text: {ex.Message}");
                return errorResponse;
            }
        }
    }

    public class TextGenerationRequest
    {
        public string Topic { get; set; }
        public string Difficulty { get; set; }
        public int Length { get; set; } = 300;
    }
}