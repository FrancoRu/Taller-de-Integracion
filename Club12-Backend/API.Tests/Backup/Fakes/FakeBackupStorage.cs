using Application.Interfaces.Backup;

using System.Text;

namespace API.Tests.Backup.Fakes;

/// <summary>
/// Test double for IBackupStorage. Records call counts, stored names, and
/// deleted names so hosted-service and orchestration tests can assert on
/// storage/retention/restore interaction without any real I/O.
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
    public List<string> StoredNames { get; } = [];
    public List<string> DeletedNames { get; } = [];
    public List<string> RetrievedNames { get; } = [];

    /// <summary>Content returned by RetrieveAsync for any name.</summary>
    public string RetrieveContent { get; set; } = "-- restored dump --";

    /// <summary>When set, RetrieveAsync throws this instead of returning content.</summary>
    public Exception? RetrieveException { get; set; }

    public Task StoreAsync(string name, Stream content, CancellationToken ct = default)
    {
        Interlocked.Increment(ref _storeCallCount);
        lock (StoredNames)
        {
            StoredNames.Add(name);
        }

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

        return Task.CompletedTask;
    }

    public Task<Stream> RetrieveAsync(string name, CancellationToken ct = default)
    {
        if (RetrieveException is not null)
        {
            throw RetrieveException;
        }

        lock (RetrievedNames)
        {
            RetrievedNames.Add(name);
        }

        Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(RetrieveContent));
        return Task.FromResult(stream);
    }
}
