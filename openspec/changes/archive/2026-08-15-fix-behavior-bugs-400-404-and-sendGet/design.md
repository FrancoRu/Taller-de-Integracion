# Design: Fix HTTP error-contract bugs (400→404 not-found + sendGet 401 pipeline)

## Technical Approach

Two independent, low-risk fixes. Backend: replace ~27 `BadRequest($"... not found.")`
not-found sites (9 controllers) with a single shared `NotFoundProblem` extension that
emits a 404 `ProblemDetails` identical in shape to `GlobalExceptionHandler`. Frontend:
collapse `sendGet`'s bespoke `try/catch` into a delegation to the shared `sendRequest<T>`
so GET errors flow through `throwError` like every other verb.

## Architecture Decisions

### Decision: Shared `NotFoundProblem` extension vs. inline `Problem(...)` at each site

**Choice**: Add `ControllerBaseExtensions.NotFoundProblem(this ControllerBase, string entity, object id)`
in `API/Utils/`. Each of the ~27 sites becomes `return this.NotFoundProblem(nameof(Venue), id);`.
**Alternatives considered**: (a) 27× inline `Problem(statusCode: 404, ...)`; (b) `NotFound(new ProblemDetails{...})`.
**Rationale**: Repetition across 9 controllers makes a one-line helper strictly DRYer and
guarantees a single canonical body/title — aligned with the project's "no magic strings"
goal — without introducing a real abstraction layer. `ControllerBase.Problem()` routes through
the framework `ProblemDetailsFactory`, which auto-injects the `traceId` extension, so the body
(`title`/`detail`/`status`/`traceId`) matches `GlobalExceptionHandler` exactly. Rejecting (b)
because hand-built `ProblemDetails` would omit `traceId` and re-diverge into a third shape.

### Decision: `NotFoundProblem` return type

**Choice**: return `ObjectResult` (what `Problem()` returns). Implicitly usable from actions
declaring `ActionResult`, `ActionResult<T>`, and `IActionResult`.
**Rationale**: No call-site signature changes; drop-in for existing `return BadRequest(...)`.

### Decision: `sendGet` fix shape

**Choice**: delegate to the existing pipeline: `sendRequest<T>('GET', resource, {}, null, query)`.
**Alternatives considered**: keep the bespoke `try/catch` but add a `throwError(error)` call.
**Rationale**: `sendRequest` already applies identical headers (`getHeaders({})` ≡ `getHeaders()`),
identical URL building, and the shared catch. Delegation removes duplicated request logic and the
stray `console.error`, and cannot drift from other verbs. Public signature and success-path return
(`Promise<AxiosResponse<T>>`) are unchanged.

## Data Flow

    GET caller ─→ sendGet ─→ sendRequest ─→ axios.request ──ok──→ AxiosResponse<T>
                                                │
                                              catch → throwError → triggerStatusCodeHandlers
                                                                 → handleUnauthorizedToken (401 → /token-invalido)

    Backend miss: controller ─→ this.NotFoundProblem ─→ Problem() ─→ ProblemDetailsFactory
                              (404, application/problem+json, title/detail/status/traceId)

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `Club12-Backend/API/Utils/ControllerBaseExtensions.cs` | Create | `NotFoundProblem` extension emitting 404 ProblemDetails |
| `Club12-Backend/API/Controllers/{BlogPost,Division,Match,Player,Team,Tournament,Venue,PlayerStatistic,PlayerSanction}Controller.cs` | Modify | ~27 not-found `BadRequest` → `NotFoundProblem`; swap `ProducesResponseType(400)`→`404` on those actions |
| `Club12-WebClient/src/modules/core/utils/axiosUtils.ts` | Modify | `sendGet` delegates to `sendRequest`; drop bespoke catch |
| `Club12-Backend/API.Tests/NotFoundContractTests.cs` | Create | Parameterized 404-contract characterization test |
| `Club12-WebClient/src/modules/core/utils/axiosUtils.test.ts` | Modify | Add `sendGet` 401-redirect + 404-reject cases |

**Out of scope (stay 400):** image/logo validation (BlogPost, Team), business-state rules
(Match already-started, PlayerSanction appeal states), Player-POST create-time missing-Team FK.
Grep on `"not found"` will not match those, but apply must confirm each site is a genuine lookup miss.

## Interfaces / Contracts

```csharp
public static ObjectResult NotFoundProblem(this ControllerBase controller, string entity, object id) =>
    controller.Problem(
        detail: $"{entity} with id {id} not found.",
        statusCode: StatusCodes.Status404NotFound,
        title: "Not Found: The specified resource could not be found.");
```

```ts
export const sendGet = async <T>(resource: string, query?: object): Promise<AxiosResponse<T>> =>
  await sendRequest<T>('GET', resource, {}, null, query);
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Integration (BE) | Representative GET-by-id endpoints return 404 for a random Guid | xUnit `[Theory]` over `CustomWebApplicationFactory` anonymous GETs (`api/venues/{guid}`, `api/players/{guid}`, `api/matches/{guid}`, `api/player-sanctions/{guid}`) — assert `404`, `content-type: application/problem+json`, body has `traceId`. One theory ≈ batch1 characterization, not 27 cases |
| Unit (FE) | `sendGet` fires 401 → `/token-invalido` redirect (auth header); rejects on 404 | Extend `axiosUtils.test.ts`: mock `axios.request` rejecting a 401 (mirrors `sendDelete` test) asserting `assign('/token-invalido')`; add a 404 reject case |
| Regression | Success paths unchanged | Existing `SmokeTests.GetDivisions_ReturnsOk` + existing `sendDelete`/queryKeys tests stay green |

## Threat Matrix

N/A — no routing, shell, subprocess, VCS/PR automation, executable-file classification, or
process-integration boundary. Change is HTTP status-code semantics and a client error path only.

## Migration / Rollout

No migration required. BE and FE commits are independent (no atomic-landing dependency); either
is revertible alone. If the diff exceeds the 400-line review budget, split into BE-fix and FE-fix PRs.

## Success-path impact

Zero. BE change only alters the `entity is null` branch (already an error return); 200/201/204
paths untouched. FE change preserves `sendGet`'s signature and returns the same `AxiosResponse<T>`
on success — only the error branch gains the shared pipeline.

## Open Questions

- [ ] PlayerSanction grep shows 4 not-found sites (proposal estimated 3) — apply must classify each.
- [ ] `NotFoundProblem` entity arg: `nameof(Type)` (e.g. "Player") vs. display literal ("Player sanction"). Assumed `nameof`; minor wording drift is acceptable.
