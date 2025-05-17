using keyraces.Infrastructure.Security;
using keyraces.Server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using keyraces.Core.Interfaces;
using keyraces.Infrastructure.Data;
using keyraces.Infrastructure.Repositories;
using keyraces.Infrastructure.Services;
using Microsoft.OpenApi.Models;
using keyraces.Server.Hubs;
using Microsoft.AspNetCore.Components;
using keyraces.Core.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using keyraces.Infrastructure;
using keyraces.Server;
using Microsoft.AspNetCore.Components.Authorization;
using keyraces.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<HttpClient>(sp =>
{
    var nav = sp.GetRequiredService<NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(nav.BaseUri) };
});

builder.Services.AddHttpClient();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddScoped<ThemeService>();

builder.Services.AddDbContextFactory<AppDbContext>(opts =>
  opts.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddDistributedMemoryCache();

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

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"])),
        ClockSkew = TimeSpan.Zero
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
    options.Cookie.HttpOnly = true;
});

builder.Services.AddAuthorization();

builder.Services.AddScoped<IPasswordHasher, AspNetPasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();

// Repositories
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


// Services
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
builder.Services.AddScoped<ITextGenerationService, LocalLLMTextGenerationService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<LocalLLMTextGenerationService>>();
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();

    var apiUrl = "http://localhost:11434/api/generate";
    var modelName = "llama2";

    return new LocalLLMTextGenerationService(logger, httpClient, sp, apiUrl, modelName);
});
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSignalR();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "KeyRaces API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "KeyRaces API v1");
    });

    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapHub<TypingHub>("/hub/typing");

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");


using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        if (!ctx.TextSnippets.Any())
        {
            logger.LogInformation("No text snippets found in database. Adding default texts.");

            ctx.TextSnippets.Add(new TextSnippet
            {
                Title = "������� �����",
                Content = "������� ���������� ���� ������� ����� ������� ������. ���� ����� �������� ��� ����� ��������.",
                Difficulty = "easy",
                Language = "ru",
                IsGenerated = false,
                CreatedAt = DateTime.UtcNow
            });

            ctx.TextSnippets.Add(new TextSnippet
            {
                Title = "������� �����",
                Content = "���������������� � ��� ������� �������� ������ ����������, ������� ��������� ����������, ��� ��������� ������. ���������������� ����� ����������� � �������������� ��������� ������ ����������������.",
                Difficulty = "medium",
                Language = "ru",
                IsGenerated = false,
                CreatedAt = DateTime.UtcNow
            });

            ctx.TextSnippets.Add(new TextSnippet
            {
                Title = "������� �����",
                Content = "� ������������ ����� ������������� ��������� (��), ������ ���������� �������� �����������, � ��� ���������, ��������������� ��������, � ������� �� ������������� ����������, ������������ ������ � ���������.",
                Difficulty = "hard",
                Language = "ru",
                IsGenerated = false,
                CreatedAt = DateTime.UtcNow
            });

            ctx.TextSnippets.Add(new TextSnippet
            {
                Title = "Easy Text",
                Content = "The quick brown fox jumps over the lazy dog. This pangram contains all the letters of the English alphabet.",
                Difficulty = "easy",
                Language = "en",
                IsGenerated = false,
                CreatedAt = DateTime.UtcNow
            });

            ctx.TextSnippets.Add(new TextSnippet
            {
                Title = "Medium Text",
                Content = "Programming is the process of creating a set of instructions that tell a computer how to perform a task. Programming can be done using a variety of computer programming languages.",
                Difficulty = "medium",
                Language = "en",
                IsGenerated = false,
                CreatedAt = DateTime.UtcNow
            });

            ctx.TextSnippets.Add(new TextSnippet
            {
                Title = "Hard Text",
                Content = "In computer science, artificial intelligence (AI), sometimes called machine intelligence, is intelligence demonstrated by machines, in contrast to the natural intelligence displayed by humans and animals.",
                Difficulty = "hard",
                Language = "en",
                IsGenerated = false,
                CreatedAt = DateTime.UtcNow
            });

            await ctx.SaveChangesAsync();
            logger.LogInformation("Default text snippets added successfully.");
        }
        else
        {
            var textsWithoutLanguage = await ctx.TextSnippets.Where(t => t.Language == null).ToListAsync();
            if (textsWithoutLanguage.Any())
            {
                logger.LogInformation($"Found {textsWithoutLanguage.Count} text snippets without language. Setting default language to 'ru'.");

                foreach (var text in textsWithoutLanguage)
                {
                    text.Language = "ru";
                }

                await ctx.SaveChangesAsync();
                logger.LogInformation("Updated text snippets with default language.");
            }

            logger.LogInformation($"Database already contains {await ctx.TextSnippets.CountAsync()} text snippets.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database with text snippets.");
    }
}

app.Run();
