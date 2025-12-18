using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Aion.Security.Authorization
{
    public sealed class RightPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly DefaultAuthorizationPolicyProvider _fallback;
        public RightPolicyProvider(IOptions<AuthorizationOptions> options) => _fallback = new DefaultAuthorizationPolicyProvider(options);

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();
        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (policyName.StartsWith("Right:", StringComparison.OrdinalIgnoreCase))
            {
                var p = new AuthorizationPolicyBuilder().AddRequirements(new RightRequirement(policyName)).Build();
                return Task.FromResult<AuthorizationPolicy?>(p);
            }
            return _fallback.GetPolicyAsync(policyName);
        }
    }
}
