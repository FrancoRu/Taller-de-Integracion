# Archive Report: fix-player-admin-slug-404

**Change**: fix-player-admin-slug-404  
**Archived**: 2026-08-29  
**Status**: COMPLETE  
**Artifact store**: hybrid (OpenSpec filesystem + Engram)

## Executive Summary

The `fix-player-admin-slug-404` change has been fully implemented, verified, and archived. The admin player-detail route now accepts both GUID and slug formats (fixing the routing-level 404 error), player slug generation has been unified across create/seed/backfill operations (eliminating DNI leakage), and all manual verification steps have been completed. The change is production-ready and closed.

## Final State (at Archive Time)

### Implementation Status

**COMPLETE.** All 31 tasks across 8 phases completed (all checkboxes marked).

- **Part 1** (404 fix): Backend controller route widened to accept id-or-slug; frontend characterization tests added (no source changes).
- **Part 2** (slug unification): Shared `Player.BuildSlugSource` helper; seed registry; reversible re-backfill migration with snapshot-based inverse.
- **Manual Phase 7** (dev-DB verification): Completed 2026-08-29 against Supabase dev DB (320 players, real seeded data).

**Branch**: `fix/player-admin-slug-404`  
**Commits**: 3 (92632c3 Part 1, 373eeee Part 2 + migration, 254570f docs)  
**Pushed to origin**: Yes  
**PR status**: To `develop`, being opened by the user.

### Verification Status

Per **verify-report** observation #178 (dated 2026-08-29 14:11):
- **Build**: 0 warnings, 0 errors (SonarAnalyzer S3358/S3267 clean).
- **Backend tests**: 688 passed / 0 failed / 0 skipped.
- **Frontend tests**: 477 passed (105 test files).
- **Type-check**: tsc --noEmit exit 0.
- **Lint**: eslint --max-warnings 0 exit 0.
- **Spec scenarios**: 11/13 automated, 2/13 manual (Phase 7).
- **Critical findings**: 0.
- **Blockers**: 0.

**Machine envelope verdict**: `fail` (by design — 2/13 scenarios deferred to manual Phase 7, which cannot execute migration SQL in the SQLite CI harness).  
**Human-facing verdict** (per verify-report section 4.6): **PASS WITH WARNINGS** — once Phase 7 is executed and recorded, archive-ready.

**Phase 7 Execution** (2026-08-29, recorded in **final-state facts**):
- ✓ Applied migration against Supabase dev DB (320 players).
- ✓ Assertions all pass: `dni_leak=0`, `dup_slugs=0`, `tmp_left=0`, `bad_shape=0`, `ledger_gap=0`.
- ✓ Collision suffixes applied to 304 slugs (real data deduplication extent).
- ✓ Browser verification: admin "Ver" loads without 404; match-page sanction links load.
- ✓ Rollback proof: Down migration restores 320 slugs to pre-migration values (`mismatches=0`).
- ✓ Re-applied forward; dev DB on new format.

### Specs Synced

**New capability created**:
- `openspec/specs/player-slug-identity/spec.md` — canonical definition of player slug addressing (admin/public routes) and unified slug format.

