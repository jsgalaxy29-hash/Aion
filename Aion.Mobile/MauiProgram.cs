using System.IO;
using Aion.Infrastructure;
using Aion.Infrastructure.Services;
using Aion.Infrastructure.Services.Agenda;
using Aion.Security;
using Aion.Domain.Agenda;
using Microsoft.EntityFrameworkCore;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
using Aion.DataEngine.Interfaces;

namespace Aion.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>();

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddFluentUIComponents();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // Spécifique à Aion.Mobile : base SQLite locale embarquée pour réutiliser les DbContext.
        builder.Services.AddDbContextFactory<AionDbContext>((serviceProvider, options) =>
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "aion_mobile.db3");
            options.UseSqlite($"Data Source={dbPath}");
        });

        builder.Services.AddDbContextFactory<SecurityDbContext>((serviceProvider, options) =>
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "aion_security.db3");
            options.UseSqlite($"Data Source={dbPath}");
        });

        builder.Services.AddSingleton<IClock, DataEngine.Interfaces.SystemClock>();
        builder.Services.AddScoped<IAgendaService, AgendaService>();

        var app = builder.Build();

        InitializeLocalDatabases(app.Services);

        return app;
    }

    private static void InitializeLocalDatabases(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger(nameof(MauiProgram));

        void EnsureDatabaseCreated<TContext>() where TContext : DbContext
        {
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TContext>>();
            using var context = factory.CreateDbContext();
            context.Database.EnsureCreated();
            logger?.LogInformation("Local database ready for {Context}", typeof(TContext).Name);
        }

        EnsureDatabaseCreated<AionDbContext>();
        EnsureDatabaseCreated<SecurityDbContext>();
    }
}
