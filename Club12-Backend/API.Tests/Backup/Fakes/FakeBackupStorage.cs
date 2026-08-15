using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces.Backup;

namespace API.Tests.Backup.Fakes;

/// <summary>
/// Test double for IBackupStorage. Records call counts and
/// deleted names so hosted-service tests can assert on storage/retention
/// interaction without any real I/O.
/// </summary>
public sealed class FakeBackupStorage : IBackupStorage
{
    private int _storeCallCount;
    private int _listCallCount;
    private int _deleteCallCount;

    public int StoreCallCount => _storeCallCount;
    public int ListCallCount => _listCallCount;
    public int DeleteCallCount => _deleteCallCount;

    public IReadOnlyList<BackupFile> FilesToList { get; set; } = Array.Empty<BackupFile>();
    public List<string> DeletedNames { get; } = [];

    public Task StoreAsync(string name, Stream content, CancellationToken ct = default)
    {
        Interlocked.Increment(ref _storeCallCount);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<BackupFile>> ListAsync(CancellationToken ct = default)
    {
        Interlocked.Increment(ref _listCallCount);
        return Task.FromResult(FilesToList);
    }

    public Task DeleteAsync(string name, CancellationToken ct = default)
    {
        Interlocked.Increment(ref _deleteCallCount);
        lock (DeletedNames)
            DeletedNames.Add(name);
        return Task.CompletedTask;
    }
}
