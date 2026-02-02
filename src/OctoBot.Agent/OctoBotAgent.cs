using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
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
    private readonly IConversationMemory _memory;
    private Kernel? _kernel;

    public Guid BotInstanceId => _botInstance.Id;

    public OctoBotAgent(
        BotInstance botInstance,
        ILLMProviderRegistry llmRegistry,
        IPluginRegistry pluginRegistry,
        IConversationMemory memory)
    {
        _botInstance = botInstance;
        _llmRegistry = llmRegistry;
        _pluginRegistry = pluginRegistry;
        _memory = memory;
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

        _kernel = await provider.CreateKernelAsync(llmConfig, ct);

        // Register plugins
        foreach (var pluginConfig in _botInstance.PluginConfigs.Where(p => p.IsEnabled))
        {
            var plugin = _pluginRegistry.GetPlugin(pluginConfig.PluginId);
            if (plugin != null)
            {
                var builder = Kernel.CreateBuilder();
                plugin.RegisterFunctions(builder);
            }
        }
    }

    public async Task<string> ProcessMessageAsync(IncomingMessage message, CancellationToken ct = default)
    {
        if (_kernel == null)
        {
            throw new InvalidOperationException("Agent not initialized. Call InitializeAsync first.");
        }

        var conversation = await _memory.GetOrCreateAsync(
            _botInstance.Id,
            message.ChannelId,
            message.UserId,
            ct);

        var history = await _memory.GetHistoryAsync(conversation.Id, 50, ct);
        var chatHistory = BuildChatHistory(history);
        chatHistory.AddUserMessage(message.Content);

        var chatService = _kernel.GetRequiredService<IChatCompletionService>();
        var response = await chatService.GetChatMessageContentsAsync(chatHistory, cancellationToken: ct);
        var responseContent = response.FirstOrDefault()?.Content ?? "";

        // Save messages to memory
        await _memory.AddMessageAsync(conversation.Id, new ChatMessage(
            MessageRole.User,
            message.Content,
            message.Timestamp
        ), ct);

        await _memory.AddMessageAsync(conversation.Id, new ChatMessage(
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
        if (_kernel == null)
        {
            throw new InvalidOperationException("Agent not initialized. Call InitializeAsync first.");
        }

        var conversation = await _memory.GetOrCreateAsync(
            _botInstance.Id,
            message.ChannelId,
            message.UserId,
            ct);

        var history = await _memory.GetHistoryAsync(conversation.Id, 50, ct);
        var chatHistory = BuildChatHistory(history);
        chatHistory.AddUserMessage(message.Content);

        var chatService = _kernel.GetRequiredService<IChatCompletionService>();
        var fullResponse = new StringBuilder();

        await foreach (var chunk in chatService.GetStreamingChatMessageContentsAsync(chatHistory, cancellationToken: ct))
        {
            if (chunk.Content != null)
            {
                fullResponse.Append(chunk.Content);
                yield return chunk.Content;
            }
        }

        // Save messages to memory after streaming completes
        await _memory.AddMessageAsync(conversation.Id, new ChatMessage(
            MessageRole.User,
            message.Content,
            message.Timestamp
        ), ct);

        await _memory.AddMessageAsync(conversation.Id, new ChatMessage(
            MessageRole.Assistant,
            fullResponse.ToString(),
            DateTime.UtcNow
        ), ct);
    }

    private ChatHistory BuildChatHistory(IReadOnlyList<ChatMessage> messages)
    {
        var chatHistory = new ChatHistory();

        if (!string.IsNullOrEmpty(_botInstance.SystemPrompt))
        {
            chatHistory.AddSystemMessage(_botInstance.SystemPrompt);
        }

        foreach (var msg in messages)
        {
            switch (msg.Role)
            {
                case MessageRole.User:
                    chatHistory.AddUserMessage(msg.Content);
                    break;
                case MessageRole.Assistant:
                    chatHistory.AddAssistantMessage(msg.Content);
                    break;
                case MessageRole.System:
                    chatHistory.AddSystemMessage(msg.Content);
                    break;
            }
        }

        return chatHistory;
    }
}
