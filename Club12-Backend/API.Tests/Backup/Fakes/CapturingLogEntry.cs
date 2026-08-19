using Microsoft.Extensions.Logging;

namespace API.Tests.Backup.Fakes;

/// <summary>
/// A single log call captured by CapturingLogger{T}.
/// </summary>
public sealed record CapturingLogEntry(LogLevel Level, string Message, Exception? Exception);
