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
/// Backfills a real, stored medical record file for every seeded Approved registration that lacks one.
/// </summary>
public sealed class MedicalRecordSeedBackfiller(
    ApplicationDBContext db,
    ILogger<MedicalRecordSeedBackfiller> logger,
    IMedicalRecordStorage medicalRecordStorage)
{
    /// <summary>
    /// Embedded resource name of the generic ficha medica shipped inside the assembly.
    /// </summary>
    private const string EmbeddedMedicalRecordResourceName =
        "Infrastructure.Persistance.Seeding.Assets.ficha-medica-generica.pdf";

    /// <summary>
    /// File name recorded for the seeded ficha medica, whether it came from the embedded resource or the last-resort generated placeholder.
    /// </summary>
    private const string PlaceholderMedicalRecordFileName = "ficha-medica-generica.pdf";

    // Flushes progress every N uploaded rows so an interruption loses at most this many refs and the step stays resumable.
    private const int MedicalRecordSaveBatchSize = 50;

    /// <summary>
    /// Uploads a real medical PDF for every Approved registration whose file reference is missing or legacy.
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
                    // An explicitly configured path that is not there is a misconfiguration, so warn and skip rather than papering over it with the generic one.
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
            // Falling back to the generic ficha médica keeps the seeded league coherent, since without a real stored file every Approved registration reads as not habilitado while the same players hold scorer and statistic rows for thousands of played matches, the exact combination PlayerStatisticService rejects on a real match sheet.
            pdf = LoadEmbeddedMedicalRecordPdf();
            fileName = PlaceholderMedicalRecordFileName;
            logger.LogInformation(
                "No Seed:MedicalRecordPath configured — seeding the built-in generic ficha médica so "
                + "approved registrations end up habilitado.");
        }

        // Superset filter kept EF-translatable, since StartsWith on a constant becomes LIKE 'medical-records/%'.
        // The per-row IsStoredReference check below is the authoritative skip-vs-upload decision, the same predicate the read sites and the approve-time write guard use, so the three can never drift.
        // The CreatedBy == SystemUser branch lets a seeder-created Pending or Rejected row self-heal too, safe because that value is never written by a real admin action, only by the seeder itself.
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
    /// Reads the generic ficha medica embedded in the assembly, named by EmbeddedMedicalRecordResourceName.
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
    /// A real, valid one-page PDF built in memory, with a correct cross-reference table and offsets.
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
            // Every character written here is ASCII, so the builder's length is also the byte offset the xref table has to point at.
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
