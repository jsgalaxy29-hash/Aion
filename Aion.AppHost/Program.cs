using Aion.AppHost;
using Aion.AppHost.Services;
using Aion.Domain.Contracts;
using Aion.Infrastructure;
using Aion.Infrastructure.Seeders;
using Aion.Infrastructure.Services;
using Aion.Security;
using Aion.Security.Authentication;
using Aion.Security.Authorization;
using Aion.Security.Services;
using Aion.Module.CRM;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.FluentUI.AspNetCore.Components;
using Aion.DataEngine.Interfaces;
using Aion.DataEngine.Services;
using Aion.Infrastructure.Data;


var builder = WebApplication.CreateBuilder(args);

// ===== Fluent UI - DOIT ÊTRE AJOUTÉ AVANT AddRazorComponents =====
builder.Services.AddFluentUIComponents();

// ===== Services Blazor =====
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient();
builder.Services.AddRazorPages();

// ===== Database Contexts =====
var connectionString = builder.Configuration.GetConnectionString("AionDb")
    ?? "Server=localhost;Database=AionDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;";

builder.Services.AddDbContext<AionDbContext>(opt =>
    opt.UseSqlServer(connectionString));

builder.Services.AddDbContext<SecurityDbContext>(opt =>
    opt.UseSqlServer(connectionString));

// ===== DataEngine Services =====
builder.Services.AddSingleton<IDataProvider, SqlDataProvider>();
builder.Services.AddSingleton<IClock, Aion.Infrastructure.Services.SystemClock>();
builder.Services.AddScoped<IAionProvisioningService, AionProvisioningService>();


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

builder.Services.AddAuthorizationBuilder();

// Claims Transformation
builder.Services.AddScoped<IClaimsTransformation, AionClaimsTransformation>();

// Policy Provider
builder.Services.AddSingleton<IAuthorizationPolicyProvider, RightPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, RightHandler>();

// ===== Services Aion =====
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<IRightService, RightService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMenuProvider, MenuProvider>();


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

// modules
new CrmBootstrapper().Register();

// ===== Database Initialization =====
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {

        // 1. Provisioning de la structure SQL
        logger.LogInformation("📊 Provisioning de la base de données...");
        var provisioning = scope.ServiceProvider.GetRequiredService<IAionProvisioningService>();
        await provisioning.EnsureDatabaseReadyAsync();

        logger.LogInformation("🔄 Initialisation de la base de données...");

        var securityDb = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();
        // Créer la base si elle n'existe pas
        await securityDb.Database.EnsureCreatedAsync();

        // Seed des données de sécurité
        await SecuritySeeder.SeedAsync(securityDb);

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

app.MapRazorPages();

// ===== Démarrage =====
app.Logger.LogInformation("🚀 Aion démarré");
app.Logger.LogInformation("🔑 Connexion : admin / admin (TenantId: 1)");

app.Run();
