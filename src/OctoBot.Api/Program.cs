using OctoBot.Agent;
using OctoBot.Application;
using OctoBot.Channels.Abstractions;
using OctoBot.Channels.Telegram;
using OctoBot.Infrastructure;
using OctoBot.LLM.Abstractions;
using OctoBot.LLM.Anthropic;
using OctoBot.LLM.Ollama;
using OctoBot.LLM.OpenAI;
using OctoBot.Plugins.Abstractions;
using OctoBot.Plugins.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add OctoBot services
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=octobot.db";

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddApplication();
builder.Services.AddAgent();

// LLM Providers
builder.Services.AddOpenAIProvider();
builder.Services.AddAnthropicProvider();
builder.Services.AddOllamaProvider();
builder.Services.AddSingleton<ILLMProviderRegistry, LLMProviderRegistry>();

// Channels
builder.Services.AddTelegramChannel();
builder.Services.AddSingleton<IChannelRegistry, ChannelRegistry>();

// Plugins
builder.Services.AddCorePlugins();
builder.Services.AddSingleton<IPluginRegistry, PluginRegistry>();

var app = builder.Build();

// Initialize database
await OctoBot.Infrastructure.DependencyInjection.InitializeDatabaseAsync(app.Services);

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseStaticFiles();
app.MapControllers();

// Fallback for SPA
app.MapFallbackToFile("index.html");

app.Run();
