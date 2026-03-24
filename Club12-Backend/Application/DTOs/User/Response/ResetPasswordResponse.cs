using System;

namespace Application.DTOs.User.Response;

/// <summary>
/// Contains the temporary password generated after a forced reset.
/// Must be communicated to the user out-of-band.
/// </summary>
public sealed record ResetPasswordResponse(
    Guid   UserId
);