using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Backup;

/// <summary>
/// Thin abstraction over external process execution.
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// Runs fileName with the given args, passed as a vector so no argument can be reinterpreted as shell syntax by the child process.
    /// </summary>
    Task<ProcessResult> RunAsync(
        string fileName,
        IReadOnlyList<string> args,
        IReadOnlyDictionary<string, string>? environmentVariables = null,
        CancellationToken ct = default);
}
