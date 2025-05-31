// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Source: https://github.com/dotnet/aspnetcore/blob/main/src/Components/WebAssembly/Server/src/Circuits/RemoteNavigationManager.cs#L188
// Modified to be a DelegatingHandler and to use a specific cookie name.

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging; // Added for logging

namespace keyraces.Server // Ensure this namespace matches your project structure
{
    public class BlazorCookieHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<BlazorCookieHandler> _logger;
        private const string CookieName = "KeyRaces"; // Make sure this matches your app's auth cookie name

        public BlazorCookieHandler(IHttpContextAccessor httpContextAccessor, ILogger<BlazorCookieHandler> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext != null)
            {
                // Check if the cookie already exists in the request (e.g., added by another handler or default behavior)
                if (!request.Headers.Contains("Cookie"))
                {
                    var cookieValue = httpContext.Request.Cookies[CookieName];
                    if (!string.IsNullOrEmpty(cookieValue))
                    {
                        request.Headers.Add("Cookie", $"{CookieName}={cookieValue}");
                        _logger.LogInformation("BlazorCookieHandler: Added '{CookieName}' cookie to outgoing request to {RequestUri}.", CookieName, request.RequestUri);
                    }
                    else
                    {
                        _logger.LogWarning("BlazorCookieHandler: '{CookieName}' cookie not found in HttpContext for request to {RequestUri}.", CookieName, request.RequestUri);
                    }
                }
                else
                {
                    // If Cookie header already exists, you might want to log its current value or ensure your cookie is appended correctly
                    _logger.LogInformation("BlazorCookieHandler: Cookie header already present for request to {RequestUri}. Value: {CookieHeaderValue}", request.RequestUri, request.Headers.GetValues("Cookie"));
                }
            }
            else
            {
                _logger.LogWarning("BlazorCookieHandler: HttpContext is null, cannot add cookie for request to {RequestUri}.", request.RequestUri);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
