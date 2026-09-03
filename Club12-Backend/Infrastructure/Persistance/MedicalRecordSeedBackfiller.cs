using Application.Interfaces.Storage;

using Domain.Entities.Models;
using Domain.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistance;

/// <summary>
/// Backfills a real, stored medical-record file for every seeded
/// <c>Approved</c> registration that doesn't have one yet
/// (medical-records-storage-eligibility, Part 3) — used by both
/// <see cref="DataSeeder"/> (the startup, config-driven seed) and
/// <see cref="DataMaintenanceService"/> (the admin-triggered
/// <c>/api/data-maintenance/seed</c> reset), so neither seed path can leave a
/// league of Approved-but-fileless (therefore not-habilitado) players. Split
/// out from <see cref="DataSeeder"/> so a caller that only needs this step
/// doesn't have to satisfy <c>SupabaseHelper</c>'s live-Supabase constructor
/// requirement, which this step never touches.
/// </summary>
public sealed class MedicalRecordSeedBackfiller(
    ApplicationDBContext db,
    ILogger<MedicalRecordSeedBackfiller> logger,
    IMedicalRecordStorage medicalRecordStorage)
{
    /// <summary>
    /// Default medical PDF read from when <c>Seed:MedicalRecordPath</c> is not
    /// configured. Missing file warns and skips the whole backfill step
    /// (medical-records-storage-eligibility, Part 3).
    /// </summary>
#pragma warning disable S1075 // Dev-only seed default path; overridden by the Seed:MedicalRecordPath config key.
    public const string DefaultMedicalRecordPath = @"C:\Users\Franco\Downloads\ficha-medica-club12.pdf";
#pragma warning restore S1075

    /// <summary>
    /// File name recorded for the generated fallback ficha médica (see
    /// <see cref="BuildPlaceholderMedicalRecordPdf"/>).
    /// </summary>
    private const string PlaceholderMedicalRecordFileName = "ficha-medica-ejemplo.pdf";

    // Flushes progress every N uploaded rows so an interruption loses at most
    // this many refs and the step stays resumable (medical-records-storage-eligibility, ADR #7).
    private const int MedicalRecordSaveBatchSize = 50;

    /// <summary>
    /// Uploads a real medical PDF (<paramref name="medicalRecordPath"/>, or
    /// <see cref="DefaultMedicalRecordPath"/> when unset) for every
    /// <c>Approved</c> registration whose file reference is null or a legacy
    /// <see cref="PlayerTeamRegistration.LegacyReferencePrefix"/> ref, so it
    /// stops reading as not-habilitado under Part 2's file-backed rule
    /// (medical-records-storage-eligibility, Part 3). Idempotent (a
    /// new-scheme ref is skipped), resumable (flushed every
    /// <see cref="MedicalRecordSaveBatchSize"/> rows), and failure-tolerant: a
    /// missing/unreadable PDF warns and skips the whole step, and a per-row
    /// upload failure warns and continues — this step can never fail the
    /// seed.
    /// </summary>
    /// <param name="medicalRecordPath">See <see cref="DefaultMedicalRecordPath"/>.</param>
    /// <param name="approveNonApproved">
    /// TEMPORARY, for the one-off live-data fix only (see
    /// <c>MedicalRecordController.BackfillMedicalRecords</c>): when true, also
    /// widens the candidate set to Pending/Rejected registrations and marks
    /// each Approved once it has a real file, instead of only touching rows
    /// the seeder already marked Approved. A dataset seeded before the
    /// Approved-by-default seed logic existed has every registration stuck
    /// Pending, so the normal (false) candidate set finds nothing to fix.
    /// </param>
    public async Task BackfillMedicalRecordsAsync(string? medicalRecordPath, bool approveNonApproved = false)
    {
        bool isConfigured = !string.IsNullOrWhiteSpace(medicalRecordPath);
        string path = isConfigured ? medicalRecordPath! : DefaultMedicalRecordPath;

        byte[] pdf;
        string fileName;
        try
        {
            if (File.Exists(path))
            {
                pdf = await File.ReadAllBytesAsync(path);
                fileName = Path.GetFileName(path);
            }
            else if (isConfigured)
            {
                // An explicitly configured path that is not there is a
                // misconfiguration (a typo, a file that moved) — warn and skip
                // rather than papering over it with a placeholder.
                logger.LogWarning(
                    "Seed medical-record file '{Path}' not found — skipping medical-record seeding.", path);
                return;
            }
            else
            {
                // Nothing configured and the machine-specific default is not
                // there — the normal case on any machine but the one the
                // default points at. Falling back to a generated PDF keeps the
                // seeded league coherent: without a REAL stored file every
                // Approved registration reads as NOT habilitado, while the same
                // players hold scorer/statistic rows for thousands of played
                // matches — exactly the combination PlayerStatisticService
                // rejects on a real match sheet (HU-57/HU-60).
                pdf = BuildPlaceholderMedicalRecordPdf();
                fileName = PlaceholderMedicalRecordFileName;
                logger.LogInformation(
                    "No Seed:MedicalRecordPath configured and '{Path}' is absent — seeding the built-in "
                    + "placeholder ficha médica so approved registrations end up habilitado.", path);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not read seed medical record from '{Path}' — skipping.", path);
            return;
        }

        // Superset filter, EF-translatable (StartsWith on a constant -> LIKE 'medical-records/%').
        // The per-row IsStoredReference check below is the authoritative
        // skip-vs-upload decision — the same predicate the read sites and the
        // approve-time write guard use, so the three can never drift.
        List<PlayerTeamRegistration> candidates = await db.PlayerTeamRegistrations
            .Where(r => (r.MedicalRecordStatus == MedicalRecordStatus.Approved || approveNonApproved)
                && (r.MedicalRecordFileUrl == null
                    || r.MedicalRecordFileUrl == ""
                    || r.MedicalRecordFileUrl.StartsWith(PlayerTeamRegistration.LegacyReferencePrefix)))
            .ToListAsync();

        int uploaded = 0;
        int failed = 0;
        int pending = 0;
        foreach (PlayerTeamRegistration registration in candidates)
        {
            if (PlayerTeamRegistration.IsStoredReference(registration.MedicalRecordFileUrl))
            {
                continue;
            }

            try
            {
                using MemoryStream content = new(pdf, writable: false);
                string objectPath = await medicalRecordStorage.StoreAsync(
                    registration.TeamId, registration.PlayerId, fileName, content);

                registration.MedicalRecordFileUrl = objectPath;
                registration.MedicalRecordFileName = fileName;
                if (approveNonApproved && registration.MedicalRecordStatus != MedicalRecordStatus.Approved)
                {
                    registration.MedicalRecordStatus = MedicalRecordStatus.Approved;
                    registration.MedicalRecordReviewReason = null;
                    registration.MedicalRecordReviewedAt = DateTime.UtcNow;
                }
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

            if (pending >= MedicalRecordSaveBatchSize)
            {
                await db.SaveChangesAsync();
                pending = 0;
            }
        }

        if (pending > 0)
        {
            await db.SaveChangesAsync();
        }

        logger.LogInformation(
            "Medical-record seed: {Uploaded} uploaded, {Failed} failed, {Total} candidates, from '{Path}'.",
            uploaded, failed, candidates.Count, path);
    }

    /// <summary>
    /// A real, valid one-page PDF built in memory (correct xref table and
    /// offsets, so it opens like any other file), used as the ficha médica when
    /// no path is configured and the default file is absent. Deterministic: the
    /// same bytes on every run.
    /// </summary>
    private static byte[] BuildPlaceholderMedicalRecordPdf()
    {
        const string content =
            "BT /F1 16 Tf 60 760 Td (Ficha medica de ejemplo - Liga Club 12) Tj ET";

        string[] bodies =
        [
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] "
                + "/Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {content.Length} >>\nstream\n{content}\nendstream",
        ];

        StringBuilder pdf = new("%PDF-1.4\n");
        List<int> offsets = [];

        for (int i = 0; i < bodies.Length; i++)
        {
            // Every character written here is ASCII, so the builder's length is
            // also the byte offset the xref table has to point at.
            offsets.Add(pdf.Length);
            pdf.Append(i + 1).Append(" 0 obj\n").Append(bodies[i]).Append("\nendobj\n");
        }

        int xrefOffset = pdf.Length;
        pdf.Append("xref\n0 ").Append(bodies.Length + 1).Append("\n")
            .Append("0000000000 65535 f \n");

        foreach (int offset in offsets)
        {
            pdf.Append(offset.ToString("D10", CultureInfo.InvariantCulture)).Append(" 00000 n \n");
        }

        pdf.Append("trailer\n<< /Size ").Append(bodies.Length + 1).Append(" /Root 1 0 R >>\n")
            .Append("startxref\n").Append(xrefOffset).Append("\n%%EOF\n");

        return Encoding.ASCII.GetBytes(pdf.ToString());
    }
}
