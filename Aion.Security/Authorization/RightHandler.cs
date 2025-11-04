using System.Threading.Tasks;
using Aion.Domain.Contracts;
using Aion.Security.Services;
using Microsoft.AspNetCore.Authorization;

namespace Aion.Security.Authorization
{
    /// <summary>
    /// Handler chargé de vérifier les droits utilisateur via IRightService.
    /// </summary>
    public class RightHandler : AuthorizationHandler<RightRequirement>
    {
        private readonly IUserContext _userContext;
        private readonly IRightService _rightService;

        public RightHandler(IUserContext userContext, IRightService rightService)
        {
            _userContext = userContext;
            _rightService = rightService;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, RightRequirement requirement)
        {
            var userId = _userContext.UserId;
            var tenantId = _userContext.TenantId;

            if (userId <= 0 || tenantId <= 0)
            {
                return;
            }

            var hasRight = await _rightService.HasRightAsync(
                userId,
                tenantId,
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
