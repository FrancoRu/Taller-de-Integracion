namespace Application.Interfaces.Backup;

/// <summary>
/// Result of running an external process via IProcessRunner, where callers must not treat StdOut as valid output unless ExitCode is 0.
/// </summary>
public sealed record ProcessResult(int ExitCode, string StdOut, string StdErr);
