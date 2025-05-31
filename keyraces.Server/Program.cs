using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using keyraces.Infrastructure;
using keyraces.Infrastructure.Data;
using keyraces.Infrastructure.Repositories;
using keyraces.Infrastructure.Security;
using keyraces.Infrastructure.Services;
using keyraces.Server; // Для LoggingDelegatingHandler и BlazorCookieHandler
using keyraces.Server.Hubs;
using keyraces.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
    });

    options.ConfigureEndpointDefaults(listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
    });
});

// Регистрация DelegatingHandlers
builder.Services.AddTransient<LoggingDelegatingHandler>();
builder.Services.AddTransient<BlazorCookieHandler>();

// Удаляем старую регистрацию HttpClient
// builder.Services.AddScoped<HttpClient>(sp =>
// {
//     var nav = sp.GetRequiredService<NavigationManager>();
//     return new HttpClient { BaseAddress = new Uri(nav.BaseUri) };
// });
// builder.Services.AddHttpClient(); // Эта общая регистрация также заменяется подходом с фабрикой

// Конфигурируем именованный HttpClient с логирующим и cookie обработчиками
// BlazorCookieHandler должен идти ПЕРЕД LoggingDelegatingHandler, чтобы логирующий увидел добавленный cookie
builder.Services.AddHttpClient("BlazorAppClient", client =>
{
    // BaseAddress будет установлен при создании клиента в регистрации AddScoped ниже
})
.AddHttpMessageHandler<BlazorCookieHandler>()      // Этот обработчик пытается добавить cookie
.AddHttpMessageHandler<LoggingDelegatingHandler>(); // Этот обработчик логирует запрос (теперь, надеюсь, включая cookie)

// Регистрируем HttpClient для DI, чтобы он использовал фабрику и устанавливал BaseAddress
// Это гарантирует, что @inject HttpClient в Blazor компонентах получит сконфигурированный клиент.
builder.Services.AddScoped(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var client = httpClientFactory.CreateClient("BlazorAppClient"); // Используем именованный клиент

    var navigationManager = sp.GetRequiredService<NavigationManager>();
    client.BaseAddress = new Uri(navigationManager.BaseUri);

    return client;
});


// Добавляем сервисы в контейнер.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpContextAccessor(); // Крайне важен для BlazorCookieHandler

