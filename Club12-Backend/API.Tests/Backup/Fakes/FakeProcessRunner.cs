using Application.Interfaces.Backup;

namespace API.Tests.Backup.Fakes;

/// <summary>
/// Test double for IProcessRunner. Records exactly what it was
/// invoked with (file name, argument vector, environment variables) so tests
/// can assert on those without a real subprocess, and returns a
/// pre-configured ProcessResult.
/// </summary>
public sealed class FakeProcessRunner : IProcessRunner
{
    public string? CapturedFileName { get; private set; }
    public IReadOnlyList<string>? CapturedArgs { get; private set; }
    public IReadOnlyDictionary<string, string>? CapturedEnvironmentVariables { get; private set; }
    public int CallCount { get; private set; }

    public ProcessResult ResultToReturn { get; set; } = new(0, string.Empty, string.Empty);

    public Task<ProcessResult> RunAsync(
        string fileName,
        IReadOnlyList<string> args,
        IReadOnlyDictionary<string, string>? environmentVariables = null,
        CancellationToken ct = default)
    {
        CallCount++;
        CapturedFileName = fileName;
        CapturedArgs = args;
        CapturedEnvironmentVariables = environmentVariables;

        return Task.FromResult(ResultToReturn);
    }
}
