# Design: Backend build with 0 warnings

## Technical Approach

Two-track fix, no behavior change: (1) suppress the CS1591 doc-completeness noise
centrally via a new `Club12-Backend/Directory.Build.props`; (2) hand-fix the ~23
genuine defects (stale XML docs + nullable-reference sites) at their exact source
lines. Every nullable fix is individually assessed as either a real guard or a
compiler-can't-prove `!` erase. No public signature changes required — confirmed
against every site below.

## Architecture Decisions

### Decision: Central NoWarn placement

**Choice**: Create `Club12-Backend/Directory.Build.props` with
`<PropertyGroup><NoWarn>$(NoWarn);CS1591</NoWarn></PropertyGroup>`.
**Alternatives**: per-csproj `<NoWarn>` in each of the 4 projects; `#pragma`.
**Rationale**: MSBuild discovers `Directory.Build.props` by walking up from each
`.csproj` directory — all 5 projects (API, Application, Domain, Infrastructure,
API.Tests) sit directly under `Club12-Backend/`, so one file covers all. The
`.sln` lives under `Club12-Backend/Solution/` and is irrelevant to props
discovery. Using `$(NoWarn);CS1591` (not a bare `CS1591`) merges with any
inherited value. Confirmed safe: the only existing `<NoWarn>NU1903</NoWarn>`
occurrences are `PackageReference`-level metadata (a different scope), so nothing
is clobbered. CS1572/1573/1574 stay active — only CS1591 is suppressed.

### Decision: Nullable fixes — `!` erase vs. runtime guard

**Choice**: Null-forgiving `!` for EF/AutoMapper expression trees; real `??` guard
only where the SDK signature makes null genuinely reachable.
**Alternatives**: blanket suppression; `?.` in expressions.
**Rationale**: In expression lambdas (AutoMapper `ForMember`/`ForPath`, EF
`Where`), `!` is erased from the emitted expression tree → provably zero runtime
change, unlike `?.` which alters tree shape and AutoMapper/EF translation. A real
`??` guard is used only for `Supabase.Storage.List()` whose signature returns a
nullable list.

## Data Flow

No data-flow change. All edits are compile-time annotations, doc comments, or one
build-property file.

## File Changes

| File | Action | Fix |
|------|--------|-----|
| `Club12-Backend/Directory.Build.props` | Create | `<NoWarn>$(NoWarn);CS1591</NoWarn>` — suppress CS1591 solution-wide |
| `API/AutoMapperProfiles/AutoMapperProfiles.cs` | Modify | L141/142: `dest.HomeTeam!.Score`, `dest.VisitorTeam!.Score`; L149/150: `src.HomeTeam!.Name`, `src.VisitorTeam!.Name` (CS8602, `!` erased in expr tree) |
| `Infrastructure/Repositories/ScorerRepository.cs` | Modify | L41: `s.Match!.Stage!.Division!.TournamentId` (CS8602, EF expr → SQL, `!` erased) |
| `Application/Utils/Helper/SupabaseHelper/SupabaseHelper.cs` | Modify | L128: `... .List(prefix) ?? new List<Supabase.Storage.FileObject>()` (CS8600 — real guard, empty-list on null); L131: `file.Name!` (CS8604 — always-present, `!` erased). L129 resolves transitively |
| `Infrastructure/Identity/IdentityAppDbContextFactory.cs` | Modify | L34: change `string[] extras` → `string?[] extras` (CS8619 — `Path.GetFileName` yields `string?`; `file!` at use already asserts) |
| `Infrastructure/Persistance/ApplicationDBContextFactory.cs` | Modify | L31: same `string[]` → `string?[]` fix |
| `API/Controllers/MatchController.cs` | Modify | Add `<param name="stageTeamMatchService">` doc (CS1573) |
| `API/Controllers/TeamController.cs` | Modify | Add `<param name="supabaseHelper">` doc (CS1573) |
| `API/Controllers/VenueController.cs` | Modify | Add `<param name="supabaseHelper">` doc (CS1573) |
| `Application/Interfaces/Services/IBlogPostService.cs` | Modify | L18 remove stale `<param name="userId">`; L32 rename `blogPostEntity`→`id` (fixes CS1572 L32 + CS1573 L33); L39 remove stale `userId` |
| `Application/Interfaces/Services/ITournamentService.cs` | Modify | L38 rename `tournamentEntity`→`id` (fixes CS1572 L38 + CS1573 L40) |
| `Application/Interfaces/Services/IDivisionService.cs` | Modify | L19 + L38 remove stale `<param name="userId">` (CS1572) |
| `Application/Interfaces/Services/IVenueService.cs` | Modify | L39 remove stale `<param name="filter">` on parameterless `GetAllVenuesAsync()` (CS1572) |
| `Infrastructure/Backup/SupabaseBackupStorage.cs` | Modify | L85 `<see cref="rawStorage"/>` → `<c>rawStorage</c>` (CS1574 — `rawStorage` is a ctor param, not cref-resolvable) |

## Interfaces / Contracts

None changed. XML-doc edits, `!`/`??` annotations, and one `string[]`→`string?[]`
local type are all non-breaking. No public method signature is touched.

## Testing Strategy

| Layer | What | Approach |
|-------|------|----------|
| Build | 0/0/0 | `dotnet build Solution/Club12.sln --no-incremental` → 0 errors, 0 warnings, 0 suggestions |
| Behavior | No regression | Existing API.Tests suite must stay green (no logic touched) |

## Threat Matrix

N/A — no routing, shell, subprocess, VCS/PR automation, executable-file
classification, or process-integration boundary. Build-hygiene edits only.

## Migration / Rollout

No migration. Single PR; rollback = revert (delete props restores CS1591, edits
self-contained).

## Open Questions

- [ ] None blocking. Assumption to verify at apply: `HomeTeam`/`VisitorTeam`/EF
  `Match.Stage.Division` are always-present required navigations (basis for `!`).
