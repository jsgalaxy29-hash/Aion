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

        builder.Services.AddSingleton<IClock, SystemClock>();
        builder.Services.AddScoped<IAgendaService, AgendaService>();

        return builder.Build();
    }
}
