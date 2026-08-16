namespace API.Utils;

/// <summary>
/// Centralized log message text used at application startup and shutdown.
/// </summary>
public static class LogMessages
{
    public const string StartingUp = "----- Starting up -----";
    public const string Started = "----- Started     -----";
    public const string TerminatedUnexpectedly = "Application terminated unexpectedly";

    public const string Banner = @"

  ####    ##       ##  ##   #####               ##      ####
 ##  ##   ##       ##  ##   ##  ##             ###     ##  ##
 ##       ##       ##  ##   #####               ##        ##
 ##       ##       ##  ##   ##  ##              ##       ##
 ##  ##   ##       ##  ##   ##  ##              ##      ##
  ####    ######   ######   #####             ######   ######

";
}
