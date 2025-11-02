using Aion.Module.CRM;
using Aion.Module.SystemCatalog;
using Aion.Module.SecurityAdmin;
using Aion.AppHost;
using Aion.AppHost.Services;
using Aion.DataEngine.Interfaces;
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
using Microsoft.Extensions.DependencyInjection;
using Aion.DataEngine.Services;
using Aion.Infrastructure.Data;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// ===== Fluent UI =====
builder.Services.AddFluentUIComponents();

// ===== Services Blazor + Razor Pages =====
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRazorPages(); // IMPORTANT pour Login.cshtml
builder.Services.AddHttpClient();

// ===== Database Contexts =====
var connectionString = builder.Configuration.GetConnectionString("AionDb")
    ?? "Server=localhost;Database=AionDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;";

builder.Services.AddDbContext<AionDbContext>(opt =>
    opt.UseSqlServer(connectionString));

builder.Services.AddDbContext<SecurityDbContext>(opt =>
    opt.UseSqlServer(connectionString));

// ===== Authentication & Authorization =====
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/login";
        o.LogoutPath = "/logout";
        o.AccessDeniedPath = "/access-denied";
        o.SlidingExpiration = true;
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        o.Cookie.HttpOnly = true;
        o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

builder.Services.AddAuthorization(options =>
{
    // Policy par défaut : authentifié (sauf routes explicitement [AllowAnonymous])
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
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<IUserContext, HttpContextUserContext>();
builder.Services.AddScoped<IRightService, RightService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMenuProvider, MenuProvider>();
builder.Services.AddScoped<ITabService, TabService>();
builder.Services.AddScoped<IDataQueryResolver, DataQueryResolver>();
builder.Services.AddScoped<IWidgetService, WidgetServiceEf>();
builder.Services.AddScoped<IDataProvider, SqlDataProvider>();
builder.Services.AddScoped<IAionProvisioningService, AionProvisioningService>();
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
builder.Services.AddSingleton<IClock, Aion.Infrastructure.Services.SystemClock>();
builder.Services.AddScoped<IValidationService, SimpleValidationService>();
builder.Services.AddScoped<IHistorizationService, NoOpHistorizationService>();
builder.Services.AddScoped<IDataEngine, DataEngine>();
builder.Services.AddSingleton<IModuleBootstrapper, CrmBootstrapper>();
builder.Services.AddSingleton<IModuleBootstrapper, SystemCatalogBootstrapper>();
builder.Services.AddSingleton<IModuleBootstrapper, SecurityAdminBootstrapper>();
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
        logger.LogInformation("🔧 Initialisation des modules...");

        var bootstrappers = scope.ServiceProvider.GetServices<IModuleBootstrapper>();

        foreach (var bootstrapper in bootstrappers)
        {
            bootstrapper.Register();
        }

        logger.LogInformation("✅ Modules initialisés");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Erreur lors de l'initialisation des modules");
    }

    try
    {
        logger.LogInformation("🔄 Initialisation de la base de données...");
        var orchestrator = scope.ServiceProvider.GetRequiredService<StartupOrchestrator>();
        await orchestrator.InitializeAsync();
        logger.LogInformation("✅ Base de données initialisée");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Erreur lors de l'initialisation de la base");
    }
}

// ===== Mapping =====
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.MapRazorPages(); // IMPORTANT pour /login

// ===== Démarrage =====
app.Logger.LogInformation("🚀 Aion démarré");
app.Logger.LogInformation("🔑 Connexion : https://localhost:5001/login");
app.Logger.LogInformation("   User: admin / Pass: admin / Tenant: 1");

app.Run();