builder.Services.AddStackExchangeRedisCache(options =>
{
    var redisConnection = builder.Configuration.GetSection("Redis")["Connection"] ?? "redis:6379";
    options.Configuration = redisConnection;
    options.InstanceName = "keyraces:";
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


builder.Services.AddScoped<ThemeService>();

builder.Services.AddDbContextFactory<AppDbContext>(opts =>
  opts.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Настройка ASP.NET Core Identity
builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(options => {
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 0;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Конфигурация аутентификации
builder.Services.AddAuthentication(options => {
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
})
    .AddJwtBearer(options => // JWT для SignalR или других специфичных API
    {
        options.SaveToken = true;
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JWT:Issuer"],
            ValidAudience = builder.Configuration["JWT:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"] ?? throw new InvalidOperationException("JWT:Secret not configured"))),
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (string.IsNullOrEmpty(accessToken))
                {
                    accessToken = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                }
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    (path.StartsWithSegments("/hub/typing") || path.StartsWithSegments("/typingHub")))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// Настройка cookie приложения (используется Identity)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "KeyRaces"; // Это имя cookie, которое будет искать BlazorCookieHandler
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    options.AccessDeniedPath = "/access-denied";
    options.LoginPath = "/login";
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddAuthorization(options => {
    // Здесь можно определить политики, если необходимо
});


builder.Services.AddScoped<IPasswordHasher, AspNetPasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();

// Репозитории
builder.Services.AddScoped<ICompetitionRepository, CompetitionRepository>();
builder.Services.AddScoped<ICompetitionParticipantRepository, CompetitionParticipantRepository>();
builder.Services.AddScoped<ILeaderboardRepository, LeaderboardRepository>();
builder.Services.AddScoped<IAchievementRepository, AchievementRepository>();
builder.Services.AddScoped<IUserAchievementRepository, UserAchievementRepository>();
builder.Services.AddScoped<IUserProfileRepository, UserProfileRepository>();
builder.Services.AddScoped<ITextSnippetRepository, TextSnippetRepository>();
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<ITypingStatisticRepository, TypingStatisticRepository>();
builder.Services.AddScoped<ITypingSessionRepository, TypingSessionRepository>();


// Сервисы
builder.Services.AddScoped<ICompetitionService, CompetitionService>();
builder.Services.AddScoped<ICompetitionParticipantService, CompetitionParticipantService>();
builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();
builder.Services.AddScoped<IAchievementService, AchievementService>();
builder.Services.AddScoped<ITextSnippetService, TextSnippetService>();
builder.Services.AddScoped<ITypingSessionService, TypingSessionService>();
builder.Services.AddScoped<ITypingStatisticService, TypingStatisticService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IUserAchievementService, UserAchievementService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();
builder.Services.AddScoped<ICompetitionLobbyService, RedisCompetitionLobbyService>();

// Регистрация Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var redisConfig = builder.Configuration.GetSection("Redis");
    var connectionString = redisConfig["Connection"] ?? "redis:6379";
    var configuration = ConfigurationOptions.Parse(connectionString);
    configuration.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(configuration);
});

// Сервис генерации текста (OllamaClient)
builder.Services.AddHttpClient("OllamaClient", (serviceProvider, client) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var apiUrl = configuration["Ollama:ApiUrl"] ?? "http://localhost:11434/api/generate";
    var timeoutSeconds = configuration.GetValue<int>("Ollama:TimeoutSeconds", 120);
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    var baseUrl = apiUrl.Replace("/api/generate", "");
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddScoped<ITextGenerationService>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
    var logger = serviceProvider.GetRequiredService<ILogger<LocalLLMTextGenerationService>>();
    var apiUrl = configuration["Ollama:ApiUrl"] ?? "http://localhost:11434/api/generate";
    var modelName = configuration["Ollama:ModelName"] ?? "llama2";
    var ollamaHttpClient = httpClientFactory.CreateClient("OllamaClient"); // Используем именованный клиент для Ollama
    return new LocalLLMTextGenerationService(logger, ollamaHttpClient, serviceProvider, apiUrl, modelName);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.MaximumReceiveMessageSize = 102400;
});
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "KeyRaces API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }}, Array.Empty<string>() }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "KeyRaces API v1"); });
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();

app.UseSession(); // Убедитесь, что UseSession вызывается перед UseAuthentication и UseAuthorization, если сессии используются для чего-то еще

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<TypingHub>("/typingHub");
app.MapHub<TypingHub>("/hub/typing");
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Инициализация и сидинг базы данных
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var ctx = services.GetRequiredService<AppDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Checking database connection...");
        if (!await ctx.Database.CanConnectAsync()) { logger.LogError("Cannot connect to database!"); throw new Exception("Database connection failed"); }

        logger.LogInformation("Applying database migrations...");
        await ctx.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully.");

        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        string[] roleNames = { "Admin", "User" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
                logger.LogInformation("{RoleName} role created.", roleName);
            }
        }

        var adminEmail = builder.Configuration["AdminUser:Email"] ?? "admin@keyraces.com";
        var adminPassword = builder.Configuration["AdminUser:Password"] ?? "Admin123!";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                logger.LogInformation("Default admin user created: {AdminEmail}", adminEmail);
            }
            else { logger.LogError("Failed to create admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description))); }
        }

        if (app.Environment.IsDevelopment())
        {
            var testUsers = new[]
            {
                new { Email = "user1@test.com", Name = "TestUser1", Password = "Test123!" },
                new { Email = "user2@test.com", Name = "TestUser2", Password = "Test123!" },
                new { Email = "user3@test.com", Name = "TestUser3", Password = "Test123!" }
            };
            foreach (var testUserData in testUsers)
            {
                var existingUser = await userManager.FindByEmailAsync(testUserData.Email);
                if (existingUser == null)
                {
                    var testUserIdentity = new IdentityUser { UserName = testUserData.Email, Email = testUserData.Email, EmailConfirmed = true };
                    var result = await userManager.CreateAsync(testUserIdentity, testUserData.Password);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(testUserIdentity, "User");
                        var profileService = services.GetRequiredService<IUserProfileService>();
                        await profileService.CreateProfileAsync(testUserIdentity.Id, testUserData.Name);
                        logger.LogInformation($"Test user created: {testUserData.Email} / {testUserData.Password}");
                    }
                }
            }
        }

        if (!ctx.TextSnippets.Any())
        {
            logger.LogInformation("Adding default text snippets...");
            ctx.TextSnippets.Add(new TextSnippet { Title = "Easy Text", Content = "The quick brown fox jumps over the lazy dog. This pangram contains all the letters of the English alphabet.", Difficulty = "easy", Language = "en", IsGenerated = false, CreatedAt = DateTime.UtcNow });
            await ctx.SaveChangesAsync();
            logger.LogInformation("Default text snippets added successfully.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while setting up the database.");
        throw;
    }
}

app.Run();
