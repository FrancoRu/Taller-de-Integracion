# Design: Private Medical-Records Bucket + File-Backed Habilitación + Seed Backfill

> Phase: sdd-design · Store: hybrid (Engram topic `sdd/medical-records-storage-eligibility/design` + this file)
> Builds on `proposal.md` and `exploration.md` in this folder. Scope pre-approved; not re-litigated.

## Technical Approach

**Part 1** parameterizes one existing seam instead of adding a second Supabase
client. `ISupabaseRawStorage` gains a trailing optional `string? bucket = null`
on all four raw methods; `SupabaseHelper` resolves `bucket ?? _bucketName` per
call. `SupabaseMedicalRecordStorage` reads `SupaBase:MedicalRecordsBucketName`
(default `"medical-records"`) exactly the way `SupabaseHelper` reads its own
config, drops the now-redundant `medical-records/` key prefix, and repartitions
the key on `teamId`. Backup and image call sites pass nothing and are
byte-identical.

**Part 2** moves the habilitación rule *down* into Domain. `PlayerTeamRegistration`
— which already owns `MedicalRecordStatus` and `MedicalRecordFileUrl` — gets
`IsStoredReference` + a computed `IsHabilitado`, and every read site calls it.
This is the same layering precedent as `Player.BuildSlugSource`
(archive/2026-08-29-fix-player-admin-slug-404, ADR #2): Application and
Infrastructure call *downward* into a Domain invariant instead of each
re-encoding it. The write guard in `ReviewAsync` uses the identical predicate.

**Part 3** adds `SeedMedicalRecordsAsync` to `DataSeeder`, modelled on
`UploadTeamLogosAsync` but running **after** `SaveChangesAsync` (see ADR #6),
and strips the fake ref from `SampleTournamentBuilder`.

## Architecture Decisions

| # | Decision | Alternatives rejected | Rationale |
|---|---|---|---|
| 1 | `ISupabaseRawStorage`: trailing **optional parameter** `string? bucket = null` on all four raw methods | Four extra **overloads** `(string bucket, …)`; a separate `IBucketScopedRawStorage`; a second `SupabaseHelper` instance keyed to the medical bucket | Verified there are exactly **3** implementers: `SupabaseHelper`, `API.Tests/Backup/Fakes/FakeSupabaseRawStorage`, and the private `InMemoryRawStorage` in `MedicalRecordDownloadTests.cs:147`. Optional param = **4 signature edits per implementer**; overloads = **8 methods per implementer** plus a delegating body in each. Sonar S2360 ("optional parameters") is already accepted project-wide on this exact family of boundaries — `IMedicalRecordStorage.StoreAsync(…, CancellationToken ct = default)` and `DataSeeder.SeedAsync(bool reset = false, string? logosPath = null)` both ship today. A second client would add a second blocking `InitializeAsync().Wait()` in a startup singleton |
| 2 | `SupabaseMedicalRecordStorage(ISupabaseRawStorage rawStorage, IConfiguration configuration)`, with an in-code default | New `MedicalRecordStorageOptions` + `IOptions<>`; DI lambda passing a literal bucket string | Mirrors `SupabaseHelper.cs:37-42` verbatim (`configuration.GetSection(ConfigurationKeys.Supabase.Section)[…]`). No new options class, and `RegisterSingletons` keeps its plain `AddSingleton<IMedicalRecordStorage, SupabaseMedicalRecordStorage>()` shape. `?? DefaultMedicalRecordsBucket` means **no appsettings edit is required** for the change to work (`appsettings.json` has no `SupaBase` section at all today) |
| 3 | Object key `{teamId}/{playerId}/{guid}{ext}` — `medical-records/` prefix **dropped** | Keep the prefix inside a bucket already named `medical-records` | The prefix was only ever a partition *within* `public-images`, alongside `backups/`. A dedicated bucket makes it pure redundancy, and keeping it would make "legacy ref" and "new ref" indistinguishable — the whole discriminator in ADR #4 depends on the prefix being gone |
| 4 | **"Real file" = non-blank AND not starting with `medical-records/`**, defined once as `PlayerTeamRegistration.IsStoredReference(string?)` in Domain and reused by the read sites, the write guard, and the seed | (a) naive `!string.IsNullOrWhiteSpace(MedicalRecordFileUrl)`; (b) rely on the seed reseed landing in the same PR | **Naive is wrong.** `SampleTournamentBuilder.cs:47` writes a non-empty *fake* ref to every Approved row, so ~280 seeded players would keep reading habilitado between the Part 2 merge and the Part 3 seed run — defeating "effective immediately". Option (b) leaves a documented-wrong window and couples correctness to operator discipline. One Domain predicate makes the read rule, the approve guard, and the seed's skip-vs-upload decision literally the same code, so they can never drift |
| 5 | `PlayerTeamRegistration.IsHabilitado` is the single rule; `Player` carries a transient **`bool HasMedicalRecordFile`**, not the file path | Transient `string? MedicalRecordFileUrl` on `Player`; recompute the rule at each of the 5 read sites | `Player.IsHabilitado` is projected by AutoMapper name convention onto `PublicPlayerResponse` (`[AllowAnonymous] GET api/players/{idOrSlug}` and `/public`). Putting a **private storage object path** on the entity feeding an anonymous endpoint is an unnecessary disclosure surface; a bool carries the decision and nothing else. `PublicPlayerResponse.cs:55` therefore needs **zero code change** — verified there is no `ForMember` override for `IsHabilitado` in `Application/Utils/Mappers` |
| 6 | `SeedMedicalRecordsAsync` runs **after** `db.SaveChangesAsync()` (`DataSeeder.cs:314`), querying the DB — unlike `UploadTeamLogosAsync`, which runs before it | Mirror the logo step exactly and upload from the in-memory graph before save | `EntityBase.Id` defaults to `Guid.Empty` and `SampleTournamentBuilder` explicitly writes `TeamId = Guid.Empty` / `PlayerId = Guid.Empty`; keys are **store-generated**. Before `SaveChangesAsync` there is no `teamId` or `playerId` to build the object key from. Querying after save is also what makes the standalone `Seed:MedicalRecords` backfill possible at all |
| 7 | Buffer the PDF to a `byte[]` **once**, wrap a fresh `MemoryStream` per upload | Reuse one `FileStream` across ~280 uploads; re-open the file per iteration | `SupabaseHelper.UseStreamDotReadMethod` (`:200-210`) reads the stream **to exhaustion**. A reused `FileStream` yields zero bytes from upload #2 onward — every record after the first would silently store an empty PDF. Re-opening per iteration also works but does ~280 file opens for a file measured in KB |
| 8 | `Seed:MedicalRecords` is a **bypass** (proposal semantics (b)), not an extra gate | An additional AND-gate required for the step to run | The step must run during a normal `Seed:Reset` reseed without extra flags, *and* be runnable alone against an already-seeded DB. Only the bypass reading satisfies both |
| 9 | Ship as **chained PRs P1 → P2 → P3** | Single PR | See Review Budget. P1 must land first regardless: under ADR #4, uploads made while P2 is live but P1 is not would produce `medical-records/…` refs that the new rule classifies as legacy |

## Interfaces / Contracts

```csharp
// Application/Utils/Helper/SupabaseHelper/ISupabaseRawStorage.cs — resolved interface
public interface ISupabaseRawStorage
{
    /// <param name="bucket">Target bucket; null uses the configured SupaBase:BucketName.</param>
    Task UploadRawAsync(string objectPath, Stream content, string? bucket = null);
    Task<IReadOnlyList<SupabaseStorageEntry>> ListRawAsync(string prefix, string? bucket = null);
    Task RemoveRawAsync(string objectPath, string? bucket = null);
    Task<byte[]> DownloadRawAsync(string objectPath, string? bucket = null);
}

// Infrastructure/Storage/SupabaseHelper.cs:115-192 — one added expression per method
await _client.Storage.From(bucket ?? _bucketName).Upload(UseStreamDotReadMethod(content), objectPath, new() { Upsert = true });
// …identical `bucket ?? _bucketName` substitution in ListRawAsync, RemoveRawAsync, DownloadRawAsync.
// UploadImageAsync<T>/DeleteImageAsync<T> are NOT touched — they stay hard-bound to _bucketName.

// Application/Utils/Constants/Configuration/ConfigurationKeys.cs:53-59
public static class Supabase
{
    public const string Section = "SupaBase";
    public const string ProjectUrl = "ProjectUrl";
    public const string ServiceRole = "ServiceRole";
    public const string BucketName = "BucketName";
    public const string MedicalRecordsBucketName = "MedicalRecordsBucketName";   // NEW
}

// Infrastructure/Storage/SupabaseMedicalRecordStorage.cs — full new shape
public sealed class SupabaseMedicalRecordStorage : IMedicalRecordStorage
{
    /// Private bucket, provisioned manually per environment. Never created by the client.
    public const string DefaultBucketName = "medical-records";

    private readonly ISupabaseRawStorage _rawStorage;
    private readonly string _bucketName;

    public SupabaseMedicalRecordStorage(ISupabaseRawStorage rawStorage, IConfiguration configuration)
    {
        _rawStorage = rawStorage;
        IConfigurationSection section = configuration.GetSection(ConfigurationKeys.Supabase.Section);
        string? configured = section[ConfigurationKeys.Supabase.MedicalRecordsBucketName];
        _bucketName = string.IsNullOrWhiteSpace(configured) ? DefaultBucketName : configured;
    }

    public async Task<string> StoreAsync(
        Guid teamId, Guid playerId, string fileName, Stream content, CancellationToken ct = default)
    {
        string extension = Path.GetExtension(fileName);
        string objectPath = $"{teamId}/{playerId}/{Guid.NewGuid()}{extension}";
        await _rawStorage.UploadRawAsync(objectPath, content, _bucketName);
        return objectPath;
    }

    public async Task<byte[]> DownloadAsync(string objectPath, CancellationToken ct = default)
        => await _rawStorage.DownloadRawAsync(objectPath, _bucketName);
}

// Application/Interfaces/Storage/IMedicalRecordStorage.cs — parameter rename only
Task<string> StoreAsync(Guid teamId, Guid playerId, string fileName, Stream content, CancellationToken ct = default);

// Domain/Entities/Models/PlayerTeamRegistration.cs — the single habilitación rule (Part 2)
/// Refs written before the private-bucket relocation lived under this prefix inside
/// public-images and no longer resolve. They are NOT real stored files.
public const string LegacyReferencePrefix = "medical-records/";

public static bool IsStoredReference(string? fileReference) =>
    !string.IsNullOrWhiteSpace(fileReference)
    && !fileReference.StartsWith(LegacyReferencePrefix, StringComparison.Ordinal);

[NotMapped]
public bool IsHabilitado =>
    MedicalRecordStatus == Enums.MedicalRecordStatus.Approved && IsStoredReference(MedicalRecordFileUrl);

// Domain/Entities/Models/Player.cs — transient carrier + new IsHabilitado body (no ternary at all → S3358 N/A)
[NotMapped]
public bool HasMedicalRecordFile { get; set; }

[NotMapped]
public bool IsHabilitado =>
    MedicalRecordStatus == Domain.Enums.MedicalRecordStatus.Approved && HasMedicalRecordFile;
```

`Player.HasMedicalRecordFile` defaults to `false`, so a `Player` loaded outside a
season roster reads `IsHabilitado == false` — exactly today's behaviour, where
the transient `MedicalRecordStatus` is null outside `AttachSeasonRostersAsync`.

### Part 2 — the five read sites, exactly

| # | Site | Change |
|---|---|---|
| 1 | `Domain/Entities/Models/Player.cs:91` | `=> MedicalRecordStatus == Approved && HasMedicalRecordFile;` (+ new `[NotMapped] bool HasMedicalRecordFile`) |
| 2 | `Application/Services/TeamService.cs:267` | one added line after the status assignment: `r.Player!.HasMedicalRecordFile = PlayerTeamRegistration.IsStoredReference(r.MedicalRecordFileUrl);` |
| 3 | `Application/DTOs/MedicalRecord/Response/MedicalRecordResponse.cs:49` | `IsHabilitado = registration.IsHabilitado,` |
| 4 | `Application/Services/PlayerStatisticService.cs:185` | see below |
| 5 | `Application/DTOs/Player/Response/PublicPlayerResponse.cs:55` | **no change** — AutoMapper name convention from site 1; no `ForMember` override exists |

Site 4, the match-sheet gate:

```csharp
bool sanctioned = !playersById.TryGetValue(playerId, out Player? player) || player.IsSanctioned;
if (sanctioned || !registration.IsHabilitado)
{
    throw new InvalidOperationException(ErrorMessages.MatchSheet.PlayerNotEligible(playerId));
}
```

The `PlayerNotEligible` path and message are **reused unchanged** —
`ErrorMessages.cs:327-330` already reads "missing approved registration or under
an active sanction", which stays accurate. No new error constant, no new status
code: `InvalidOperationException` maps to **409 Conflict** via
`GlobalExceptionHandler.cs:89`.

### Part 2 — the write guard

Placed at the **top of the approve branch** in `MedicalRecordService.ReviewAsync`
(`:54-67`), before any mutation, so a rejected approve leaves the row untouched:

```csharp
public async Task<MedicalRecordResponse> ReviewAsync(
    Guid playerId, Guid teamId, Guid tournamentId, bool approve, string? reason, string actor)
{
    PlayerTeamRegistration registration = await GetRegistrationAsync(playerId, teamId, tournamentId);

    // A ficha can only be approved against a file that is actually stored.
    // Refs under the legacy medical-records/ prefix point into the old public
    // bucket and no longer resolve, so they do not count as stored.
    if (approve && !PlayerTeamRegistration.IsStoredReference(registration.MedicalRecordFileUrl))
    {
        throw new InvalidOperationException(ErrorMessages.MedicalRecord.NoStoredFile);
    }
    …unchanged…
}
```

Rejecting is deliberately still allowed with no file. New constant, matching the
Spanish style of `AlreadyApproved` (`ErrorMessages.cs:343-344`):

```csharp
public const string NoStoredFile =
    "No se puede aprobar la ficha médica: no hay un archivo cargado. Subí la ficha antes de aprobarla.";
```

`InvalidOperationException` is the type this service already uses for both of its
validation failures (`AlreadyApproved`, `RegistrationNotFound`) → **409**,
consistent with the existing documented `409 Conflict` on the controller. No
`Result` object exists in this codebase.

### Part 2 — existing tests to fix

`SeedRegistrationAsync` (`:250-266`) and `SeedFinishedMatchAsync` (`:268-290`)
never set `MedicalRecordFileUrl`.

| Test | Asserts today | Minimal change |
|---|---|---|
| `Approve_MakesHabilitado` (`:117-133`) | approves a file-less registration → `IsHabilitado` true | insert one `RecordUploadAsync(fx…, $"{fx.TeamId}/{fx.PlayerId}/{Guid.NewGuid()}.pdf", "ficha.pdf", "owner@club12")` before the `ReviewAsync`; all four assertions stand |
| `LoadTeamMatchSheetAsync_AfterApproval_Succeeds` (`:176-196`) | approves file-less, then loads the sheet → `Assert.Single(created)` | same one-line upload before `ReviewAsync`; needed for **both** the write guard and the site-4 read gate |
| `ApprovalInOneSeason_DoesNotHabilitateAnother` (`:200-244`) | season A approved file-less; season B not eligible | same one-line upload for **season A only**. Season B's registration (`:219-226`) stays file-less and Pending — its "not eligible" expectation is unchanged and now doubly guaranteed |
| **`RecordUpload_AfterApproval_IsRejected` (`:84-113`)** — *not in the proposal's list* | uploads ref `"medical-records/some/object/path.pdf"` then approves at `:97` | Under ADR #4 that literal is a **legacy** ref, so the approve now throws and the test breaks. Replace the two `medical-records/…` literals with a `{teamId}/{playerId}/…` shaped ref (`:96`, `:104`, `:111`) |

`RecordUpload_StoresReference_ButStaysPending` (`:53-70`) still passes (it never
approves) but its literal should be updated for consistency.

### Part 2 — frontend

`PlayerMedicalRecordDialog.tsx` already computes
`hasStoredFile = Boolean(record?.fileUrl ?? record?.fileName)` at `:73` — that
stays as-is (it drives the download affordance). `MedicalRecordResponse` already
exposes `fileUrl` (`MedicalRecordResponse.cs:26`), so **no DTO change is needed**.
Add a module-level mirror of the Domain discriminator and a second derived flag:

```ts
// src/modules/medicalRecord/... — mirrors Domain PlayerTeamRegistration.LegacyReferencePrefix
export const LEGACY_MEDICAL_RECORD_PREFIX = 'medical-records/';
export const isStoredMedicalRecordFile = (fileUrl?: string | null): boolean =>
  Boolean(fileUrl) && !fileUrl!.startsWith(LEGACY_MEDICAL_RECORD_PREFIX);
```

At `:311-318` the "Aprobar" button becomes `disabled={submitting || !canApprove}`
with `const canApprove = isStoredMedicalRecordFile(record?.fileUrl);`, wrapped in
a MUI `Tooltip` (voseo, and `<span>`-wrapped because MUI tooltips need a
non-disabled child):

> `Subí la ficha médica antes de aprobarla.`

"Rechazar" (`:319-326`) is **not** disabled — rejecting a file-less record stays
legal, matching the backend. This is UX only; the 409 guard remains the
authority. Rejected alternative: adding a server-computed `canApprove` bool to
`MedicalRecordResponse` — cleaner (no duplicated rule) but expands a public API
contract for a cosmetic gate.

## Sequence — upload

```
Admin UI       MedicalRecordController      IMedicalRecordStorage        ISupabaseRawStorage      Supabase
   │                     │                  (SupabaseMedicalRecord…)      (SupabaseHelper)
   ├ POST /api/medical-records (multipart: PlayerId, TeamId, TournamentId, File)
   │        [Authorize(AdminOrOwner)]
   │                     ├ IsValidPdfFile()  ── false ─▶ 400
   │                     ├ GetAsync(PlayerId, TeamId, TournamentId)
   │                     │      Status == Approved ─▶ 409 AlreadyApproved
   │                     ├ StoreAsync(request.TeamId, request.PlayerId, File.FileName, File.OpenReadStream())
   │                     │      ▲ TeamId already exists on UploadMedicalRecordRequest:23 — no lookup needed
   │                     │                     ├ key = "{teamId}/{playerId}/{guid}.pdf"
   │                     │                     ├ UploadRawAsync(key, stream, bucket: "medical-records")
   │                     │                     │                 ├ .From("medical-records").Upload(Upsert=true) ─▶ PRIVATE bucket
   │                     │◀──── objectPath ────┤
   │                     ├ RecordUploadAsync(…, fileReference: objectPath, …)
   │                     │      → FileUrl = objectPath; Status forced back to Pending
   │◀── 201 MedicalRecordResponse (Status=Pending, IsHabilitado=false) ─┤
```

`UploadMedicalRecordRequest` already carries `TeamId` (`:23`) and the controller
already passes it to `medicalRecordService.GetAsync` at `:61`. **No registration
lookup and no service change is required** to satisfy the `tournamentId → teamId`
switch — `MedicalRecordController.cs:69` changes from `request.TournamentId` to
`request.TeamId`, one token. `TournamentId` stays in the request and on the
registration; only the *object key* loses the season dimension.

## Sequence — download

```
Admin UI       MedicalRecordController      IMedicalRecordStorage        ISupabaseRawStorage      Supabase
   ├ GET /api/medical-records/download?playerId&teamId&tournamentId
   │        [Authorize(AdminOrOwner)]
   │                     ├ GetAsync(...) ── record?.FileUrl is null ─▶ 404 ProblemDetails
   │                     ├ DownloadAsync(record.FileUrl)
   │                     │                     ├ DownloadRawAsync(objectPath, bucket: "medical-records")
   │                     │                     │                 ├ .From("medical-records").Download(...)   [service-role key]
   │                     │◀──── byte[] ────────┤
   │◀── 200 File(content, "application/pdf", fileName) ──┤
```

No public URL and no signed URL is produced anywhere; the service-role key
bypasses RLS, which is why the bucket **must not** carry anon read policies.

## Part 3 — seed

```csharp
// Infrastructure/Persistance/DataSeeder.cs
public sealed class DataSeeder(
    ApplicationDBContext db,
    ILogger<DataSeeder> logger,
    SupabaseHelper supabaseHelper,
    IMedicalRecordStorage medicalRecordStorage)      // NEW
{
#pragma warning disable S1075 // Dev-only seed default path; overridden by Seed:MedicalRecordPath.
    public const string DefaultMedicalRecordPath = @"C:\Users\Franco\Downloads\ficha-medica-club12.pdf";
#pragma warning restore S1075

    private const int MedicalRecordSaveBatchSize = 50;

    public async Task SeedAsync(
        bool reset = false, string? logosPath = null,
        string? medicalRecordPath = null, bool forceMedicalRecords = false)   // NEW params, both defaulted
    {
        if (reset) { await ResetSeededDataAsync(); }
        else if (await db.Teams.AnyAsync())
        {
            if (forceMedicalRecords)   // standalone backfill: bypass the skip, run only this step
            {
                logger.LogInformation("Sample data already present — running the medical-records backfill only.");
                await SeedMedicalRecordsAsync(medicalRecordPath);
                return;
            }
            logger.LogInformation("Sample data already present — skipping data seeding.");
            return;
        }
        … unchanged through db.SaveChangesAsync() at :314 …
        await SeedMedicalRecordsAsync(medicalRecordPath);   // AFTER save — keys are store-generated (ADR #6)
        … unchanged logging …
    }
}
```

`IMedicalRecordStorage` is registered in `StartupExtensions.RegisterSingletons`
(`:361`) as a **singleton**; `DataSeeder` is **scoped** (`:79`). Scoped→singleton
is legal, and the seed call site (`:170`,
`scope.ServiceProvider.GetRequiredService<DataSeeder>()`) resolves it fine.
`DataSeeder` already takes the concrete `SupabaseHelper`, which is the same
singleton `ISupabaseRawStorage` resolves to (`:360`), so **no new startup network
initialization is introduced**.

```csharp
private async Task SeedMedicalRecordsAsync(string? medicalRecordPath)
{
    string path = string.IsNullOrWhiteSpace(medicalRecordPath) ? DefaultMedicalRecordPath : medicalRecordPath;

    byte[] pdf;
    try
    {
        if (!File.Exists(path))
        {
            logger.LogWarning("Seed medical-record file '{Path}' not found — skipping medical-record seeding.", path);
            return;
        }
        pdf = await File.ReadAllBytesAsync(path);          // buffered ONCE (ADR #7)
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Could not read seed medical record from '{Path}' — skipping.", path);
        return;
    }

    string fileName = Path.GetFileName(path);

    // Superset filter, EF-translatable (StartsWith on a constant → LIKE 'medical-records/%').
    List<PlayerTeamRegistration> candidates = await db.PlayerTeamRegistrations
        .Where(r => r.MedicalRecordStatus == MedicalRecordStatus.Approved
            && (r.MedicalRecordFileUrl == null
                || r.MedicalRecordFileUrl == ""
                || r.MedicalRecordFileUrl.StartsWith(PlayerTeamRegistration.LegacyReferencePrefix)))
        .ToListAsync();

    int uploaded = 0, failed = 0, pending = 0;
    foreach (PlayerTeamRegistration registration in candidates)
    {
        // Authoritative decision — same predicate as the read sites and the write guard.
        if (PlayerTeamRegistration.IsStoredReference(registration.MedicalRecordFileUrl)) { continue; }

        try
        {
            using MemoryStream content = new(pdf, writable: false);   // FRESH stream per upload (ADR #7)
            string objectPath = await medicalRecordStorage.StoreAsync(
                registration.TeamId, registration.PlayerId, fileName, content);

            registration.MedicalRecordFileUrl = objectPath;
            registration.MedicalRecordFileName = fileName;
            uploaded++;
            pending++;
        }
        catch (Exception ex)
        {
            failed++;
            logger.LogWarning(ex,
                "Failed to upload the seed medical record for player {PlayerId} / team {TeamId} — leaving it without a file.",
                registration.PlayerId, registration.TeamId);
        }

        if (pending >= MedicalRecordSaveBatchSize) { await db.SaveChangesAsync(); pending = 0; }
    }

    if (pending > 0) { await db.SaveChangesAsync(); }

    logger.LogInformation(
        "Medical-record seed: {Uploaded} uploaded, {Failed} failed, {Total} candidates, from '{Path}'.",
        uploaded, failed, candidates.Count, path);
}
```

**Idempotency / resumability / failure tolerance — mechanics**

- *Idempotent*: after a successful run every touched row holds a
  `{teamId}/…` ref, which `IsStoredReference` accepts, so the DB query no longer
  returns it. A second run selects 0 candidates and performs 0 uploads.
- *Resumable*: progress is flushed every 50 rows, so an interruption at row N
  loses at most 49 refs. The next run re-selects exactly the rows still lacking a
  real ref. Uploads that succeeded but whose ref was never committed leave an
  orphan object in the bucket (harmless, private, overwritten conceptually by the
  next GUID) — the correctness invariant is the *ref*, never the object.
- *Failure tolerant*: per-row `try/catch` → warn and continue. A missing or
  unreadable PDF warns and skips the whole step. The step can never fail the
  seed, exactly like `UploadTeamLogosAsync` (`:403-415`).
- *No cancellation token* is threaded: the seed is a startup one-shot, matching
  the surrounding methods.

**Config wiring**

```csharp
// API/Utils/ConfigurationKeys.cs — inside the existing Seed class (:44-61)
public const string MedicalRecordPath = "Seed:MedicalRecordPath";
public const string MedicalRecords = "Seed:MedicalRecords";

// API/Utils/StartupExtensions.cs:166-172
if (configuration.GetValue<bool>(ConfigurationKeys.Seed.Enabled))
{
    bool reset = configuration.GetValue<bool>(ConfigurationKeys.Seed.Reset);
    string? logosPath = configuration[ConfigurationKeys.Seed.LogosPath];
    string? medicalRecordPath = configuration[ConfigurationKeys.Seed.MedicalRecordPath];
    bool medicalRecords = configuration.GetValue<bool>(ConfigurationKeys.Seed.MedicalRecords);
    DataSeeder dataSeeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await dataSeeder.SeedAsync(reset, logosPath, medicalRecordPath, medicalRecords);
}
```

`Seed:Enabled` still gates everything. `Seed:MedicalRecords=true` is a **bypass**
of the skip-if-teams-exist guard (`DataSeeder.cs:173-177`), not an extra
precondition: during a normal `Seed:Reset` reseed the step runs with the flag
unset.

**`SampleTournamentBuilder.cs`** — delete `SampleMedicalRecordFileUrl` (`:47`)
and change `:361` to `MedicalRecordFileUrl = null`.
`SampleMedicalRecordFileName` and `SampleMedicalRecordReviewedAt` **stay** (the
seed overwrites the name, and the reviewed-at timestamp is still meaningful);
the comment at `:342-346` is rewritten to say the ref is filled by
`SeedMedicalRecordsAsync`.

## File Changes

| File | Action | Part | Description |
|---|---|---|---|
| `Application/Utils/Helper/SupabaseHelper/ISupabaseRawStorage.cs` | Modify | 1 | `string? bucket = null` on 4 methods + docs |
| `Infrastructure/Storage/SupabaseHelper.cs:115-192` | Modify | 1 | `bucket ?? _bucketName` in 4 methods |
| `Infrastructure/Storage/SupabaseMedicalRecordStorage.cs` | Modify | 1 | ctor `IConfiguration`, `DefaultBucketName`, new key, bucket on both calls |
| `Application/Interfaces/Storage/IMedicalRecordStorage.cs` | Modify | 1 | `tournamentId` → `teamId` + doc |
| `Application/Utils/Constants/Configuration/ConfigurationKeys.cs:53-59` | Modify | 1 | `MedicalRecordsBucketName` |
| `API/Controllers/MedicalRecordController.cs:69` | Modify | 1 | `request.TournamentId` → `request.TeamId` |
| `API/appsettings.Franco.json` (+ any env-specific file with a `SupaBase` section) | Modify | 1 | `"MedicalRecordsBucketName": "medical-records"` (optional — code defaults) |
| `API.Tests/Backup/Fakes/FakeSupabaseRawStorage.cs` | Modify | 1 | 4 signatures + record the bucket per call |
| `API.Tests/MedicalRecordDownloadTests.cs:39-50,137-164` | Modify | 1 | `InMemoryRawStorage`/`FakeStorage` signatures; invert the `:49` prefix assertion to `{teamId}/` |
| `API.Tests/MedicalRecordStorageTests.cs` | Create | 1 | Key shape, bucket routing, default-bucket fallback, backup regression |
| `Domain/Entities/Models/PlayerTeamRegistration.cs` | Modify | 2 | `LegacyReferencePrefix`, `IsStoredReference`, `IsHabilitado` |
| `Domain/Entities/Models/Player.cs:82-91` | Modify | 2 | `HasMedicalRecordFile`; `IsHabilitado` body |
| `Application/Services/TeamService.cs:267` | Modify | 2 | populate `HasMedicalRecordFile` |
| `Application/DTOs/MedicalRecord/Response/MedicalRecordResponse.cs:49` | Modify | 2 | delegate to `registration.IsHabilitado` |
| `Application/Services/PlayerStatisticService.cs:185` | Modify | 2 | `!registration.IsHabilitado` |
| `Application/Services/MedicalRecordService.cs:54-67` | Modify | 2 | approve guard |
| `Application/Utils/Constants/ErrorMessages.cs:333-352` | Modify | 2 | `MedicalRecord.NoStoredFile` |
| `API.Tests/MedicalRecordEligibilityTests.cs` | Modify | 2 | fix 4 tests + new guard/read-rule tests |
| `Club12-WebClient/src/modules/medicalRecord/...` | Modify | 2 | `LEGACY_MEDICAL_RECORD_PREFIX`, `isStoredMedicalRecordFile` |
| `Club12-WebClient/src/views/medicalRecord/PlayerMedicalRecordDialog.tsx:311-318` | Modify | 2 | disable "Aprobar" + Spanish tooltip |
| `Club12-WebClient/src/views/medicalRecord/PlayerMedicalRecordDialog.test.tsx` | Create | 2 | Disabled/enabled states |
| `Infrastructure/Persistance/DataSeeder.cs` | Modify | 3 | ctor, const, `SeedMedicalRecordsAsync`, guard bypass, `SeedAsync` params |
| `Infrastructure/Persistance/SampleTournamentBuilder.cs:47,361` | Modify | 3 | drop the fake ref |
| `API/Utils/ConfigurationKeys.cs:44-61` | Modify | 3 | `MedicalRecordPath`, `MedicalRecords` |
| `API/Utils/StartupExtensions.cs:166-172` | Modify | 3 | read + pass both keys |
| `API.Tests/MedicalRecordSeedTests.cs` | Create | 3 | Skip/upload decision logic against a fake storage |

`Application/Services/RosterCopyService.cs` is untouched (HU-59). No EF migration:
`IsHabilitado`, `IsStoredReference` and `HasMedicalRecordFile` are `[NotMapped]`
or static.

## Testing Strategy (Strict TDD — RED first)

| Layer | What to test | Approach |
|---|---|---|
| Unit (P1) | `StoreAsync` key is `{teamId}/{playerId}/{guid}.pdf`, no `medical-records/` prefix; `teamId` is the first segment | `SupabaseMedicalRecordStorage` + `FakeSupabaseRawStorage`, in-memory `IConfiguration` |
| Unit (P1) | Upload and download both target the configured medical bucket; the fallback is `"medical-records"` when the key is absent | Fake records `bucket` per call |
| Unit (P1) | `SupabaseBackupStorage` still passes `bucket: null` (default bucket) — regression | Existing backup tests + one new bucket assertion |
| Unit (P2) | `PlayerTeamRegistration.IsStoredReference`: null / "" / whitespace / `medical-records/x` → false; `{guid}/{guid}/x.pdf` → true | Pure xUnit, no fixture |
| Unit (P2) | `IsHabilitado` truth table on registration **and** `Player` (Approved+file, Approved+legacy, Approved+null, Pending+file) | Pure xUnit |
| Unit (P2) | `MedicalRecordResponse.FromRegistration` — Approved + legacy ref → `IsHabilitado false` | Pure xUnit |
| Integration (P2) | Approve with no file → `InvalidOperationException`, message contains "no hay un archivo cargado"; row stays Pending | `MedicalRecordEligibilityTests` + SQLite host |
| Integration (P2) | Approve with a legacy `medical-records/` ref → rejected | Same |
| Integration (P2) | **Reject** with no file → still allowed | Same |
| Integration (P2) | Match sheet with an Approved + null-ref registration → `PlayerNotEligible` | Same fixture as `LoadTeamMatchSheetAsync_NotApprovedPlayer_IsRejected` |
| Integration (P2) | Roster surfacing: Approved+legacy player reads `isHabilitado false` | `TeamService.AttachSeasonRostersAsync` path |
| Unit (P3) | Skip-vs-upload: null → upload, `medical-records/…` → upload, `{teamId}/…` → skip; second run uploads 0 | `DataSeeder` against SQLite + a fake `IMedicalRecordStorage` |
| Unit (P3) | A throwing fake storage does not fail the seed and leaves the ref null | Same |
| Unit (P3) | Missing PDF path → warn + skip, 0 uploads | Same |
| Unit (P3) | The 320 seeded registrations carry `MedicalRecordFileUrl == null` after `SampleTournamentBuilder.Build` | Extend the builder tests |
| Frontend (P2) | "Aprobar" disabled + tooltip when `fileUrl` is null or legacy; enabled for a `{teamId}/…` ref; "Rechazar" never disabled | Vitest + Testing Library |
| Manual (P3) | Real bucket behaviour — see below | Dev DB + Supabase dashboard |

**Verification gap.** `CustomWebApplicationFactory` replaces only the two
DbContexts, so `SupabaseHelper` and every real bucket call are unreachable from
the test host (pre-existing, `SupabaseDependentControllerNotFoundTests`). Every
automated test above stops at the `ISupabaseRawStorage` / `IMedicalRecordStorage`
seam.

### Manual dev-DB verification procedure (Part 3)

```powershell
# 0. Baseline, BEFORE deploying Part 2. Expect the current (wrong) answer: 280 habilitado.
psql $env:CLUB12_DEV -c @"
SELECT count(*) FROM ""Club12"".""PlayerTeamRegistrations""
WHERE ""MedicalRecordStatus"" = 'Approved';
"@
# Note one Approved player's id + team id; hit GET /api/players/{slug} → isHabilitado today.

# 1. Deploy Parts 1+2, do NOT seed yet.
#    Same player → isHabilitado must now be FALSE (legacy ref rejected). This is the
#    accepted, documented window.

# 2. Deploy Part 3 and run the standalone backfill against the already-seeded DB.
dotnet run --project Club12-Backend/API `
  --Seed:Enabled=true --Seed:MedicalRecords=true `
  --Seed:MedicalRecordPath="C:\Users\Franco\Downloads\ficha-medica-club12.pdf"
```

Assertions after step 2 — all must hold:

1. `SELECT count(*) FROM "Club12"."PlayerTeamRegistrations" WHERE "MedicalRecordStatus"='Approved' AND "MedicalRecordFileUrl" LIKE 'medical-records/%';` → **0** (no legacy refs left).
2. `SELECT count(*) FROM "Club12"."PlayerTeamRegistrations" WHERE "MedicalRecordStatus"='Approved' AND "MedicalRecordFileUrl" IS NULL;` → **0**.
3. `SELECT count(*) FROM "Club12"."PlayerTeamRegistrations" WHERE "MedicalRecordStatus"='Approved' AND "MedicalRecordFileUrl" ~ '^[0-9a-f-]{36}/[0-9a-f-]{36}/';` → equals the count from step 0. **40 teams × 7 approved of 8 players = 280** on a stock reseed (not ~320 — 320 is the *total* player count; the 8th player of each team stays Pending). Assert equality with step 0 rather than a literal.
4. Supabase dashboard → bucket `medical-records`: object count equals assertion 3; the bucket is still **Private**; spot-check an object path is `{teamId}/{playerId}/{guid}.pdf`.
5. Supabase dashboard → bucket `public-images`: object count is **unchanged** from step 0 (no medical PDF leaked into the public bucket).
6. Re-run the exact step-2 command. Log reads `Medical-record seed: 0 uploaded, 0 failed, 0 candidates` and the `public-images`/`medical-records` object counts are unchanged → **no-op proven**.
7. The player noted in step 0 → `GET /api/players/{slug}` now reports `isHabilitado: true`; the panel roster badge shows habilitado; `GET /api/medical-records/download?...` streams the real PDF (not a 404).
8. Upload a fresh ficha through the UI for the 8th (Pending) player of any team → the new object lands under `medical-records/{teamId}/{playerId}/`, `public-images` is unchanged, and "Aprobar" is enabled.
9. Rename/point `Seed:MedicalRecordPath` at a non-existent file and re-run → one warning, seed completes, DB unchanged.

## Threat Matrix

| Boundary | Applicable? | Expected safe behaviour | RED test |
|---|---|---|---|
| Executable-file classification | **Applicable** | `IsValidPdfFile()` gate at `MedicalRecordController.cs:53` is unchanged and still rejects non-PDFs with 400 before storage is touched. The seed reads only the operator-configured path and never inspects user input | Existing 400 test; new "seed skips a missing path" test |
| Path traversal into storage | **Applicable** | The object key is entirely server-generated (`{teamId}/{playerId}/{Guid.NewGuid()}{ext}`) from route/form **Guids**; only the extension derives from the client file name, and `Path.GetExtension` cannot emit `/` or `..`. Download takes the **persisted** ref, never a client-supplied path | Key-shape unit test asserting the two leading segments parse as the expected Guids |
| Access control / data exposure | **Applicable** | Both endpoints keep `[Authorize(Roles = AdminOrOwner)]`; the bucket is private and served only through the service-role streaming path. `Player` carries a **bool**, never the object path, so the `[AllowAnonymous]` player endpoints cannot leak a storage reference (ADR #5) | Existing `AuthorizationGatingTests`; a `PublicPlayerResponse` shape test asserting no file-reference field |
| Routing | N/A | No route, constraint, or verb changes | — |
| Shell / subprocess | N/A | No process spawned; the seed only reads a file and calls HTTPS | — |
| VCS/PR automation | N/A | None added | — |
| Process integration | N/A | Single in-process ASP.NET path plus the startup seed | — |

## Migration / Rollout

No EF migration. Deployment order is **P1 → P2 → P3** and is load-bearing: with
P2 live before P1, new uploads would still write `medical-records/…` keys that
the new rule classifies as legacy, so freshly uploaded fichas could not be
approved.

- **P1** — deploy; the private bucket already exists. Existing `public-images`
  medical objects are abandoned by decision (go-forward only).
- **P2** — deploy; ~280 seeded players read not-habilitado until P3 runs. This is
  the accepted, documented window (proposal Risks).
- **P3** — deploy, then run the `Seed:MedicalRecords=true` backfill immediately.
- **Rollback** — per the proposal. The sharp edge is unchanged: reverting P3
  without reseeding leaves Approved rows with a null ref.
- **Manual per-environment prerequisite** — the `medical-records` bucket must
  exist and be **Private** with no anon read policy. The client never creates
  buckets; a missing bucket fails every upload and download.

## Review Budget

Estimated **authored** changed lines (`additions + deletions`):

| Part | Backend src | BE tests | FE src + tests | Authored total |
|---|---|---|---|---|
| 1 | ~100 | ~170 | — | **~270** |
| 2 | ~90 | ~180 | ~95 | **~365** |
| 3 | ~180 | ~110 | — | **~290** |
| | | | | **~925** |

Against the 400-line default budget every part is Medium/High on its own, and
the total roughly doubles the 800-line budget named for this change.

- `Chained PRs recommended: Yes`
- `Decision needed before apply: Yes`
- `400-line budget risk: High`

Chain shape: tracker PR → `develop` carrying Part 1 (self-contained: relocation
+ signature ripple, no behaviour change visible to users). PR #2 targets PR #1's
branch with Part 2 (backend rule + FE gate). PR #3 targets PR #2's branch with
Part 3 (seed). Only the tracker merges to `develop`.

## Open Questions

- [x] **Which registrations does the seed treat as "should be habilitado"?**
      Resolved: `MedicalRecordStatus == Approved` (still written by
      `SampleTournamentBuilder:358-360`). The `p < 7` rule is not re-derived.
- [x] **`Seed:MedicalRecords` semantics** — resolved as (b), a bypass. ADR #8.
- [x] **Frontend "Aprobar" disable** — ships in this change, in Part 2's PR.
- [x] **Naive vs prefix-aware file check** — resolved as prefix-aware. ADR #4.
- [ ] `appsettings.Franco.json` is the only file carrying a `SupaBase` section in
      this repo; confirm whether any deployed environment supplies the section by
      env var instead, in which case `SupaBase__MedicalRecordsBucketName` should
      be documented alongside it. Non-blocking — the code default covers it.
