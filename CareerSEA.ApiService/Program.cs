using CareerSEA.Data;
using CareerSEA.Services.Interfaces;
using CareerSEA.Services.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using System;
using System.Text;
using static CareerSEA.Services.Interfaces.IResourceRecommendationService;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<CareerSEADbContext>("webAppDb");

builder.Services.AddServiceDiscovery();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJobPostService,JobPostService>();
builder.Services.AddScoped<ILlamaInputService,LlamaInputService>();

builder.Services.AddScoped<ISkillGapService, SkillGapService>();


builder.Services.AddHttpClient<IOnetService, OnetService>(client =>
{
    client.BaseAddress = new Uri("https://api-v2.onetcenter.org/");

    var apiKey = builder.Configuration["Onet:ApiKey"];
    if (!string.IsNullOrEmpty(apiKey))
    {
        client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
    }
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddStandardResilienceHandler(); // Leverages Polly for automatic retries

builder.Services.AddHttpClient<IResourceRecommendationService, ResourceRecommendationService>(client =>
{
    client.BaseAddress = new Uri("https://api.search.brave.com/");

    var apiKey = builder.Configuration["Brave:ApiKey"];
    if (!string.IsNullOrEmpty(apiKey))
    {
        client.DefaultRequestHeaders.Add("X-Subscription-Token", apiKey);
    }
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddStandardResilienceHandler();

builder.AddOllamaApiClient("ollamaModel").AddChatClient();
builder.Services.AddHttpClient("ollamaModel_httpClient")
    .AddStandardResilienceHandler(options =>
    {
        // 1. Give it 5 minutes for the "Cold Start" on your 3050
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(300);

        // 2. TOTAL must be at least as long as the attempt
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(300);

        // 3. CRITICAL: This must be DOUBLE the Attempt Timeout (600s)
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(600);
    });

builder.Services.AddHttpClient<IExperiencePredictionService, ExperiencePredictionService>(client =>
{
    var aiUrl = builder.Configuration["services:aiservice:api:0"];

    if (string.IsNullOrEmpty(aiUrl))
    {
        aiUrl = "http://aiservice:8001";
    }
    if (aiUrl.Contains("azurecontainerapps.io") && aiUrl.StartsWith("http://"))
    {
        aiUrl = aiUrl.Replace("http://", "https://");
    }

    client.BaseAddress = new Uri(aiUrl);
    client.Timeout = TimeSpan.FromMinutes(3);
}).AddTransientHttpErrorPolicy(policy =>
    policy.WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) // 2s, 4s, 8s
    )
);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "CareerSEA API",
        Version = "v1"
    });

    // Define JWT Bearer Authentication scheme
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token like: Bearer {your_token}"
    });

    // Apply JWT authentication globally to all endpoints
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[] { }
        }
    });
});
// Add services to the container.
builder.Services.AddProblemDetails();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(
    options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "CareerSEA",
            ValidateAudience = true,
            ValidAudience = "MyUsers",
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("MyVerySecureSecretKeyHere53278!!@#$%*^*^^*^%&!")),
            ValidateIssuerSigningKey = true,
        };
    });
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CareerSEADbContext>();
    await db.Database.MigrateAsync();
}
app.UseRouting();
app.UseAuthentication();   
app.UseAuthorization();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();

app.Run();

