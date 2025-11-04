using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Aion.Security.Models;

namespace Aion.Security.Authorization
{
    /// <summary>
    /// Requirement représentant un droit Aion.
    /// </summary>
    public class RightRequirement : IAuthorizationRequirement
    {
        public string Target { get; }
        public int SubjectId { get; }
        public RightFlag Flag { get; }

        public RightRequirement(string target, int subjectId, RightFlag flag)
        {
            Target = target;
            SubjectId = subjectId;
            Flag = flag;
        }
    }

    /// <summary>
    /// Policy provider chargé de traduire une policy Right:* en requirement.
    /// </summary>
    public class RightPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        public const string Prefix = "Right:";

        public RightPolicyProvider(IOptions<AuthorizationOptions> options)
            : base(options)
        {
        }

        public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (!policyName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            {
                return await base.GetPolicyAsync(policyName);
            }

            var parts = policyName.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4)
            {
                return await base.GetPolicyAsync(policyName);
            }

            var target = parts[1];

            if (!int.TryParse(parts[2], out var subjectId))
            {
                return await base.GetPolicyAsync(policyName);
            }

            if (!Enum.TryParse(parts[3], ignoreCase: true, out RightFlag flag))
            {
                return await base.GetPolicyAsync(policyName);
            }

            var requirement = new RightRequirement(target, subjectId, flag);

            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(requirement)
                .Build();

            return policy;
        }
    }
}
