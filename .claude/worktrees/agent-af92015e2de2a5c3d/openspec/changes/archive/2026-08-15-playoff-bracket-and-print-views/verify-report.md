```yaml
schema: gentle-ai.verify-result/v1
evidence_revision: sha256:a873c1c1e36cf7fc6a0154f64a2e09a64c8758bb8a692f576a29bc3d3afc83e4
verdict: pass_with_warnings
blockers: 0
critical_findings: 0
requirements: 13/13
scenarios: 17/17
test_command: npm run test
test_exit_code: 0
test_output_hash: sha256:d59c3521446776ebbc8a26f719951efdc93f72f09d004b7005605c2c176dbdf9
build_command: npm run build
build_exit_code: 0
build_output_hash: sha256:9eb36d5f190ed047b720be41398bcb51b924495de0ea025abdbb6edab72b1565
```

## Verification Report

**Change**: playoff-bracket-and-print-views
**Version**: N/A (delta spec, no prior version)
**Mode**: Strict TDD (Phase 2 builder) + Standard (Phase 3-5 UI)

### Completeness
| Metric | Value |
|--------|-------|
| Tasks total (checkbox items counted) | 23 (tasks.md footer claims "24 tasks complete" -- off-by-one in the document's own summary line; all 23 checkbox items are marked [x]) |
| Tasks complete | 23/23 |
| Tasks incomplete | 0 |

### Build & Tests Execution
**Build**: PASSED (npm run build, exit 0) -- tsc + vite build succeeded, including the new @fontsource/oswald asset emission.

**Tests**: PASSED -- 73/73 (npm run test, exit 0, 23 test files). Scoped re-run of `npx vitest run src/modules/playoff/buildBracket.test.ts` independently confirms 11/11 passing.

**Lint**: PASSED -- npm run lint (eslint . --ext ts,tsx --report-unused-disable-directives --max-warnings 0), exit 0, 0 warnings.

**Coverage**: Not available -- no --coverage script/config in this repo; skipped per rules (not a failure).

### Spec Compliance Matrix
| Requirement | Scenario | Test | Result |
|---|---|---|---|
| Llaves Tab on Public Tournament Page | Both tabs available | (none -- source-only: PublicTournamentPage.tsx:54,201-211) | UNTESTED |
| Llaves Tab on Public Tournament Page | No elimination stages for the division | (none -- source-only: PlayoffBracket.tsx:24-32) | UNTESTED |
| Bracket Scoped Per Division | Multi-division tournament | (none -- source-only: PublicTournamentPage.tsx:135-158) | UNTESTED |
| Round Grouping by Stage Type Order | Standard bracket depth | buildBracket.test.ts > orders rounds Cuartos -> Semifinal -> Final | COMPLIANT |
| Third Place as Side Match, Final as Terminal Node | Third place and final coexist | buildBracket.test.ts > drops Group stages entirely and holds ThirdPlace aside (data partition only; visual placement in PlayoffBracket.tsx untested) | PARTIAL |
| TBD Slots for Unresolved Participants | Next round not yet seeded | buildBracket.test.ts > preserves a null homeTeam/visitorTeam (data preservation only; TBD text render in BracketMatchNode.tsx untested) | PARTIAL |
| Match Node Content | Played match with score | (none -- source-only: BracketMatchNode.tsx:67-71) | UNTESTED |
| Client-Side Connector Inference | Clear winner advances | buildBracket.test.ts > emits one edge when a Cuartos winner appears in exactly one Semifinal match (edge inference only; SVG line render in BracketConnectors.tsx untested) | PARTIAL |
| Graceful Degradation on Ambiguous Inference | Unplayed match, no winner yet | buildBracket.test.ts > emits no edge when the source match has no winningTeamId yet | COMPLIANT |
| Graceful Degradation on Ambiguous Inference | Ambiguous winner mapping | buildBracket.test.ts > emits no edge when the winner matches zero/more-than-one next-round slots (2 cases) | COMPLIANT |
| Print Action on Division Standings View | Organizer triggers print | (none -- source-only: PrintableResultsSheet.tsx:116-118) | UNTESTED |
| Selectable Print Target | Print standings only | (none -- source-only) | UNTESTED |
| Selectable Print Target | Print goleadores only | (none -- source-only) | UNTESTED |
| Selectable Print Target | Print both tables | (none -- source-only) | UNTESTED |
| Print-Only CSS Hides App Chrome | Chrome hidden when printing | (none -- source-only: PrintableResultsSheet.tsx:41-60) | UNTESTED |
| Page-Break Handling for Long Tables | Long standings table spans multiple pages | (none -- source-only: PrintableResultsSheet.tsx:53-54) | UNTESTED |
| No New Dependency for Printing | Dependency check | Static verification: git diff package.json shows only @fontsource/oswald added (a webfont, not a PDF/print-rendering library) | COMPLIANT |

**Compliance summary**: 4/17 scenarios COMPLIANT (runtime-test-verified), 3/17 PARTIAL (business logic unit-tested, DOM/visual layer unverified), 10/17 UNTESTED (source-verified only, no automated runtime coverage). Treated as WARNING, not CRITICAL -- see Issues Found for rationale.

### Correctness (Static Evidence)
| Requirement | Status | Notes |
|---|---|---|
| Llaves Tab on Public Tournament Page | Implemented | "llaves" added to Tab union + Tab label Llaves (PublicTournamentPage.tsx:54,210); does not remove/alter Partidos tab |
| Bracket Scoped Per Division | Implemented | Per-division buildBracket() call, results keyed by division.id in Record<GUID,BracketModel> (PublicTournamentPage.tsx:135-158) |
| Round Grouping by Stage Type Order | Implemented and unit-tested | ROUND_ORDER map + sortMainStages + buildRound (buildBracket.ts:11-32) |
| Third Place as Side Match, Final as Terminal Node | Implemented | ThirdPlace held aside from mainStages (buildBracket.ts:74-81); rendered as a separate Stack with alignSelf flex-end beside main round columns (PlayoffBracket.tsx:62-85); Final is the last element of the ordered rounds array, rendered rightmost |
| TBD Slots for Unresolved Participants | Implemented | teamLabel() renders "A definir" for a null team (BracketMatchNode.tsx:11) |
| Match Node Content | Implemented | Team name + score shown when match.isFinished (BracketMatchNode.tsx:67-71) |
| Client-Side Connector Inference | Implemented and unit-tested | buildEdgesForRoundPair matches winningTeamId against next-round participant ids, emits edge only when exactly one match (buildBracket.ts:43-65) |
| Graceful Degradation on Ambiguous Inference | Implemented and unit-tested | Guards verified directly in source: !winnerId (line 53), targets.length !== 1 (line 59), nextRound.matches.length === 0 (line 49) -- matches spec's exact four ambiguity conditions |
| Print Action on Division Standings View | Implemented | Button onClick calls window.print(), labeled Imprimir (PrintableResultsSheet.tsx:116-118) |
| Selectable Print Target | Implemented | ToggleButtonGroup with standings/goleadores/both controlling showStandings/showGoleadores (PrintableResultsSheet.tsx:89-90,104-115) |
| Print-Only CSS Hides App Chrome | Implemented (deviation, disclosed) | body * visibility hidden + forced-visible [data-print=sheet] isolation trick, scoped inside PrintableResultsSheet.tsx:41-60, instead of design.md's per-element data-print=hide tagging across Layout/Sidebar |
| Page-Break Handling for Long Tables | Implemented | thead display table-header-group, tr breakInside avoid (PrintableResultsSheet.tsx:53-54) |
| No New Dependency for Printing | Implemented | No PDF/print-rendering library added; @fontsource/oswald (webfont for on-screen typography) is the only new dependency -- technically compliant with this requirement's letter but undisclosed as a change (see WARNING) |

### Coherence (Design)
| Decision | Followed? | Notes |
|---|---|---|
| Bracket model source: pure builder in modules/playoff | Yes | |
| Round ordering: ROUND_ORDER map, tie-break stage.order | Yes | |
| ThirdPlace as side node, not own column | Yes | |
| Connector inference via winningTeamId matching | Yes | |
| Ambiguity handling: degrade to column-only, never guess | Yes | Verified against real source -- matches all four documented conditions exactly |
| Connectors render: SVG overlay only from model.edges | Yes | BracketConnectors.tsx never guesses; renders strictly from the edges array it is given |
| Styling: MUI sx/styled, theme tokens | Partial | New bracket components hardcode raw hex/rgba values (#FF5A1F, rgba(255,90,31,0.12)) instead of referencing theme.palette tokens; also introduce an Oswald font family not mentioned anywhere in design.md |
| Print: @media print + window.print(), zero new dependencies | Partial | Print mechanism itself adds no dependency (holds), but the change as delivered adds @fontsource/oswald -- not disclosed in design.md's File Changes table or apply-progress's Files Changed table |
| Print-chrome hiding via data-print=hide on every nav/sidebar element | Deviation (disclosed) | Implemented as a scoped CSS isolation trick entirely inside PrintableResultsSheet.tsx instead -- functionally equivalent, avoids touching out-of-scope Layout/Sidebar files; explicitly disclosed in apply-progress's Deviations section |
| sortPositions extraction to new module | Deviation (disclosed) | Not in design.md's File Changes table; required by ESLint's react-refresh/only-export-components rule (--max-warnings 0); explicitly disclosed in apply-progress |

### TDD Compliance
| Check | Result | Details |
|---|---|---|
| TDD Evidence reported | Yes | Found in apply-progress, Phase 2 (buildBracket.ts) RED/GREEN/TRIANGULATE/SAFETY NET/REFACTOR table for all 5 task pairs |
| All tasks have tests | Partial | Only Phase 2 (builder, 5 task-pairs) has dedicated test files; Phases 3-5 (UI) have none -- consistent with apply-progress's declared Standard (no dedicated component test files) mode for UI, not Strict TDD |
| RED confirmed (tests exist) | Yes | buildBracket.test.ts exists on disk, read directly, contains the 11 described cases |
| GREEN confirmed (tests pass) | Yes | Independently re-ran npx vitest run src/modules/playoff/buildBracket.test.ts -> 11/11 passed; full suite -> 73/73 passed |
| Triangulation adequate | Yes | Ambiguity-degradation requirement (4 distinct spec conditions: null winner, 0-match, more-than-1-match, empty next round) has 4 distinct test cases, each asserting the same edges:[] outcome from a different precondition -- legitimate triangulation of the branch conditions |
| Safety Net for modified files | N/A | buildBracket.ts / buildBracket.test.ts / bracket.d.ts are new files, no pre-existing tests to safety-net |

**TDD Compliance**: 5/6 checks passed (the one Partial reflects UI phases using the repo's standard/non-TDD convention, not a TDD-protocol violation for the builder itself)

---

### Test Layer Distribution
| Layer | Tests | Files | Tools |
|---|---|---|---|
| Unit | 11 | 1 (buildBracket.test.ts) | Vitest |
| Integration | 0 | 0 | React Testing Library (installed, unused for this change) |
| E2E | 0 | 0 | not installed |
| Total | 11 | 1 | |

---

### Changed File Coverage
Coverage analysis skipped -- no coverage tool/script configured in this repo (npm run test runs vitest run without --coverage).

---

### Assertion Quality
No violations found in buildBracket.test.ts (the only new test file):
- No tautologies.
- Every expect(model.edges).toEqual([]) (degradation cases) has a companion non-empty-result test (emits one edge when a Cuartos winner appears in exactly one Semifinal match) proving the assertion is not vacuous.
- All assertions call the real buildBracket() production function -- no dead assertions.
- No ghost loops, no mocks (pure function, zero vi.mock() calls), no CSS/implementation-detail coupling.

**Assertion quality**: All assertions verify real behavior.

---

### Quality Metrics
**Linter**: No errors, 0 warnings (--max-warnings 0 enforced and passing)
**Type Checker**: No errors (part of npm run build's tsc step, which succeeded)

### Issues Found

**CRITICAL**: None.

**WARNING**:
1. Undisclosed new dependency: @fontsource/oswald was added to package.json/package-lock.json, and Oswald CSS weight imports were added to Club12-WebClient/src/App.tsx (import "@fontsource/oswald/500.css" etc.) -- required for PlayoffBracket.tsx's hardcoded fontFamily "Oswald, sans-serif" round labels. Neither file appears in design.md's File Changes table nor in apply-progress's Files Changed table or line-count accounting (apply-progress's approximately-1057-lines total explicitly covers only the 11 files it lists). Functionally harmless (build succeeds, font loads), but it is scope creep beyond the declared change surface and under-reports the true diff.
2. Working tree contains substantial unrelated pending changes that must not be swept into this change's PR: Club12-WebClient/src/theme.ts (approximately 140-line full color/typography rebrand, FD6B00 to FF5A1F, new navy secondary, MuiAppBar/MuiTableHead/MuiDataGrid restyle), theme.color-tokens.test.ts, README.md, MANUAL_USUARIO.md (untracked), three backend test files (PlayerSanctionServiceTests.cs, ScorerRepositoryTests.cs, TeamServiceRegisterTests.cs), and a fully separate already-archived SDD change (openspec/changes/archive/2026-08-15-high-test-coverage-batch-c-remaining/). None of these are part of playoff-bracket-and-print-views's design.md File Changes table or apply-progress's Files Changed table -- they are leftover uncommitted work from other, unrelated changes sitting in the same working tree. This confirms the requested check "no backend files were touched" is TRUE for this change specifically (the .cs files belong to a different, already-archived change), but "no unrelated files were modified" requires care: git add for this PR must be scoped explicitly to the 11 playoff-bracket-and-print-views files, not a blanket git add -A / git add ..
3. 10 of 17 spec scenarios have no automated runtime-test coverage (Llaves tab visibility, per-division isolation, TBD/score/connector visual rendering, print-target selection, print CSS chrome-hiding, page-break handling) -- verified only by direct source-code inspection, not by a passing test. Downgraded from the default CRITICAL severity to WARNING because: (a) independently confirmed this matches a genuine, pre-existing repo convention -- searching src/views for *.test.tsx returns only 5 files in the entire tree (all in team/ and blogPost/, none for simple presentational containers like MatchCard, PublicTournamentPage, or any other public view); (b) tasks.md 6.2/6.3 already honestly disclose these as code-review-only verification and explicitly recommend a manual browser spot-check before merge, rather than silently claiming test coverage that does not exist.
4. 3 of 17 spec scenarios are PARTIAL: the underlying business logic (ThirdPlace/Final ordering, TBD data preservation, connector edge inference) is unit-tested via buildBracket.test.ts, but the corresponding visual/DOM rendering (PlayoffBracket.tsx, BracketMatchNode.tsx, BracketConnectors.tsx) has no covering component test.
5. tasks.md's own closing line claims "All 24 tasks complete" but the document contains 23 checkbox items (0.1, 1.1, 2.1-2.10, 3.1-3.3, 4.1-4.2, 5.1-5.3, 6.1-6.3) -- a minor off-by-one in the artifact's self-reported count, not a missing task.

**SUGGESTION**:
1. Consider adding lightweight RTL smoke tests for BracketMatchNode, PlayoffBracket, and PrintableResultsSheet -- design.md's own Testing Strategy table already listed Component Node/print tests as optional; adding them would convert most of the 10 UNTESTED scenarios into COMPLIANT ones at relatively low cost, since the branching logic they would exercise (winner highlight, TBD label, print-target toggling) is simple and already isolated in small, pure-prop components.
2. Reference theme.palette.primary.main / theme.palette.secondary.main from bracket/connector components instead of hardcoding #FF5A1F / rgba(255,90,31,...), per design.md's own "Styling: MUI sx/styled, theme tokens" decision.
3. Document the @fontsource/oswald addition explicitly as a deviation in a future apply-progress update, for traceability.

### Verdict
PASS WITH WARNINGS
Build/lint/tests all pass (73/73, 0 lint warnings), all 23 tasks complete, and the pure connector-inference/degradation logic (the change's actual technical risk) is correctly implemented and fully unit-tested with no CRITICAL defects found in source review -- but most UI-facing spec scenarios lack automated runtime coverage (mitigated by genuine pre-existing convention and honest self-disclosure) and the working tree holds unrelated pending changes that must be excluded from this PR's staging.
