using System.Security.Claims;
using FinTrack.Application.Authentication;
using Microsoft.AspNetCore.Http;

namespace FinTrack.Infrastructure.Authentication;

internal sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? UserId => Guid.TryParse(accessor.HttpContext?.User.FindFirstValue("sub"), out var id) ? id : null;
}
