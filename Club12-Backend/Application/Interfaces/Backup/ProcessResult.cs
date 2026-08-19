namespace Application.Interfaces.Backup;

/// <summary>
/// Result of running an external process via IProcessRunner.
/// A non-zero ExitCode (including the sentinel value the
/// ProcessRunner adapter returns when the process could not even be
/// started, e.g. a missing binary) signals failure; callers must not treat
/// StdOut as valid output unless ExitCode is 0.
/// </summary>
public sealed record ProcessResult(int ExitCode, string StdOut, string StdErr);
