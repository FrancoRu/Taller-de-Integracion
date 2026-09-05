# Club12 Backend — XML Documentation Standard

> **Scope:** This document covers **XML doc comments** (`/// <summary>` and friends), **inline `//` comments** (see INLINE-001), and **one rule of file organization** (one type per file — see FILE-001) across `Domain`, `Application`, `Infrastructure`, and `API`. It intentionally does not cover naming, code style, nullable reference types, async conventions, or testing — those aren't in scope for this pass. `API.Tests` is excluded (test methods are self-documenting via their names; see the project's existing `MethodName_Scenario_ExpectedResult` convention).
>
> **Stack facts this document is grounded in (verified 2026-09-05):** .NET 8, xUnit 2.9.3. No `.editorconfig` exists. `Directory.Build.props` sets `<NoWarn>$(NoWarn);CS1591</NoWarn>` — missing-XML-doc warnings are suppressed, so nothing here is compiler-enforced; it's a review convention. `GenerateDocumentationFile=true` is set on all four non-test projects, but **Swagger (`AddCustomSwagger` in `API/Utils/StartupExtensions.cs`) only calls `IncludeXmlComments` for the API project's own generated XML file** — Domain/Application/Infrastructure XML comments never reach Swagger UI, they're IDE-only (IntelliSense). Rule XML-005 below addresses this gap directly.

---

## Why this exists

The codebase already has a real, working comment convention — establshed independently of this document, in files like `ErrorMessages.cs`, `StageService.cs`, and `PlayerSanctionService.cs`: comments explain *why* a rule exists or *why* a workaround is needed, never restate what the code already says via its own name. This document writes that convention down for XML comments specifically, so the backend-wide cleanup pass has a fixed target instead of "make it better."

---

## Rule XML-001 — Every public member gets a `<summary>`, but it earns its keep

Public types and members in `Domain`, `Application`, `Infrastructure`, `API` get a `<summary>`. A `<summary>` that only restates the identifier name is worse than no comment — it's noise a reader has to read past.

```csharp
// ❌ VIOLATION — restates the name, adds nothing
/// <summary>Gets or sets the tournament status.</summary>
public TournamentStatus Status { get; set; }

// ✅ CORRECT — no comment needed at all; the name is the whole story
public TournamentStatus Status { get; set; }
```

```csharp
// ✅ CORRECT — the name can't carry this; the comment earns its line
// (Application/Services/PlayerSanctionService.cs, actual codebase example)
/// <summary>
/// Fechas (match-days) still remaining on the sanction's suspension. Short-circuits
/// to 0 when the appeal was accepted — the sanction is lifted immediately rather
/// than continuing to count down, without mutating the original Duration so the
/// sanction's history stays intact.
/// </summary>
```

**Applies to:** public classes, interfaces, methods, and non-trivial properties (ones with business rules, side effects, or non-obvious units/ranges). Skip auto-properties whose name and type already say everything (`public Guid Id { get; set; }`, `public string Name { get; set; }`).

---

## Rule XML-002 — Document the WHY, never the WHAT

Matches this repo's existing inline-comment convention. A `<summary>` should carry the non-obvious part: a business rule, an invariant, a workaround for a specific bug, a reason a check exists. If removing the comment wouldn't confuse a future reader, don't write it.

```csharp
// ❌ VIOLATION — narrates the code instead of explaining the decision
/// <summary>
/// Loops through pending matches and sets their status to Canceled, then
/// calls UpdateRangeAsync to persist the changes.
/// </summary>
private async Task CancelPendingMatchesAsync(Guid tournamentId)

// ✅ CORRECT — states the rule and the invariant that isn't visible from the code alone
/// <summary>
/// Cascades a tournament's Cancel/Finish transition onto its still-pending matches,
/// marking them Canceled so the tournament and its matches never disagree about
/// whether play is still ongoing. Never marks a canceled match as finished — a
/// canceled match must never read as played.
/// </summary>
```

---

## Rule XML-003 — API-project comments are consumer-facing; other layers are IDE-only

Because of the Swagger wiring described above, `<summary>` text on **Controllers, request DTOs, and response DTOs in the `API` project** is what external API consumers actually see in Swagger UI — it functions as public documentation, not a code comment. Write it for that reader: describe the endpoint's behavior and each field's meaning/constraints, not implementation detail.

`<summary>` text in `Domain`, `Application`, and `Infrastructure` is IntelliSense-only (see XML-005) — it should still follow XML-001/002, but there's no external-consumer bar to clear.

```csharp
// ✅ CORRECT — API project, Swagger-visible, written for a consumer of the endpoint
// (API/Controllers/PlayerController.cs style)
/// <summary>
/// Creates a player as an administrator. Returns the full admin-facing player
/// record (including PII fields not exposed on the public endpoint).
/// </summary>
```

