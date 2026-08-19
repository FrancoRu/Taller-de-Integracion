# Verification Report — Slice A1 (MatchServiceGenerationTests.cs)

**Change**: high-test-coverage-batch-a-match-stage
**Scope**: Slice A1 only. Sibling slice A2 (StageServiceTests.cs) and Batch B files present in the working tree but out of scope for this verify pass.
**Mode**: Full artifacts (spec + tasks + apply-progress retrieved and cross-checked against live source).

## Build/Test Evidence (independently re-run)

| Command | Result |
|---|---|
| `dotnet build Club12-Backend/Solution/Club12.sln` | 0 Warnings, 0 Errors |
| `dotnet test API.Tests --filter FullyQualifiedName~MatchServiceGenerationTests` | 15/15 passed |
| `dotnet test Club12-Backend/Solution/Club12.sln` (full) | 106/106 passed, 0 failed (differs from apply-progress's "93/93" because sibling slice A2 StageServiceTests.cs + Batch B AuthServiceJwtTests.cs/PlayerSanctionAppealTests.cs were concurrently added to the same working tree — not a regression, no failures either way) |

## Deviation Claim — Independently Verified TRUE

Read directly (not trusted from apply report):

- `MatchService.cs:98-152` (`GenerateFixtureAsync`): confirmed no `Match.StageId` assignment on any created `Match` object in either round; `divisionId` parameter never referenced in the method body.
- `MatchEntityConfiguration.cs:16,22-25`: confirmed `StageId` is a required, non-nullable FK (`.IsRequired()` on both the property and the `HasOne(...).HasForeignKey(...)` relationship) to `Stage`. A `Guid.Empty` StageId with no matching row genuinely fails `SaveChanges` with `DbUpdateException` (FK violation), for every valid team count, in both the SQLite test harness and the real Postgres schema (same required relationship, unconditional on provider).
- `MatchServiceGenerationTests.cs`: confirmed `GenerateFixtureAsync_ValidTeamCount_ThrowsForeignKeyViolation_Bug1` (Theory 2/4/8) asserts `Assert.ThrowsAsync<DbUpdateException>` + `Assert.Empty(matches)` on read-back — non-vacuous, faithful to source behavior. BUG-2 documented via code comment (source-verified: `currentMatchDate` captured once per leg, reused unmodified for every match in that leg — MatchService.cs:107,133).
- Repo-wide grep (backend + frontend `Club12-WebClient`) for `GenerateFixtureAsync`: zero call sites outside the interface declaration, the implementation, the new test file, and SDD docs. Confirmed genuinely dead/unwired code — no controller, service, or frontend caller exists.
- `ResolveGroupTeamCountAsync` (MatchService.cs:189-214): confirmed `totalGroups` count query is self-inclusive of the very Group-type stage being queried, so `totalGroups <= 0` is unreachable whenever the group code path executes at all (task 3.1 unreachability claim confirmed).

## Git State

`git diff --stat` → empty (no tracked file modified). `git status --porcelain` → only new untracked files (this test file + sibling slices, `.codegraph/`, `openspec/`). Zero production files touched — confirmed.

## Task Completion (A1, 12/12)

All tasks 1.1, 1.2, 2.1-2.5, 3.1-3.5, 4.1-4.4, 5.1-5.2 marked `[x]` in tasks.md; inline deviation notes on 2.1/2.2/2.4/2.5/3.1 match the actual test file content exactly (verified by direct comparison, not assumed).

## Spec Compliance Matrix

| Spec Scenario | Status | Note |
|---|---|---|
| 4 teams → 12 matches, no self-play/dup pairing | UNOBSERVABLE (documented) | Superseded by FK-violation characterization; genuinely unreachable via black-box surface given BUG-1 + required FK |
| 8 teams → 56 matches, home/away swap | UNOBSERVABLE (documented) | Same reason |
| Odd/sub-minimum team count → ArgumentException | PASS | Covered, passing |
| BUG-1 (no StageId, divisionId ignored) | CHARACTERIZED | Via throw-behavior test, faithful to source |
| BUG-2 (identical MatchDate per leg) | CHARACTERIZED (comment only) | Source-verified accurate, unobservable via black-box for same FK reason |
| Group-stage zero-stages/no-teams/indivisible/<2-per-group/valid-distribution | PASS | All 5 scenarios covered, passing |
| DistributeMatchDates midpoint/spread/end<start | PASS | All 3 covered, passing |
| DistributeMatchDates matchCount<=0 | DOCUMENTED ONLY | Correctly unreachable, per spec's own "documented only" scenario |

## Issues

None CRITICAL. No WARNING. One SUGGESTION (non-blocking, does not affect this change's verdict): BUG-1 (`GenerateFixtureAsync` never sets `StageId`, ignores `divisionId`) is confirmed dead/unwired code (zero callers repo-wide) — it is NOT a live production defect affecting the running site today, since nothing invokes this method. Still worth a tracked follow-up decision: either wire it up correctly (set StageId, honor divisionId) if a future feature needs it, or delete the dead method — but this is explicitly out of scope for this pure-test-addition change and correctly not fixed here.

## Verdict: PASS

The apply pass's significant-deviation claim is verified accurate in every particular: the FK-violation reality, the zero-callers/dead-code status, the faithfulness of the rewritten tests, and the correct documented-only treatment of the two now-unobservable requirements (round-robin scenarios) and BUG-2. This is dead code, not a live production bug — no follow-up urgency, though a cleanup/decision ticket is reasonable.
