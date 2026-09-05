using System;

namespace Application.DTOs.User.Response;

/// <summary>
/// Confirms a forced password reset was performed; the generated temporary password is never included here.
/// </summary>
public sealed record ResetPasswordResponse(
    Guid UserId
);