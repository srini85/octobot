namespace OctoBot.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IBotInstanceRepository BotInstances { get; }
    IConversationRepository Conversations { get; }
    IMessageRepository Messages { get; }
    IRepository<Entities.ChannelConfig> ChannelConfigs { get; }
    IRepository<Entities.PluginConfig> PluginConfigs { get; }
    IRepository<Entities.LLMConfig> LLMConfigs { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
