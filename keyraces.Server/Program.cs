using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using keyraces.Infrastructure;
using keyraces.Infrastructure.Data;
using keyraces.Infrastructure.Repositories;
using keyraces.Infrastructure.Security;
using keyraces.Infrastructure.Services;
using keyraces.Server;
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
using Blazored.LocalStorage;

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

builder.Services.AddTransient<LoggingDelegatingHandler>();
builder.Services.AddTransient<BlazorCookieHandler>();

builder.Services.AddHttpClient("BlazorAppClient", client =>
{
})
.AddHttpMessageHandler<BlazorCookieHandler>()
.AddHttpMessageHandler<LoggingDelegatingHandler>();

builder.Services.AddScoped(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var client = httpClientFactory.CreateClient("BlazorAppClient");

    var navigationManager = sp.GetRequiredService<NavigationManager>();
    client.BaseAddress = new Uri(navigationManager.BaseUri);

    return client;
});


builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpContextAccessor();

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
builder.Services.AddBlazoredLocalStorage();

builder.Services.AddDbContextFactory<AppDbContext>(opts =>
  opts.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

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

builder.Services.AddAuthentication(options => {
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
})
    .AddJwtBearer(options =>
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
            },
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError("JWT Authentication failed: {Error}", context.Exception.Message);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "KeyRaces";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    options.AccessDeniedPath = "/access-denied";
    options.LoginPath = "/login";
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddAuthorization(options => {
    options.AddPolicy("SignalRPolicy", policy =>
    {
        policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
        policy.AuthenticationSchemes.Add(IdentityConstants.ApplicationScheme);
        policy.RequireAuthenticatedUser();
    });
});


builder.Services.AddScoped<IPasswordHasher, AspNetPasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();

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

builder.Services.AddScoped<ICompetitionService, CompetitionService>();
builder.Services.AddScoped<ICompetitionParticipantService, CompetitionParticipantService>();
builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();
builder.Services.AddScoped<IAchievementService, AchievementService>();
builder.Services.AddScoped<IUserAchievementService, UserAchievementService>();
builder.Services.AddScoped<IAchievementCheckerService, AchievementCheckerService>();
builder.Services.AddScoped<ITextSnippetService, TextSnippetService>();
builder.Services.AddScoped<ITypingSessionService, TypingSessionService>();
builder.Services.AddScoped<ITypingStatisticService, TypingStatisticService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();
builder.Services.AddScoped<ICompetitionLobbyService, RedisCompetitionLobbyService>();

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var redisConfig = builder.Configuration.GetSection("Redis");
    var connectionString = redisConfig["Connection"] ?? "redis:6379";
    var configuration = ConfigurationOptions.Parse(connectionString);
    configuration.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(configuration);
});

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
    var ollamaHttpClient = httpClientFactory.CreateClient("OllamaClient");
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

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<TypingHub>("/typingHub");
app.MapHub<TypingHub>("/hub/typing");
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

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

        await keyraces.Infrastructure.Data.Seed.AchievementSeeder.SeedAsync(ctx, logger);


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
