using Application.Interfaces.Services;

using Domain.Constants;

using Microsoft.AspNetCore.Http;

using System.Security.Claims;

namespace API.Utils.Helpers;

/// <summary>
/// Resolves the current caller's identity from the HTTP context for the audit trail, falling back to AuditConstants.SystemUser when there is no authenticated request.
/// </summary>
public sealed class HttpCurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
{
    public string Actor
    {
        get
        {
            ClaimsPrincipal? user = httpContextAccessor.HttpContext?.User;

            string? identifier = user?.FindFirstValue(ClaimTypes.Email)
                                 ?? user?.FindFirstValue(ClaimTypes.NameIdentifier);

            return string.IsNullOrWhiteSpace(identifier) ? AuditConstants.SystemUser : identifier;
        }
    }
}
