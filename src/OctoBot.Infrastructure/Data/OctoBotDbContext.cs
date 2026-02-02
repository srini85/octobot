using Microsoft.EntityFrameworkCore;
using OctoBot.Core.Entities;

namespace OctoBot.Infrastructure.Data;

public class OctoBotDbContext : DbContext
{
    public OctoBotDbContext(DbContextOptions<OctoBotDbContext> options) : base(options)
    {
    }

    public DbSet<BotInstance> BotInstances => Set<BotInstance>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<ChannelConfig> ChannelConfigs => Set<ChannelConfig>();
    public DbSet<PluginConfig> PluginConfigs => Set<PluginConfig>();
    public DbSet<LLMConfig> LLMConfigs => Set<LLMConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BotInstance>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.SystemPrompt).HasMaxLength(10000);
            entity.HasIndex(e => e.Name).IsUnique();

            entity.HasOne(e => e.DefaultLLMConfig)
                .WithMany(l => l.BotInstances)
                .HasForeignKey(e => e.DefaultLLMConfigId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ChannelId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.HasIndex(e => new { e.BotInstanceId, e.ChannelId, e.UserId });

            entity.HasOne(e => e.BotInstance)
                .WithMany(b => b.Conversations)
                .HasForeignKey(e => e.BotInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Role).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.HasIndex(e => e.ConversationId);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChannelConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ChannelType).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => new { e.BotInstanceId, e.ChannelType }).IsUnique();

            entity.HasOne(e => e.BotInstance)
                .WithMany(b => b.ChannelConfigs)
                .HasForeignKey(e => e.BotInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PluginConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PluginId).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => new { e.BotInstanceId, e.PluginId }).IsUnique();

            entity.HasOne(e => e.BotInstance)
                .WithMany(b => b.PluginConfigs)
                .HasForeignKey(e => e.BotInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LLMConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ProviderType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ModelId).HasMaxLength(100);
            entity.Property(e => e.Endpoint).HasMaxLength(500);
            entity.HasIndex(e => e.Name).IsUnique();
        });
    }
}
