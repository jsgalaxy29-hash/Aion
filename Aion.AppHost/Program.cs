using Aion.AppHost;
using Aion.Domain.Contracts;
using Aion.Infrastructure;
using Aion.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Fast.Components.FluentUI;

var builder = WebApplication.CreateBuilder(args);

// === UI (.NET 8 Razor Components) ===
builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

// === Fluent UI ===
builder.Services.AddFluentUIComponents();

// === EF Core / DB ===
builder.Services.AddDbContext<AionDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("AionDb")));

// === Auth cookies ===
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opt =>
    {
        opt.LoginPath = "/login";
        opt.AccessDeniedPath = "/forbidden";
    });
builder.Services.AddAuthorization();

// ★ nécessaires pour DataQueryResolver/AuthService
builder.Services.AddHttpClient();           // IHttpClientFactory
builder.Services.AddHttpContextAccessor();  // IHttpContextAccessor

// === Services applicatifs ===
builder.Services.AddScoped<IRightsService, RightsServiceEf>();
builder.Services.AddScoped<IMenuProvider, MenuProviderEf>();
builder.Services.AddScoped<ITabService, TabService>();
builder.Services.AddScoped<IUserDashboardService, DashboardServiceEf>();
builder.Services.AddScoped<IDataQueryResolver, DataQueryResolver>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IWidgetService, WidgetServiceEf>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// modules
new Aion.Module.CRM.CrmBootstrapper().Register();

// point d'entrée Razor Components
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.Run();
