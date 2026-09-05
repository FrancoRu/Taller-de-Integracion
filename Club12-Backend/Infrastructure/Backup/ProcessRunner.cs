using Application.Interfaces.Backup;

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Backup;

/// <summary>
/// IProcessRunner adapter backed by Process that passes arguments via ArgumentList instead of a concatenated shell command string.
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
        {
            startInfo.ArgumentList.Add(arg);
        }

        if (environmentVariables is not null)
        {
            foreach (KeyValuePair<string, string> kvp in environmentVariables)
            {
                startInfo.Environment[kvp.Key] = kvp.Value;
            }
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
