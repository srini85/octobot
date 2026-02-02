using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OctoBot.Core.Interfaces;
using OctoBot.Infrastructure.Data;
using OctoBot.Infrastructure.Repositories;
using OctoBot.Infrastructure.Services;

namespace OctoBot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<OctoBotDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IConversationMemory, ConversationMemory>();

        return services;
    }

    public static async Task InitializeDatabaseAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OctoBotDbContext>();
        await context.Database.EnsureCreatedAsync();
    }
}
