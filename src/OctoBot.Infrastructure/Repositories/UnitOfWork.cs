using Microsoft.EntityFrameworkCore.Storage;
using OctoBot.Core.Entities;
using OctoBot.Core.Interfaces;
using OctoBot.Infrastructure.Data;

namespace OctoBot.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly OctoBotDbContext _context;
    private IDbContextTransaction? _transaction;

    private IBotInstanceRepository? _botInstances;
    private IConversationRepository? _conversations;
    private IMessageRepository? _messages;
    private IRepository<ChannelConfig>? _channelConfigs;
    private IRepository<PluginConfig>? _pluginConfigs;
    private IRepository<LLMConfig>? _llmConfigs;

    public UnitOfWork(OctoBotDbContext context)
    {
        _context = context;
    }

    public IBotInstanceRepository BotInstances =>
        _botInstances ??= new BotInstanceRepository(_context);

    public IConversationRepository Conversations =>
        _conversations ??= new ConversationRepository(_context);

    public IMessageRepository Messages =>
        _messages ??= new MessageRepository(_context);

    public IRepository<ChannelConfig> ChannelConfigs =>
        _channelConfigs ??= new Repository<ChannelConfig>(_context);

    public IRepository<PluginConfig> PluginConfigs =>
        _pluginConfigs ??= new Repository<PluginConfig>(_context);

    public IRepository<LLMConfig> LLMConfigs =>
        _llmConfigs ??= new Repository<LLMConfig>(_context);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(ct);
    }

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
