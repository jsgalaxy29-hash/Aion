using Aion.AppHost;
using Aion.AppHost.Services;
using Aion.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.FluentUI.AspNetCore.Components;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddHttpClient();             // requis côté Server
builder.Services.AddFluentUIComponents();     // enregistre tous les services Fluent UI

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/login";
        o.LogoutPath = "/logout";
        o.AccessDeniedPath = "/login";
        o.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

builder.Services.AddDbContext<AionDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("AionDb")));

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IAionThemeService, AionThemeService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddRazorPages();

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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AionDbContext>();
    db.Database.EnsureCreated();
}

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.MapRazorPages();

app.Run();
