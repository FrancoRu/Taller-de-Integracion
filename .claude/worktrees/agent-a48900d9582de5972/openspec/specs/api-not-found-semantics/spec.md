# API Not-Found Semantics Specification

## Purpose

Backend controllers currently return `BadRequest` (400) with a bare-string body when a client requests an action against a resource identified by an ID that does not exist. This is wrong HTTP semantics and diverges from the `ProblemDetails` shape `GlobalExceptionHandler` already emits elsewhere. This spec defines the corrected behavior: identity-lookup failures return 404 with a `ProblemDetails`-consistent body, across `BlogPostController`, `DivisionController`, `MatchController`, `PlayerController`, `PlayerSanctionController`, `PlayerStatisticController`, `TeamController`, `TournamentController`, and `VenueController`.

## Requirements

### Requirement: Not-Found Status for Entity-Identity Lookups

The system MUST return HTTP 404 (Not Found) instead of 400 (Bad Request) when a client requests an action targeting a specific resource by its own identifier (GET/PUT/DELETE by id, or a nested sub-resource action referencing a parent id) and no entity with that identifier exists, for all nine controllers listed in Purpose.

#### Scenario: GET by nonexistent ID returns 404

- GIVEN no entity exists with id `999999` in a fixed controller's data store (e.g. Match, Team, BlogPost, Division, Player, PlayerSanction, PlayerStatistic, Tournament, Venue)
- WHEN a client sends a GET request for that id (e.g. `GET /api/matches/999999`)
- THEN the response status is 404
- AND the response body is `ProblemDetails`-shaped, not a bare string

#### Scenario: PUT/DELETE or nested action against nonexistent parent returns 404

- GIVEN no entity exists with id `999999`
- WHEN a client sends `PUT`/`DELETE` for that id, or a nested action referencing it as a parent (e.g. adding a sanction to a nonexistent player)
- THEN the response status is 404 with a `ProblemDetails`-shaped body

### Requirement: ProblemDetails-Consistent Body Shape

All 404 responses covered by this spec MUST return a body shaped consistently with `GlobalExceptionHandler`'s `ProblemDetails` output (`status`, `title`, `detail`, `traceId`), not a bare string.

#### Scenario: 404 body matches ProblemDetails shape

- GIVEN a request to a fixed endpoint for a nonexistent entity
- WHEN the 404 response is returned
- THEN the JSON body includes `status: 404` and a non-empty `title`/`detail`
- AND the body is not solely a plain string as it was before the fix

### Requirement: ProducesResponseType Reflects 404

Each fixed controller action MUST declare `[ProducesResponseType(StatusCodes.Status404NotFound)]` (or equivalent) instead of the previous 400 declaration.

#### Scenario: OpenAPI metadata reflects 404

- GIVEN a fixed controller action
- WHEN OpenAPI/Swagger metadata is inspected for that action
- THEN 404 is listed as a possible response instead of 400

### Requirement: Create-Time FK Validation Stays 400 (non-goal boundary)

The system MUST NOT change status codes for validation errors raised while creating a resource when the request body references a related entity that does not exist (e.g. POST creating a Player with a `TeamId` that has no matching Team). These MUST remain 400 (or 422 if already so) — this is input validation, not a "resource itself doesn't exist" case.

#### Scenario: POST with invalid FK reference stays 400

- GIVEN a POST request body references a `TeamId` that does not exist
- WHEN the create action processes the request
- THEN the response status remains 400 (unchanged), and is NOT converted to 404

## Acceptance Evidence

Each of the nine controllers MUST have at least one passing integration test (via the API.Tests xUnit harness, real HTTP round-trip against the app) asserting 404 + `ProblemDetails` shape for a not-found case, and one asserting FK-validation 400 stays unchanged. A build-only check is insufficient evidence.
