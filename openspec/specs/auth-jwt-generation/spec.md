# Auth JWT Generation Specification

## Purpose

Characterizes the existing `AuthService` JWT/refresh-token generation behavior (`GenerateJwtTokenAsync`, `HmacSha256`, `UtcNow.AddHours(24)` expiry). This spec locks in current behavior via tests; it introduces no change to signing, security configuration, or expiry.

## Requirements

### Requirement: Access Token Claims and Expiry

The generated JWT access token MUST include claims identifying the user id and the user's roles, and MUST expire approximately 24 hours after issuance (`UtcNow.AddHours(24)`), matching the `TokenResponse.ExpiresIn` value returned alongside it.

#### Scenario: Token carries expected claims

- GIVEN a user with a known id and role set
- WHEN `GenerateJwtTokenAsync` is called for that user
- THEN the returned access token's claims include the user's id
- AND the claims include the user's role(s)

#### Scenario: Token expiry is genuinely 24 hours from issuance

- GIVEN a user for whom a token is generated
- WHEN the returned access token is parsed
- THEN its expiry timestamp is ~24 hours after the issuance time (within a small tolerance)
- AND the returned `TokenResponse.ExpiresIn` equals `TimeSpan.FromHours(24)`

### Requirement: Access Token Signature Verifiability

The generated access token MUST be independently parseable and its signature MUST validate against the application's configured `JWT:Key`/`Issuer`/`Audience` using `HmacSha256`.

#### Scenario: Token round-trips through signature validation

- GIVEN the application's configured JWT signing key, issuer, and audience
- WHEN the generated access token is validated with `JwtSecurityTokenHandler` and matching `TokenValidationParameters`
- THEN validation succeeds
- AND the validated token's issuer and audience match the configured values

### Requirement: Refresh Token Uniqueness

Two separate calls to `GenerateJwtTokenAsync` (for the same or different users) MUST produce different `RefreshToken` values on the returned `TokenResponse`.

#### Scenario: Two calls yield different refresh tokens

- GIVEN two separate calls to `GenerateJwtTokenAsync`
- WHEN their `TokenResponse.RefreshToken` values are compared
- THEN the two refresh token values are not equal

## Non-Goals

- No change to JWT signing, key material, issuer/audience configuration, or expiry duration.
- No claim of cryptographic randomness quality — only inequality between two generated refresh tokens is asserted.