**Source of spec**:
- Delta spec from `openspec/changes/fix-player-admin-slug-404/specs/player-slug-identity/spec.md` (observation #173).
- Merged as-is (new capability, all ADDED, no existing spec to merge).

### Change Folder Moved

**From**: `openspec/changes/fix-player-admin-slug-404/`  
**To**: `openspec/changes/archive/2026-08-29-fix-player-admin-slug-404/`

**Archive contents**:
- `proposal.md` (observation #172)
- `design.md` (observation #174)
- `tasks.md` (observation #175) — all 31 tasks marked complete ✓
- `verify-report.md` (observation #178)
- `explore.md` (observation #171)
- `specs/player-slug-identity/spec.md` (delta spec, now also in main specs)

### Known Non-Blocking Issues & Follow-Ups

Per **verify-report** observations 4.6 (Suggestions):

1. **Migration diacritic translation** (cosmetic): SQL `translate()` handles only 7 Spanish diacritics (á,é,í,ó,ú,ü,ñ). C# `SlugGenerator` handles all diacritics via NFD normalization. Pre-existing (verbatim reuse of migration `20260828003816`), design-acknowledged, blast radius cosmetic (no 404 risk, no uniqueness risk). **Future follow-up**: swap `translate()` for PostgreSQL `unaccent()` extension across all three slug backfill migrations.

2. **R8 partial coverage**: No single automated test asserts seed-vs-create slug equivalence (covered by construction via shared `Player.BuildSlugSource` helper + Phase 7 manual verification). **Future follow-up**: add direct seed-vs-create equivalence unit test.

3. **R6 partial coverage**: Public route wrong-case 404 sub-clause lacks dedicated assertion (public route source untouched, verified by inspection). **Future follow-up**: add explicit public-route wrong-case 404 test.

4. **Ledger table**:  `Club12.PlayerSlugBackup_20260829` persists by design (owner decision). **Intentional permanent** for now; revisit after release is confirmed. **Future action**: drop via a separate cleanup migration once rollback confidence is established.

5. **Migration Down precision** (documentation note): The design prose claim "double-Down is a safe no-op" is inaccurate — the 2nd UPDATE in Down is unguarded. EF migration history prevents double-apply, so this is a documentation-only SUGGESTION. **Future follow-up**: guard the 2nd Down UPDATE with `to_regclass()` for symmetry.

6. **Plaintext secrets in `appsettings.Franco.json`** (flagged to the user, separate concern): not an artifact of this change; pre-existing project-wide issue.

## Task Completion Validation

**Total tasks**: 31 (including checklist 0.1–0.3)  
**Completed**: 31 ✓  
**Incomplete**: 0  
**Stale checkboxes**: 0 (all completed tasks marked; no dangling partial work)

Phases 1–6 all automated; Phase 7 (manual dev-DB verification) explicitly marked complete per the context's final-state facts:
- 7.0 ✓ Collision path covered on real data (320 players, 304 with `-N` suffix).
- 7.2 ✓ Migration applied.
- 7.3 ✓ Post-apply assertions (dni_leak, dup_slugs, tmp, kebab, ledger gap, browser).
- 7.4 ✓ Rollback proof (Down applied, diffed, all 320 restored, then re-applied).
- 7.5 ✓ Evidence recorded in archive and PR description.

Phase 8 (full regression) ✓ all green (688/0 backend, 477/0 frontend, lint, tsc).

## Engram Artifact IDs (Traceability)

All SDD artifacts persisted to Engram for this change:

| Artifact | Engram Observation ID | Topic Key | Dated |
|----------|--------|-----------|-------|
| Exploration | #171 | `sdd/fix-player-admin-slug-404/explore` | 2026-08-29 13:02:XX |
| Proposal | #172 | `sdd/fix-player-admin-slug-404/proposal` | 2026-08-29 13:09:59 |
| Spec (delta) | #173 | `sdd/fix-player-admin-slug-404/spec` | 2026-08-29 13:12:42 |
| Design | #174 | `sdd/fix-player-admin-slug-404/design` | 2026-08-29 13:16:12 |
| Tasks | #175 | `sdd/fix-player-admin-slug-404/tasks` | 2026-08-29 13:18:34 |
| Verify-Report | #178 | `sdd/fix-player-admin-slug-404/verify-report` | 2026-08-29 14:11:25 |
| Archive-Report | (this) | `sdd/fix-player-admin-slug-404/archive-report` | 2026-08-29 |

**Note**: Observation IDs 171–175, 178 logged during proposal/spec/design/tasks/verify phases. Archive report (#archive-report) saved at close (this phase).

## Final-State Authority Hierarchy

This archive report reflects the FINAL state per the SDD archive contract's Final-State Authority rules:

**Rank 1 — Native review authority**: No active gentle-ai review (kill switch off, delivery: disabled/unmanaged). N/A.

**Rank 2 — Persisted tasks artifact**: `openspec/changes/archive/2026-08-29-fix-player-admin-slug-404/tasks.md` — all 31 tasks complete ✓. Authority: tasks file in archive.

**Rank 3 — Explicit final-state facts (launch prompt)**: 
- Implementation COMPLETE: branch pushed, commits present, PR opening.
- Build/test results final: 0 warnings, 688/0 backend, 477/0 frontend, tsc/lint clean.
- Manual Phase 7 DONE: 320 players, assertions green, browser verified, rollback proven.
- Ledger intentional permanent: owner decision.

**Rank 4 — Intermediate snapshots** (verify-report, apply-progress): 
- verify-report marked "partial" only due to Phase 7 gap (CI harness limitation, not defect).
- That gap is now closed; final-state facts declare Phase 7 done.
- Do NOT carry verify-report's "incomplete" claim into the archive; Phase 7 completion outranks it.

**Synthesis**: The change is COMPLETE and VERIFIED. All checkboxes marked. Phase 7 manual verification executed and passed. No CRITICAL issues. Archive is accurate as of 2026-08-29.

## Specs Merge Summary

| Spec Domain | Action | Requirements | Source |
|---|---|---|---|
| `player-slug-identity` | **Created** (new capability) | 10 requirements, 13 scenarios | Delta spec #173 copied to `openspec/specs/player-slug-identity/spec.md` |

**Merge details**:
- Delta spec was all-ADDED (no existing `player-slug-identity` spec to merge).
- Dropped "delta" framing; now reads as standalone capability spec.
- Full content preserved; no truncation.

## Change Eligibility Check

Before archiving, validated:

1. **Task Completion Gate**: ✓ All 31 tasks checked; no incomplete implementation tasks; Phase 7 manually verified.
2. **Native Review Receipt Gate**: ✓ Delivery mode disabled/unmanaged (kill switch off); no blocking review state.
3. **Critical Issues Gate**: ✓ 0 CRITICAL findings in verify-report; 0 blockers.
4. **Stale Checkboxes Reconciliation**: ✓ Not needed — all tasks genuinely complete; Phase 7 explicitly done.
5. **Artifact Completeness**: ✓ Proposal, Spec, Design, Tasks, Verify-Report all persisted and archived.

**Conclusion**: All gates passed. Archive eligible.

## Post-Archive Action Items

None required for the change itself. The following are optional future follow-ups (non-blocking):

1. Drop `Club12.PlayerSlugBackup_20260829` ledger via a cleanup migration once release confidence is established.
2. Add direct seed-vs-create slug equivalence unit test (R8 partial coverage).
3. Add explicit public-route wrong-case 404 test (R6 sub-clause).
4. Refactor migration transliteration to use PostgreSQL `unaccent()` extension (cosmetic diacritic handling).
5. Guard the 2nd Down UPDATE in any future migration refresh (documentation precision).

## Sign-Off

**Archive Phase**: sdd-archive  
**Executed**: 2026-08-29  
**Archived to**: `openspec/changes/archive/2026-08-29-fix-player-admin-slug-404/` + Engram topic `sdd/fix-player-admin-slug-404/archive-report`  
**Status**: CLOSED — ready for release

The SDD cycle for `fix-player-admin-slug-404` is complete. The change is fully planned, implemented, verified, and archived. All artifacts are persisted for audit trail and future reference.
