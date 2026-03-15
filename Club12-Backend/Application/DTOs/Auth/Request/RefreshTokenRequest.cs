using System;
namespace Application.DTOs.Auth.Request;

/// <summary>
/// Represents a request object for refreshing a JWT token.
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// The refresh token to use for generating a new JWT token.
    /// </summary>
    public required string RefreshToken { get; set; }
}
