using System.Threading.Tasks;
using Application.Interfaces.Backup;
using Infrastructure.Backup;
using Xunit;

namespace API.Tests.Backup;

/// <summary>
/// Exercises the real ProcessRunner adapter (not a fake) —
/// proves the missing-binary path degrades to a failed
/// ProcessResult instead of throwing an unhandled exception,
/// and that a real successful invocation is captured correctly.
/// </summary>
public class ProcessRunnerTests
{
    [Fact]
    public async Task RunAsync_MissingExecutable_ReturnsFailedResult_DoesNotThrow()
    {
        ProcessRunner runner = new();

        ProcessResult result = await runner.RunAsync(
            "club12-definitely-not-a-real-executable-name",
            args: []);

        Assert.NotEqual(0, result.ExitCode);
        Assert.False(string.IsNullOrEmpty(result.StdErr));
    }

    [Fact]
    public async Task RunAsync_SuccessfulProcess_CapturesExitCodeAndStdOut()
    {
        ProcessRunner runner = new();

        // "dotnet --version" is a safe, always-available executable in this
        // test environment (the test host itself runs via dotnet), exits 0,
        // and writes a version string to stdout.
        ProcessResult result = await runner.RunAsync("dotnet", args: ["--version"]);

        Assert.Equal(0, result.ExitCode);
        Assert.False(string.IsNullOrWhiteSpace(result.StdOut));
    }
}
