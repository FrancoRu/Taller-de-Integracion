using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Backup;

/// <summary>
/// Thin abstraction over external process execution (e.g. pg_dump).
/// Exists so business logic (IDatabaseBackupService
/// implementations) never calls System.Diagnostics.Process.Start
/// directly and can be unit-tested with a fake runner instead of a real
/// binary.
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// Runs <paramref name="fileName"/> with the given <paramref name="args"/>.
    /// Arguments are passed as a vector (never concatenated into a shell
    /// string), so no argument can be reinterpreted as shell syntax by the
    /// child process. <paramref name="environmentVariables"/> lets callers
    /// pass sensitive values (e.g. a database password via the
    /// PGPASSWORD convention) without exposing them in the argument
    /// vector, where they would otherwise be visible via process listings.
    /// </summary>
    Task<ProcessResult> RunAsync(
        string fileName,
        IReadOnlyList<string> args,
        IReadOnlyDictionary<string, string>? environmentVariables = null,
        CancellationToken ct = default);
}
