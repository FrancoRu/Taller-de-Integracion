using Domain.Entities.Models;

using System;

namespace Application.DTOs.Backup.Response;

/// <summary>
/// API/response projection of a catalogued BackupRecord.
/// Origin is serialized as its enum name ("Manual" /
/// "Job") rather than the entity's typed enum, matching the response
/// shape already used elsewhere for cross-boundary DTOs.
/// </summary>
public sealed record BackupRecordResponse(
    Guid Id, DateTime CreatedAt, long SizeBytes, string Origin, string StoragePath)
{
    public static BackupRecordResponse FromEntity(BackupRecord record)
    {
        return new BackupRecordResponse(
            record.Id, record.DateCreated, record.SizeBytes, record.Origin.ToString(), record.StoragePath);
    }
}
