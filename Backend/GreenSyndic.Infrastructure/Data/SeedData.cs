using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GreenSyndic.Infrastructure.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GreenSyndicDbContext>();

        // Si la DB n'existe pas, on la crée. Si elle existe, on ne touche à rien.
        await db.Database.EnsureCreatedAsync();
    }
}
