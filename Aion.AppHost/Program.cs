using Aion.AppHost;
using Aion.AppHost.Services;
using Aion.Infrastructure;
using Aion.Infrastructure.Services;
using Aion.Security;
using Aion.Security.Authentication;
using Aion.Security.Authorization;
using Aion.Security.Services;
using Aion.Domain.Contracts;
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
builder.Services.AddDbContext<AionDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("AionDb")));

builder.Services.AddDbContext<SecurityDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("AionDb")));

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
    // Policy par défaut : authentifié
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Claims Transformation (charge les droits)
builder.Services.AddScoped<IClaimsTransformation, AionClaimsTransformation>();

// Policy Provider dynamique pour les droits
builder.Services.AddSingleton<IAuthorizationPolicyProvider, RightPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, RightHandler>();

// ===== Services Aion =====
builder.Services.AddMemoryCache(); // Pour cache des droits
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IRightService, RightService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMenuProvider, MenuProvider>();
builder.Services.AddScoped<IAionThemeService, AionThemeService>();

// ===== Build & Configuration =====
var app = builder.Build();

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
    var aionDb = scope.ServiceProvider.GetRequiredService<AionDbContext>();
    var securityDb = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();

    aionDb.Database.EnsureCreated();
    securityDb.Database.EnsureCreated();

    // Seed données de base (à décommenter après création du seeder)
    // await SeedSecurityData(securityDb);
}

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.MapRazorPages();

app.Run();

// ===== Méthode de seed (à externaliser dans un service dédié) =====
/*
async Task SeedSecurityData(SecurityDbContext db)
{
    if (await db.SUser.AnyAsync()) return; // Déjà seedé

    // Création tenant par défaut
    var tenant = new STenant { Id = 1, Name = "Default" };
    
    // Création groupe admin
    var adminGroup = new SGroup 
    { 
        Name = "Administrateurs", 
        Description = "Groupe administrateur système",
        IsSystem = true,
        TenantId = 1
    };
    db.SGroup.Add(adminGroup);
    await db.SaveChangesAsync();

    // Création utilisateur admin
    var admin = new SUser
    {
        UserName = "admin",
        NormalizedUserName = "ADMIN",
        Email = "admin@aion.local",
        NormalizedEmail = "ADMIN@AION.LOCAL",
        PasswordHash = "admin", // À CHANGER avec BCrypt !
        FullName = "Administrateur",
        IsActive = true,
        TenantId = 1
    };
    db.SUser.Add(admin);
    await db.SaveChangesAsync();

    // Association admin au groupe
    db.SUserGroup.Add(new SUserGroup
    {
        UserId = admin.Id,
        GroupId = adminGroup.Id,
        IsLinkActive = true,
        TenantId = 1
    });

    // Création types de droits
    var rightTypes = new[]
    {
        new SRightType { Code = "Menu", Name = "Droits sur menus", DataSource = "SMenu", Right1Name = "Voir", TenantId = 1 },
        new SRightType { Code = "Module", Name = "Droits sur modules", DataSource = "S_Module", Right1Name = "Lire", Right2Name = "Écrire", Right3Name = "Supprimer", TenantId = 1 },
        new SRightType { Code = "Table", Name = "Droits sur tables", DataSource = "STable", Right1Name = "Lire", Right2Name = "Écrire", Right3Name = "Supprimer", TenantId = 1 },
        new SRightType { Code = "Action", Name = "Droits sur actions", DataSource = "S_Action", Right1Name = "Exécuter", TenantId = 1 }
    };
    db.SRightType.AddRange(rightTypes);
    await db.SaveChangesAsync();
}
*/