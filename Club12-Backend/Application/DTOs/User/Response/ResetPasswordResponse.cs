using System;

namespace Application.DTOs.User.Response;

/// <summary>
/// Confirms a forced password reset was performed. The generated temporary
/// password is intentionally not included here — it is set server-side and
/// must be communicated to the user out-of-band.
/// </summary>
public sealed record ResetPasswordResponse(
    Guid UserId
);