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

    /// <summary>
    /// When set, DeleteAsync throws this instead of succeeding —
    /// simulates the stored file already being missing out-of-band.
    /// </summary>
    public Exception? DeleteException { get; set; }

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
        {
            DeletedNames.Add(name);
        }

        if (DeleteException is not null)
        {
            throw DeleteException;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// When set, OpenReadAsync throws this instead of succeeding —
    /// simulates a missing/unreadable stored file.
    /// </summary>
    public Exception? OpenReadException { get; set; }

    public Stream ContentToOpen { get; set; } = new MemoryStream();

    public Task<Stream> OpenReadAsync(string name, CancellationToken ct = default)
    {
        if (OpenReadException is not null)
        {
            throw OpenReadException;
        }

        return Task.FromResult(ContentToOpen);
    }
}
