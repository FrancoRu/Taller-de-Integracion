using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Backup;

/// <summary>
/// Restores the database from a local plain-SQL dump file.
/// </summary>
public interface IDatabaseRestoreService
{
    /// <summary>
    /// Restores from the plain-SQL dump at dumpFilePath.
    /// </summary>
    Task RestoreAsync(string dumpFilePath, CancellationToken ct = default);
}
