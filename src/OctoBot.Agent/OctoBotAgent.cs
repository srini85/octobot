using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using OctoBot.Core.Entities;
using OctoBot.Core.Interfaces;
using OctoBot.Core.ValueObjects;
using OctoBot.LLM.Abstractions;
using OctoBot.Plugins.Abstractions;

namespace OctoBot.Agent;

public class OctoBotAgent : IOctoBotAgent
{
    private readonly BotInstance _botInstance;
    private readonly ILLMProviderRegistry _llmRegistry;
    private readonly IPluginRegistry _pluginRegistry;
    private readonly IServiceScopeFactory _scopeFactory;
    private AIAgent? _agent;

    public Guid BotInstanceId => _botInstance.Id;

    public OctoBotAgent(
        BotInstance botInstance,
        ILLMProviderRegistry llmRegistry,
        IPluginRegistry pluginRegistry,
        IServiceScopeFactory scopeFactory)
    {
        _botInstance = botInstance;
        _llmRegistry = llmRegistry;
        _pluginRegistry = pluginRegistry;
        _scopeFactory = scopeFactory;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_botInstance.DefaultLLMConfig == null)
        {
            throw new InvalidOperationException("Bot instance must have a default LLM configuration");
        }

        var provider = _llmRegistry.GetProvider(_botInstance.DefaultLLMConfig.ProviderType);
        if (provider == null)
        {
            throw new InvalidOperationException($"LLM provider '{_botInstance.DefaultLLMConfig.ProviderType}' not found");
        }

        var llmConfig = new LLMConfiguration
        {
            ProviderName = _botInstance.DefaultLLMConfig.ProviderType,
            ModelId = _botInstance.DefaultLLMConfig.ModelId ?? "gpt-4",
            ApiKey = _botInstance.DefaultLLMConfig.ApiKey,
            Endpoint = _botInstance.DefaultLLMConfig.Endpoint
        };

        var chatClient = await provider.CreateChatClientAsync(llmConfig, ct);

        // Collect plugin functions and configure plugins with their stored settings
        var tools = new List<AITool>();
        foreach (var pluginConfig in _botInstance.PluginConfigs.Where(p => p.IsEnabled))
        {
            var plugin = _pluginRegistry.GetPlugin(pluginConfig.PluginId);
            if (plugin != null)
            {
                // Configure plugin with stored settings if it supports configuration
                if (plugin is IConfigurablePlugin configurable)
                {
                    var settings = !string.IsNullOrEmpty(pluginConfig.Settings)
                        ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(pluginConfig.Settings) ?? new()
                        : new Dictionary<string, string>();
                    configurable.Configure(_botInstance.Id, settings, _scopeFactory);
                }

                // AIFunction is a subclass of AITool, cast directly
                tools.AddRange(plugin.GetFunctions().Cast<AITool>());
            }
        }

        // Create agent from chat client with tools
        _agent = chatClient.AsAIAgent(
            instructions: _botInstance.SystemPrompt,
            tools: tools
        );
    }

    public async Task<string> ProcessMessageAsync(IncomingMessage message, CancellationToken ct = default)
    {
        if (_agent == null)
        {
            throw new InvalidOperationException("Agent not initialized. Call InitializeAsync first.");
        }

        using var scope = _scopeFactory.CreateScope();
        var memory = scope.ServiceProvider.GetRequiredService<IConversationMemory>();

        var conversation = await memory.GetOrCreateAsync(
            _botInstance.Id,
            message.ChannelId,
            message.UserId,
            ct);

        var history = await memory.GetHistoryAsync(conversation.Id, 50, ct);
        var chatMessages = BuildChatMessages(history);
        chatMessages.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, message.Content));

        var response = await _agent.RunAsync(chatMessages, cancellationToken: ct);
        var responseContent = response.Text ?? "";

        // Save messages to memory
        await memory.AddMessageAsync(conversation.Id, new Core.ValueObjects.ChatMessage(
            MessageRole.User,
            message.Content,
            message.Timestamp
        ), ct);

        await memory.AddMessageAsync(conversation.Id, new Core.ValueObjects.ChatMessage(
            MessageRole.Assistant,
            responseContent,
            DateTime.UtcNow
        ), ct);

        return responseContent;
    }

    public async IAsyncEnumerable<string> ProcessMessageStreamAsync(
        IncomingMessage message,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (_agent == null)
        {
            throw new InvalidOperationException("Agent not initialized. Call InitializeAsync first.");
        }

        using var scope = _scopeFactory.CreateScope();
        var memory = scope.ServiceProvider.GetRequiredService<IConversationMemory>();

        var conversation = await memory.GetOrCreateAsync(
            _botInstance.Id,
            message.ChannelId,
            message.UserId,
            ct);

        var history = await memory.GetHistoryAsync(conversation.Id, 50, ct);
        var chatMessages = BuildChatMessages(history);
        chatMessages.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, message.Content));

        var fullResponse = new StringBuilder();

        await foreach (var update in _agent.RunStreamingAsync(chatMessages, cancellationToken: ct))
        {
            if (update.Text != null)
            {
                fullResponse.Append(update.Text);
                yield return update.Text;
            }
        }

        // Save messages to memory after streaming completes
        await memory.AddMessageAsync(conversation.Id, new Core.ValueObjects.ChatMessage(
            MessageRole.User,
            message.Content,
            message.Timestamp
        ), ct);

        await memory.AddMessageAsync(conversation.Id, new Core.ValueObjects.ChatMessage(
            MessageRole.Assistant,
            fullResponse.ToString(),
            DateTime.UtcNow
        ), ct);
    }

    private List<Microsoft.Extensions.AI.ChatMessage> BuildChatMessages(IReadOnlyList<Core.ValueObjects.ChatMessage> messages)
    {
        var chatMessages = new List<Microsoft.Extensions.AI.ChatMessage>();

        // System prompt is handled in agent creation, not in message history

        foreach (var msg in messages)
        {
            var role = msg.Role switch
            {
                MessageRole.User => ChatRole.User,
                MessageRole.Assistant => ChatRole.Assistant,
                MessageRole.System => ChatRole.System,
                _ => ChatRole.User
            };

            chatMessages.Add(new Microsoft.Extensions.AI.ChatMessage(role, msg.Content));
        }

        return chatMessages;
    }
}