---

## Rule XML-004 — No stale comments

A `<summary>` describing behavior the code no longer has is actively harmful — worse than a missing one, because it's read as true. When editing a method, re-read its XML comment and correct or delete anything the change invalidated. This is the most common real defect to look for in the audit pass: comments left behind after a signature or behavior change.

---

## Rule XML-005 — Fix the Swagger `IncludeXmlComments` gap

`API/Utils/StartupExtensions.cs`'s `AddCustomSwagger` only loads `{ExecutingAssemblyName}.xml` (i.e. `API.xml`). `Domain.xml`, `Application.xml`, and `Infrastructure.xml` are generated (`GenerateDocumentationFile=true` is set on all four projects) but never passed to `IncludeXmlComments`, so DTO/enum XML comments defined outside the API project (e.g. on `Application.DTOs.*` request/response types) don't show up in Swagger's schema view even though they exist and are well-written.

Fix: call `IncludeXmlComments` for each of the four generated `.xml` files (guard each with `File.Exists` — publish/CI output layout can vary), not just the executing assembly's own file.

---

## Rule XML-006 — Plain prose in `<summary>`, no inline markup, no parentheses, exactly 3 lines

**Added 2026-09-05 after the first cleanup pass shipped comments full of `<see cref>` and `<c>` — that's exactly the clutter this rule now forbids. Tightened five times the same day, in order: after later passes still left parenthetical asides and summaries wrapped across too many `///` lines; after a follow-up correction clarified the target shape is 3 lines, not 1; after a correction dropped `<remarks>` as an overflow valve entirely — there is no escape hatch, everything must fit in the one summary line; after a correction banned user-story/ticket references (HU-XX) from doc comments outright; and after a final correction banned "for example" / "such as" example-introducing phrases too — no example call-outs of any kind, worded any way.**

`<summary>` text is plain English prose, held to five things at once:

1. **No inline markup.** Do not use `<see cref="...">`, `<c>...</c>`, `<paramref name="...">`, or `<typeparamref name="...">` inside it — write the member/parameter name as plain text instead. These tags read as noise in the source (which is where developers actually read comments — IntelliSense tooltips are secondary) and `<see cref>` targets silently go stale on a rename with no compiler warning, which is worse than plain text that can't "break."
2. **No parentheses at all**, not just "e.g." asides. A parenthetical is a second thought bolted onto the sentence — fold it into the main clause or cut it.
3. **No example call-outs at all** — not "e.g.", not "for example", not "such as", not "like X". State the general rule; if a concrete example is genuinely necessary to make the rule clear, it must read as a plain fact in the sentence, not flagged as an example.
4. **No user-story/ticket references** (`HU-99`, `HU-63/HU-65`, and the like). A ticket ID is external tracking metadata, not something a reader of the code needs — describe the actual rule or behavior instead of pointing at a ticket number.
5. **Exactly 3 lines: the opening tag alone, one line of concise text, the closing tag alone.** Never collapse it to one line (`<summary>text</summary>`), and never let the text wrap onto a second or third `///` line — one fact, one line, tags on their own lines around it. If the fact doesn't fit on one line, it's saying too much — cut it down to the single most important thing (per XML-001/002) and drop the rest. **No `<remarks>` tag either** — there is no overflow container; a second fact that doesn't fit gets cut, not relocated.

The structural, block-level tags — `<param>`, `<returns>`, `<exception>`, `<typeparam>` — are kept: they're not inline reference clutter, they're separate documented fields that Swagger and IntelliSense render distinctly. Their content follows the same plain-prose/no-parens rule but isn't held to the 3-line shape if a parameter genuinely needs more explaining.

```csharp
// ❌ VIOLATION — inline markup, a parenthetical, and text wrapped across 3 lines of prose
/// <summary>
/// Short-circuits to 0 when the appeal was accepted, without mutating the
/// original <see cref="PlayerSanction.Duration"/> (a 3-fecha sanction
/// stays <c>Duration = 3</c> in the record).
/// </summary>

// ❌ ALSO A VIOLATION — collapsed to one line instead of the 3-line shape
/// <summary>Short-circuits remaining fechas to 0 when the appeal was accepted, leaving Duration untouched.</summary>

// ✅ CORRECT — 3 lines, one line of plain-prose text, no parens
/// <summary>
/// Short-circuits remaining fechas to 0 when the appeal is accepted, leaving Duration untouched.
/// </summary>
```

---

## Rule INLINE-001 — Inline `//` comments follow the same discipline as XML docs

**Added 2026-09-05.** Inline `//` comments were out of scope for the first pass, but the same principles apply once they're in scope:

