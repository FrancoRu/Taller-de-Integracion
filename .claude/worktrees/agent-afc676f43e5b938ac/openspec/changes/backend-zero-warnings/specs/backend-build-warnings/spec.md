# Backend Build Warnings Specification

## Purpose

Define the acceptance bar for a zero-warning `Club12-Backend/Solution/Club12.sln`
build: CS1591 suppressed centrally, all other warnings genuinely fixed at their
real source, no behavior or public-contract regressions.

## Requirements

### Requirement: Zero-Warning Solution Build

The system MUST build `Club12-Backend/Solution/Club12.sln` with 0 errors, 0
warnings, 0 suggestions.

#### Scenario: Full rebuild is clean

- GIVEN all changes in this spec are applied
- WHEN `dotnet build Club12-Backend/Solution/Club12.sln --no-incremental` runs
- THEN the exit code is 0 AND the output reports 0 Warning(s) AND 0 Error(s)

### Requirement: CS1591 Suppressed via NoWarn, Not Documentation

The system MUST suppress CS1591 solution-wide via a new
`Club12-Backend/Directory.Build.props` (`<NoWarn>$(NoWarn);CS1591</NoWarn>`).
The system MUST NOT resolve any CS1591 instance by hand-writing XML doc
comments.

#### Scenario: CS1591 absent without doc additions

- GIVEN `Directory.Build.props` exists with the CS1591 NoWarn entry
- WHEN the solution builds
- THEN no CS1591 warnings appear AND no member gained a newly-authored
  `<summary>`/`<param>` block solely to satisfy CS1591

### Requirement: XML Doc Comments Match Real Signatures (CS1572/1573/1574)

Each doc-comment defect MUST be corrected to match the real method/type
signature it documents — not suppressed.

| Site | Fix |
|------|-----|
| `MatchController.cs:26` | Add `<param name="stageTeamMatchService">` |
| `TeamController.cs:28` | Add `<param name="supabaseHelper">`; move `<param>` tags out of `<summary>` (structural) |
| `VenueController.cs:26` | Add `<param name="supabaseHelper">` |
| `IBlogPostService.cs:18` | Remove stray `<param name="userId">` (no such param on `CreateBlogPostAsync`) |
| `IBlogPostService.cs:32/33` | Rename `<param name="blogPostEntity">` → `<param name="id">` on `DeleteBlogPostAsync(Guid id)` |
| `IBlogPostService.cs:39` | Remove stray `<param name="userId">` (no such param on `UpdateBlogPostAsync`) |
| `ITournamentService.cs:38/40` | Rename `<param name="tournamentEntity">` → `<param name="id">` on `DeleteTournamentAsync(Guid id)` |
| `IDivisionService.cs:19,38` | Remove stray `<param name="userId">` (no such param on either method) |
| `IVenueService.cs:39` | Remove `<param name="filter">` (`GetAllVenuesAsync()` takes no params) |
| `SupabaseBackupStorage.cs:85` | `<see cref="rawStorage"/>` → `<c>rawStorage</c>` (ctor param, not cref-resolvable) |

#### Scenario: Doc tags match real parameters

- GIVEN each file/site in the table above
- WHEN the doc comment is compared to the real member signature
- THEN every documented `<param>` name matches an actual parameter AND every
  actual parameter has exactly one `<param>` tag AND every `cref` resolves

### Requirement: Null-Safety Warnings Genuinely Fixed (CS8600/8602/8604/8619)

Each nullable warning MUST be resolved with an individually-justified fix
(real guard, narrowed type, or null-forgiving `!` only where the value is
structurally never dereferenced or is guaranteed non-null by a `required`
relationship). Blanket suppression or unjustified `!` is prohibited.

| Site | Expected shape | Justification |
|------|-----------------|----------------|
| `AutoMapperProfiles.cs:141,142` | Null-forgiving on `dest.HomeTeam`/`dest.VisitorTeam` in `ForPath` selectors | Structural member-path expression tree, never executed at runtime |
| `AutoMapperProfiles.cs:149,150` | Real null guard (`src.HomeTeam != null ? ... : fallback`), mirroring the existing `WinningTeam` guard at line 143 | `MapFrom` source lambda is a real runtime delegate; `Match.HomeTeam`/`VisitorTeam` are genuinely nullable `Team?` |
| `ScorerRepository.cs:41` | Null-forgiving on `s.Match` only | `Scorer.MatchId` is a required FK; EF `IQueryable` lambda translates to SQL, never executes as CLR code. `Stage`/`Division` navs are already non-nullable — no extra `!` needed there |
| `SupabaseHelper.cs:128` | Real guard (`?? []`) on the SDK's possibly-null list result | External library return, not provably non-null |
| `SupabaseHelper.cs:129` | Resolves once 128 is guarded | No separate edit |
| `SupabaseHelper.cs:131` | Real guard/fallback on `file.Name`, not `!` | External deserialized API field, not structurally guaranteed non-null |
| `IdentityAppDbContextFactory.cs:34` | Narrow declared type `string[]` → `string?[]` | `Path.GetFileName` yields `string?`; matches existing nullable handling in the loop below |
| `ApplicationDBContextFactory.cs:31` | Same type-narrowing fix | Same reasoning |

#### Scenario: Each nullable fix is locally justified

- GIVEN each site in the table above
- WHEN the fix is reviewed in isolation
- THEN it is either a real null check/guard, a corrected type annotation, or a
  null-forgiving `!` applied only to a value proven safe by a `required`
  relationship or a non-executed expression-tree path
- AND no site uses `#pragma warning disable` or an unjustified `!`

### Requirement: No Unintended Behavior or Public API Changes

Fixes MUST NOT alter any method's observable behavior or public signature
beyond the null-safety correction itself. Any case where a fix would require
a signature/contract change MUST be flagged, not applied silently.

#### Scenario: Mapping/query semantics unchanged

- GIVEN `AutoMapperProfiles.cs` and `ScorerRepository.cs` after the fix
- WHEN existing tests covering match/scorer mapping and filtering run
- THEN results are identical to pre-change behavior (aside from the
  HomeTeam/VisitorTeam-null edge case now degrading gracefully instead of
  throwing)

#### Scenario: No silent contract change

- GIVEN any nullable fix in this change
- WHEN it would require changing a public method signature or return type
- THEN the change is called out explicitly (PR description/spec risk), not
  applied without notice
