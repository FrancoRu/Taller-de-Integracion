using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces.Backup;

namespace Infrastructure.Backup;

/// <summary>
/// IProcessRunner adapter backed by Process.
/// Arguments are passed via ProcessStartInfo.ArgumentList —
/// never a concatenated shell command string — so no argument is
/// interpretable as shell syntax by the child process
/// (UseShellExecute = false, no shell is ever invoked). A
/// missing/unresolvable executable surfaces as Win32Exception
/// from the runtime when starting the process; this is caught and converted
/// into a failed ProcessResult (sentinel exit code -1) instead
/// of propagating, so callers always get a result to log/handle rather than
/// an unhandled exception.
/// </summary>
public sealed class ProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(
        string fileName,
        IReadOnlyList<string> args,
        IReadOnlyDictionary<string, string>? environmentVariables = null,
        CancellationToken ct = default)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (string arg in args)
            startInfo.ArgumentList.Add(arg);

        if (environmentVariables is not null)
        {
            foreach (KeyValuePair<string, string> kvp in environmentVariables)
                startInfo.Environment[kvp.Key] = kvp.Value;
        }

        using Process process = new() { StartInfo = startInfo };

        try
        {
            process.Start();
        }
        catch (Win32Exception ex)
        {
            return new ProcessResult(-1, string.Empty, $"Failed to start process '{fileName}': {ex.Message}");
        }

        Task<string> stdOutTask = process.StandardOutput.ReadToEndAsync(ct);
        Task<string> stdErrTask = process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);
        string stdOut = await stdOutTask;
        string stdErr = await stdErrTask;

        return new ProcessResult(process.ExitCode, stdOut, stdErr);
    }
}
