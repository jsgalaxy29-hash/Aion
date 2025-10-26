using Aion.AppHost;
using Aion.AppHost.Components;
using Aion.AppHost.Services;
using Aion.DataEngine.Interfaces;
using Aion.DataEngine.Providers;
using Aion.DataEngine.Services;
using Aion.Domain.Contracts;
using Aion.Infrastructure;
using Aion.Infrastructure.Services;
using Aion.Infrastructure.Startup;
using Aion.Security;
using Aion.Security.Authentication;
using Aion.Security.Authorization;
using Aion.Security.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.FluentUI.AspNetCore.Components;

var builder = WebApplication.CreateBuilder(args);

// ===== Services Blazor =====
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddHttpClient();
builder.Services.AddFluentUIComponents();
builder.Services.AddRazorPages();

// ===== Database Contexts =====
var connectionString = builder.Configuration.GetConnectionString("AionDb")
    ?? "Server=localhost;Database=AionDb;Trusted_Connection=True;TrustServerCertificate=True;";

builder.Services.AddDbContext<AionDbContext>(opt =>
    opt.UseSqlServer(connectionString));

builder.Services.AddDbContext<SecurityDbContext>(opt =>
    opt.UseSqlServer(connectionString));

// ===== Data Provider (pour AionProvisioningService) =====
// TODO: Implémenter votre IDataProvider concret
builder.Services.AddScoped<IDataProvider>(_ =>
    new SqlServerDataProvider(connectionString));

builder.Services.AddSingleton<IClock, Aion.DataEngine.Interfaces.SystemClock>();

// ===== Authentication & Authorization =====
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/login";
        o.LogoutPath = "/logout";
        o.AccessDeniedPath = "/access-denied";
        o.SlidingExpiration = true;
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Claims Transformation
builder.Services.AddScoped<IClaimsTransformation, AionClaimsTransformation>();

// Policy Provider
builder.Services.AddSingleton<IAuthorizationPolicyProvider, RightPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, RightHandler>();

// ===== Services Aion =====
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IRightService, RightService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMenuProvider, MenuProvider>();
builder.Services.AddScoped<IAionThemeService, AionThemeService>();

// Provisioning
builder.Services.AddScoped<IAionProvisioningService, AionProvisioningService>();
builder.Services.AddScoped<StartupOrchestrator>();

// ===== Build Application =====
var app = builder.Build();

// ===== Configuration Pipeline =====
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// ===== Database Initialization =====
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Option 1 : Avec AionProvisioningService (si IDataProvider implémenté)
        await StartupOrchestrator.InitializeDatabaseAsync(app.Services);

        // Option 2 : EF Core seulement (si pas de IDataProvider)
        logger.LogInformation("🔄 Initialisation de la base de données...");

        var aionDb = scope.ServiceProvider.GetRequiredService<AionDbContext>();
        var securityDb = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();

        // Créer les bases si elles n'existent pas
        await aionDb.Database.EnsureCreatedAsync();
        await securityDb.Database.EnsureCreatedAsync();

        // Seed des données de sécurité
        await Aion.Infrastructure.Seeders.SecuritySeeder.SeedAsync(securityDb);

        logger.LogInformation("✅ Base de données initialisée");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Erreur lors de l'initialisation de la base");
        throw;
    }
}

// ===== Mapping =====
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.MapFallbackToPage("/_Host");

app.MapRazorPages();

// ===== Démarrage =====
app.Logger.LogInformation("🚀 Aion démarré sur {Urls}", string.Join(", ", app.Urls));
app.Logger.LogInformation("🔑 Connexion par défaut : admin / admin (TenantId: 1)");

app.Run();