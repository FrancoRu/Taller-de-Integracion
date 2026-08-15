# Auth Controller Boundary Specification

## Purpose

`AuthController` MUST depend only on `Application` interfaces, never on `Infrastructure`/`Identity` types. `Logout` is currently the sole exception (injects `UserManager<ApplicationUser>` and mutates `RefreshToken`/`RefreshTokenExpiryTime` directly). This spec locks the observable Logout contract so the boundary fix (routing through a new `IAuthenticationService.LogoutAsync`) is provably behavior-preserving.

## Requirements

### Requirement: Controller Layer Isolation

`AuthController` MUST NOT reference `Infrastructure.Identity` types (`UserManager<ApplicationUser>`, `ApplicationUser`) in its constructor, fields, or method bodies. All authentication operations, including logout, MUST be invoked exclusively through `IAuthenticationService`.

#### Scenario: Controller has no Infrastructure dependency

- GIVEN the compiled `AuthController` class
- WHEN its constructor parameters and using directives are inspected
- THEN no parameter type or import resolves to `Infrastructure.Identity`
- AND `UserManager<ApplicationUser>` does not appear anywhere in the file

### Requirement: Logout Clears Refresh Token State

`POST /api/auth/logout` MUST clear the caller's `RefreshToken` and `RefreshTokenExpiryTime` when a user matching the caller's id claim exists, via `IAuthenticationService.LogoutAsync`, and MUST return `204 No Content` regardless of whether a matching user was found.

#### Scenario: Logout clears refresh token for existing user

- GIVEN an authenticated caller with a valid id claim
- AND a persisted user matching that id has a non-null `RefreshToken` and `RefreshTokenExpiryTime`
- WHEN `POST /api/auth/logout` is called
- THEN the response status is `204 No Content`
- AND the user's `RefreshToken` and `RefreshTokenExpiryTime` are persisted as `null`

#### Scenario: Logout is a no-op for a missing user

- GIVEN an authenticated caller whose id claim does not match any persisted user
- WHEN `POST /api/auth/logout` is called
- THEN the response status is `204 No Content`
- AND no persistence update is attempted

#### Scenario: Logout behavior is identical to the pre-refactor implementation

- GIVEN the same request and user-state fixtures used against the pre-refactor `AuthController.Logout` (direct `UserManager` access)
- WHEN the same requests are run against the post-refactor controller (routed through `IAuthenticationService.LogoutAsync`)
- THEN the response status codes match for both the existing-user and missing-user cases
- AND the resulting `RefreshToken`/`RefreshTokenExpiryTime` persisted state matches for both cases

## Non-Goals

- No change to the `204 No Content` response contract or route (`POST /api/auth/logout`).
- No change to any other `AuthController` action (`Register`, `Login`, `RequestMagicLink`, `MagicLinkLogin`, `Guest`, `Refresh`, `ConfirmPasswordReset`).
- No change to `IdentityAuthenticationService`'s existing methods beyond adding `LogoutAsync`.
