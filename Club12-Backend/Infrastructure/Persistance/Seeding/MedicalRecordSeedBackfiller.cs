using Application.Interfaces.Storage;

using Domain.Constants;
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
///
/// Also approves any seeder-created registration still stuck
/// <c>Pending</c>/<c>Rejected</c> (identified by
/// <c>CreatedBy == AuditConstants.SystemUser</c> — never a real admin's
/// registration) once it gets a real file. This is what makes a database
/// seeded before the Approved-by-default seed logic existed (or restored
/// from a backup/migration that predates it) self-heal on the next startup
/// instead of leaving every seeded player stuck not-habilitado forever
/// (Épica 24, ítem 10 of historias-de-usuario.md).
/// </summary>
public sealed class MedicalRecordSeedBackfiller(
    ApplicationDBContext db,
    ILogger<MedicalRecordSeedBackfiller> logger,
    IMedicalRecordStorage medicalRecordStorage)
{
    /// <summary>
    /// Embedded resource name of the generic ficha médica shipped inside the
    /// assembly (see <c>Persistance/Seeding/Assets/ficha-medica-generica.pdf</c>,
    /// wired via <c>Infrastructure.csproj</c>'s <c>EmbeddedResource</c> glob).
    /// Used when <c>Seed:MedicalRecordPath</c> is not configured, so the
    /// backfill works on any machine — including a deployed server — instead
    /// of only the one a hardcoded local path happens to point at.
    /// </summary>
    private const string EmbeddedMedicalRecordResourceName =
        "Infrastructure.Persistance.Seeding.Assets.ficha-medica-generica.pdf";

    /// <summary>
    /// File name recorded for the seeded ficha médica, whether it came from
    /// the embedded resource or the last-resort generated placeholder (see
    /// <see cref="BuildPlaceholderMedicalRecordPdf"/>).
    /// </summary>
    private const string PlaceholderMedicalRecordFileName = "ficha-medica-generica.pdf";

    // Flushes progress every N uploaded rows so an interruption loses at most
    // this many refs and the step stays resumable (medical-records-storage-eligibility, ADR #7).
    private const int MedicalRecordSaveBatchSize = 50;

    /// <summary>
    /// Uploads a real medical PDF (<paramref name="medicalRecordPath"/>, or
    /// the embedded generic ficha médica when unset — see
    /// <see cref="EmbeddedMedicalRecordResourceName"/>) for every
    /// <c>Approved</c> registration whose file reference is null or a legacy
    /// <see cref="PlayerTeamRegistration.LegacyReferencePrefix"/> ref, so it
    /// stops reading as not-habilitado under Part 2's file-backed rule
    /// (medical-records-storage-eligibility, Part 3). Idempotent (a
    /// new-scheme ref is skipped), resumable (flushed every
    /// <see cref="MedicalRecordSaveBatchSize"/> rows), and failure-tolerant: a
    /// missing/unreadable configured PDF warns and skips the whole step, and a
    /// per-row upload failure warns and continues — this step can never fail
    /// the seed.
    /// </summary>
    public async Task BackfillMedicalRecordsAsync(string? medicalRecordPath)
    {
        bool isConfigured = !string.IsNullOrWhiteSpace(medicalRecordPath);

        byte[] pdf;
        string fileName;
        if (isConfigured)
        {
            try
            {
                if (!File.Exists(medicalRecordPath))
                {
                    // An explicitly configured path that is not there is a
                    // misconfiguration (a typo, a file that moved) — warn and
                    // skip rather than papering over it with the generic one.
                    logger.LogWarning(
                        "Seed medical-record file '{Path}' not found — skipping medical-record seeding.",
                        medicalRecordPath);
                    return;
                }

                pdf = await File.ReadAllBytesAsync(medicalRecordPath!);
                fileName = Path.GetFileName(medicalRecordPath);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not read seed medical record from '{Path}' — skipping.", medicalRecordPath);
                return;
            }
        }
        else
        {
            // Nothing configured — the normal case, including on a deployed
            // server. Falling back to the generic ficha médica embedded in
            // the assembly keeps the seeded league coherent: without a REAL
            // stored file every Approved registration reads as NOT
            // habilitado, while the same players hold scorer/statistic rows
            // for thousands of played matches — exactly the combination
            // PlayerStatisticService rejects on a real match sheet
            // (HU-57/HU-60).
            pdf = LoadEmbeddedMedicalRecordPdf();
            fileName = PlaceholderMedicalRecordFileName;
            logger.LogInformation(
                "No Seed:MedicalRecordPath configured — seeding the built-in generic ficha médica so "
                + "approved registrations end up habilitado.");
        }

        // Superset filter, EF-translatable (StartsWith on a constant -> LIKE 'medical-records/%').
        // The per-row IsStoredReference check below is the authoritative
        // skip-vs-upload decision — the same predicate the read sites and the
        // approve-time write guard use, so the three can never drift.
        // The CreatedBy == SystemUser branch is what lets a seeder-created
        // Pending/Rejected row self-heal too (Épica 24, ítem 10) — safe
        // because that value is never written by a real admin action, only
        // by the seeder itself.
        List<PlayerTeamRegistration> candidates = await db.PlayerTeamRegistrations
            .Where(r => (r.MedicalRecordStatus == MedicalRecordStatus.Approved
                    || r.CreatedBy == AuditConstants.SystemUser)
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
                if (registration.MedicalRecordStatus != MedicalRecordStatus.Approved)
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
            uploaded, failed, candidates.Count, medicalRecordPath);
    }

    /// <summary>
    /// Reads the generic ficha médica embedded in the assembly (see
    /// <see cref="EmbeddedMedicalRecordResourceName"/>). This is the normal
    /// no-config fallback; <see cref="BuildPlaceholderMedicalRecordPdf"/> only
    /// backstops the (should-never-happen) case where the resource fails to
    /// load, so the seed still never fails on this step.
    /// </summary>
    private static byte[] LoadEmbeddedMedicalRecordPdf()
    {
        try
        {
            using Stream? stream = typeof(MedicalRecordSeedBackfiller).Assembly
                .GetManifestResourceStream(EmbeddedMedicalRecordResourceName);

            if (stream is null)
            {
                return BuildPlaceholderMedicalRecordPdf();
            }

            using MemoryStream buffer = new();
            stream.CopyTo(buffer);
            return buffer.ToArray();
        }
        catch
        {
            return BuildPlaceholderMedicalRecordPdf();
        }
    }

    /// <summary>
    /// A real, valid one-page PDF built in memory (correct xref table and
    /// offsets, so it opens like any other file). Last-resort fallback for
    /// <see cref="LoadEmbeddedMedicalRecordPdf"/>. Deterministic: the same
    /// bytes on every run.
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