1. **WHY, not WHAT** (same as XML-002). A `//` comment narrating what the next line does is noise — the code already says that. Keep only comments that state a business rule, an invariant, a workaround, or a reason a check exists.
2. **No parentheses.** Same as XML-006 — fold the aside into the sentence or cut it.
3. **No user-story/ticket references** (`HU-99` and the like) — describe the actual rule, don't point at a ticket number.
4. **No example call-outs** — no "e.g.", "for example", "such as", "like X".
5. **One line.** A `//` comment is one short sentence. If it needs more than that, it's saying too much — cut it down to the one fact that earns it.
6. **Delete comments that just restate the line below them.** If removing the comment wouldn't confuse a future reader, don't write it.

```csharp
// ❌ VIOLATION — restates the code, has a ticket ref and a parenthetical
// Loop through the pending matches (HU-101) and cancel them one by one.
foreach (Match match in pendingMatches)

// ✅ CORRECT — states the invariant the code doesn't say on its own
// A canceled match must never read as played, so IsFinished stays untouched here.
match.Status = MatchStatus.Canceled;
```

---

## Rule FILE-001 — One type per file

Each `.cs` file declares exactly one top-level type: one class, interface, enum, record, or struct. A file with two top-level `public`/`internal` types (a common pattern in this codebase for small request/response DTO pairs, or an enum declared alongside the class that uses it) gets split into one file per type, named after the type it contains.

