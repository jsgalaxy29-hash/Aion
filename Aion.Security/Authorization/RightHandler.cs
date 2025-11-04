using System.Threading.Tasks;
using Aion.Security.Services;
using Aion.Security.Extensions;
using Microsoft.AspNetCore.Authorization;

namespace Aion.Security.Authorization
{
    /// <summary>
    /// Handler chargé de vérifier les droits utilisateur via IRightService.
    /// </summary>
    public class RightHandler : AuthorizationHandler<RightRequirement>
    {
        private readonly IRightService _rightService;

        public RightHandler(IRightService rightService)
        {
            _rightService = rightService;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, RightRequirement requirement)
        {
            var user = context.User;
            var userId = user.GetUserId();
            var tenantId = user.GetTenantId();

            if (userId is null || tenantId is null)
            {
                context.Fail(new AuthorizationFailureReason(this, "Missing user claims"));
                return;
            }

            if (userId <= 0 || tenantId <= 0)
            {
                context.Fail(new AuthorizationFailureReason(this, "Invalid user context"));
                return;
            }

            var hasRight = await _rightService.HasRightAsync(
                userId.Value,
                tenantId.Value,
                requirement.Target,
                requirement.SubjectId,
                requirement.Flag);

            if (hasRight)
            {
                context.Succeed(requirement);
            }
        }
    }
}
