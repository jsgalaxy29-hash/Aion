using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Aion.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Aion.AppHost.Services
{
    public interface IAuthService
    {
        Task<bool> SignInAsync(string login, string password);
        Task SignOutAsync();
    }

    public class AuthService : IAuthService
    {
        private readonly IHttpContextAccessor _http;
        private readonly AionDbContext _db;

        public AuthService(IHttpContextAccessor http, AionDbContext db)
        {
            _http = http;
            _db = db;
        }

        public async Task<bool> SignInAsync(string login, string password)
        {
            var hash = Sha256(password);
            var user = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Login == login && x.PasswordHash == hash && x.IsActive);

            if (user is null) return false;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Login),
                new Claim("sub", user.Id.ToString()),
                new Claim("tenant", user.TenantId.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await _http.HttpContext!.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            return true;
        }

        public Task SignOutAsync() =>
            _http.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        private static string Sha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder();
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
