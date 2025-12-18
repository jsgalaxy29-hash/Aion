using Aion.DataEngine.Interfaces;
using Aion.Security.Extensions;
using Microsoft.AspNetCore.Http;

namespace Aion.AppHost.Services
{
    /// <summary>
    /// Implémentation de IUserContext qui récupère les informations
    /// utilisateur depuis HttpContext (claims).
    /// </summary>
    public class HttpContextUserContext : IUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpContextUserContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int UserId
        {
            get
            {
                var user = _httpContextAccessor.HttpContext?.User;
                return (int?)(user?.GetUserId()) ?? 1;
            }
        }

        public int TenantId
        {
            get
            {
                var user = _httpContextAccessor.HttpContext?.User;
                return (int?)(user?.GetTenantId()) ?? 1;
            }
        }

        public string? Username
        {
            get
            {
                var user = _httpContextAccessor.HttpContext?.User;
                return user?.Identity?.Name ?? "System";
            }
        }

       
    }
}