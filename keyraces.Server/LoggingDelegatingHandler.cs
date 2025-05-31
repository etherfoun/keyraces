using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace keyraces.Server // Ensure this namespace matches your project structure
{
    public class LoggingDelegatingHandler : DelegatingHandler
    {
        private readonly ILogger<LoggingDelegatingHandler> _logger;

        public LoggingDelegatingHandler(ILogger<LoggingDelegatingHandler> logger)
        {
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Outgoing HTTP Request:");
            sb.AppendLine($"Method: {request.Method}, RequestUri: {request.RequestUri}, Version: {request.Version}");

            sb.AppendLine("Headers:");
            foreach (var header in request.Headers)
            {
                sb.AppendLine($"- {header.Key}: {string.Join(", ", header.Value)}");
            }

            if (request.Content != null)
            {
                sb.AppendLine("Content Headers:");
                foreach (var header in request.Content.Headers)
                {
                    sb.AppendLine($"- {header.Key}: {string.Join(", ", header.Value)}");
                }
                // Optionally log content, be careful with large payloads
                // sb.AppendLine($"Content: {await request.Content.ReadAsStringAsync(cancellationToken)}");
            }
            _logger.LogInformation(sb.ToString());
            sb.Clear();


            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            sb.AppendLine("Incoming HTTP Response:");
            sb.AppendLine($"StatusCode: {(int)response.StatusCode} {response.ReasonPhrase}, Version: {response.Version}");
            sb.AppendLine("Headers:");
            foreach (var header in response.Headers)
            {
                sb.AppendLine($"- {header.Key}: {string.Join(", ", header.Value)}");
            }

            if (response.Content != null)
            {
                sb.AppendLine("Content Headers:");
                foreach (var header in response.Content.Headers)
                {
                    sb.AppendLine($"- {header.Key}: {string.Join(", ", header.Value)}");
                }
                // Optionally log content, be careful with large payloads
                // string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                // sb.AppendLine($"Content: {responseBody.Substring(0, Math.Min(responseBody.Length, 500))}..."); // Log first 500 chars
            }
            _logger.LogInformation(sb.ToString());

            return response;
        }
    }
}
