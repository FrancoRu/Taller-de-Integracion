using Domain.Entities.Models;

using System;

namespace Application.DTOs.Backup.Response;

/// <summary>
/// Response projection of a catalogued BackupRecord, with Origin serialized as its enum name.
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
