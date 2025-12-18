using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

namespace Aion.Security.Authorization
{
    public sealed class RightRequirement : IAuthorizationRequirement
    {
        public string Policy { get; }
        public RightRequirement(string policy) => Policy = policy;
    }

    public sealed class RightHandler : AuthorizationHandler<RightRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RightRequirement requirement)
        {
            var ok = context.User.Claims.Any(c => c.Type == "Right" && c.Value == requirement.Policy.Replace("Right:", ""));
            if (ok) context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}