**Not a violation:** a private/nested type declared inside its owning class (it's not top-level), and `partial class` declarations of the same type split across multiple files (each file still declares exactly one type).

```csharp
// ❌ VIOLATION — two top-level types in one file (CreatePlayerRequest.cs)
public class CreatePlayerRequest { /* ... */ }
public class CreatePlayerAddressRequest { /* ... */ }

// ✅ CORRECT — split into CreatePlayerRequest.cs and CreatePlayerAddressRequest.cs
```

---

## Rule NAME-001 — `public const` fields are PascalCase, no exceptions

**Added 2026-09-05.** Verified by sampling every constants class in the backend: PascalCase is the dominant, near-universal convention. Five files were the exception (`MaxTeams`, `TournamentBracketSize`, `ScoreConstants`, `KnockoutMatchCount`, `RoundRobinFormat` all used SCREAMING_SNAKE_CASE) and have been renamed to match — `GROUP` → `Group`, `EIGHT` → `Eight`, `POINTS_FOR_WIN` → `PointsForWin`, and so on, values unchanged, every call site updated.

```csharp
// ❌ VIOLATION
public const int GROUP_STAGE_CAP = 32;

// ✅ CORRECT
public const int GroupStageCap = 32;
```

Everything else sampled — interface `I` prefixes, `_camelCase` private fields, `Async`-suffixed async methods, file-scoped namespaces, primary constructors — was already 100% consistent; those conventions are codified in `.editorconfig`, not repeated here.

---

## Rule ASYNC-001 — No sync-over-async blocking (`.Wait()`, `.Result`, `.GetAwaiter().GetResult()`)

**Added 2026-09-05.** The backend was already almost entirely clean of this anti-pattern — a repo-wide search found exactly one instance: `SupabaseHelper`'s constructor called `_client.InitializeAsync().Wait()`, forced by the fact that C# constructors cannot be `async`. Fixed with a `Lazy<Task>` field, initialized in the constructor without blocking and awaited at the top of every public method that touches `_client`, so initialization stays lazy, thread-safe, and non-blocking instead of running synchronously inside the constructor.

```csharp
// ❌ VIOLATION — blocks a thread synchronously waiting on async work
public SupabaseHelper(IConfiguration configuration)
{
    _client = new Client(baseUrl, serviceRole, options);
    _client.InitializeAsync().Wait();
}

// ✅ CORRECT — lazy, thread-safe, never blocks
private readonly Lazy<Task> _initialization;

public SupabaseHelper(IConfiguration configuration)
{
    _client = new Client(baseUrl, serviceRole, options);
    _initialization = new Lazy<Task>(_client.InitializeAsync);
}

public async Task UploadRawAsync(string objectPath, Stream content, string? bucket = null)
{
    await _initialization.Value;
    // ...
}
```

**`ConfigureAwait(false)` is correctly NOT used** in this codebase, and that's not a gap to fill in — it's the right call for an ASP.NET Core app. `ConfigureAwait(false)` exists to avoid deadlocks from resuming on a captured `SynchronizationContext`; ASP.NET Core has no such context, so the call is a no-op that only adds noise. Do not add it.

---

## Rule NULL-001 — Nullable reference types are enabled and used correctly; the null-forgiving operator is a legitimate, audited pattern here

**Added 2026-09-05.** `<Nullable>enable</Nullable>` is set on all four projects — verified, not assumed. A sample of the 123 null-forgiving (`!`) operators across the backend confirms they fall into a small number of legitimate, consistent patterns, not lazy suppression of real nullability warnings:

- EF Core `Include` selector lambdas on nullable navigation properties (`includes: [m => m.HomeTeam!, m => m.VisitorTeam!]`) — the most common case by far. `Include()`'s lambda signature needs a non-null-typed property selector even though the navigation property itself is nullable in the model; this is the standard, unavoidable EF Core idiom for this situation.
- Values guaranteed non-null by an invariant the type system can't see (e.g. `match.HomeScore!.Value` on a match already filtered to `IsFinished`).
- Deferred-assignment locals (`Division division = null!;` followed by an awaited closure that assigns it before the method returns) — C#'s definite-assignment analysis can't see through the lambda/await boundary, but the assignment is guaranteed to run first.
- Framework APIs that return `object?`/`string?` where the actual contract guarantees non-null for valid input (`MethodInfo.Invoke(...)!`, `configuration[key]!` for a required config key).

No fix needed here — this rule documents that the pattern is intentional and consistent, so a future reviewer doesn't mistake it for carelessness or try to "clean it up" into something less correct.

---

## Rule ERROR-001 — Exceptions are always logged or translated, never silently swallowed

**Added 2026-09-05.** Every `catch (Exception ...)` block in the backend (22 total, audited individually) either logs the exception with `ILogger`/Serilog and a clear, specific message before continuing, or translates it into a domain-specific exception (`InvalidOperationException` with an `ErrorMessages.*` message) that `GlobalExceptionHandler` maps to an HTTP status. None discard the exception silently. Keep doing this — a caught exception with no logging and no rethrow is a bug report waiting to happen.

**No `#pragma warning disable` outside of EF migrations.** The five that existed (`S3267`, `S2583` ×1, `S1075`, `S6960` ×2) were all removed by fixing the underlying issue instead of suppressing it:

- **S3267** (`BackupOperationsService.ApplyRetentionAsync`) — the loop mixed a lookup-and-skip step with its async delete/remove side effects. Moved the lookup into a `.Select(...).OfType<BackupRecord>()` pipeline ahead of the loop, leaving the `foreach` for the side effects alone. The analyzer no longer flags it, and the code reads better.
- **S2583** (`PgDumpBackupService`) — a closure-captured counter (`strippedCount++` inside a `Regex.Replace` `MatchEvaluator`) confused Sonar's dataflow analysis into thinking the count could never be nonzero. Replaced with `Pattern.Matches(input).Count` computed before the replace — same result, no closure, no false positive, and arguably clearer code.
- **S1075** (`DataSeeder.DefaultLogosPath`) — a hardcoded `D:\...` path was a personal-machine default masquerading as a real fallback. Removed the constant entirely; the seeder now just logs "not configured" and keeps the generated crests when `Seed:LogosPath` isn't set, which the graceful-degradation path already handled anyway.
- **S6960** ×2 (`BackupController`, `ScorerController`) — "controller has multiple responsibilities," triggered because each controller's actions used disjoint subsets of its injected dependencies. Fixed by consolidating: `BackupController` now depends only on `IBackupOperationsService` (which already wrapped `IBackupCatalog` internally, so it gained a `ListNewestFirstAsync` passthrough); `ScorerController` now depends only on `IScorerService` (which gained a `GetAllScorersByTeamAsync` method that internally calls `IMatchService`/`IScorerMapper`, moving that orchestration out of the controller and into the service layer where it belongs).

If a future warning genuinely can't be fixed without making the code worse, that's a conversation to have explicitly — not a silent suppression.

---

## Rule TEST-001 — Test naming and structure (already established, documented here)

**Added 2026-09-05.** No changes needed — this documents the convention already in consistent use across all 851 tests, so it doesn't drift as the suite grows:

- Test method names follow `MethodUnderTest_Scenario` or `MethodUnderTest_ExpectedResult` (one or more `_`-separated segments after the method name) — not a fixed 3-part template, but always method-first, always descriptive of the scenario or the expected outcome.
- No `Thread.Sleep` anywhere in the suite (verified) — timing-dependent tests are a flakiness risk this codebase has avoided.
- No mocking library (no NSubstitute/Moq/FakeItEasy) — tests run against a real EF Core Sqlite provider and `Microsoft.AspNetCore.Mvc.Testing`'s `WebApplicationFactory`, favoring integration-style coverage over isolated unit tests with mocked dependencies. This is a deliberate, consistent choice; don't introduce a mocking library for new tests without a specific reason the existing approach can't cover.
- No shared mutable static state between test classes (verified) — each test builds its own data, avoiding cross-test contamination under xUnit's default parallel-by-class execution.

---

## What this document does not cover (yet)

Code style beyond what's already in `.editorconfig`, and file/project organization beyond FILE-001, are out of scope for this pass.
