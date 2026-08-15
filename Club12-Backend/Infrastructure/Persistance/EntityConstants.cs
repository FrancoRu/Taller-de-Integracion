namespace Infrastructure.Persistance;

/// <summary>
/// Centralizes all magic strings used across EF Core configurations:
/// schema, table names, column type names, index names, and filter expressions.
/// </summary>
public static class EntityConstants
{
    // ── Schema ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Club12 database schema.
    /// </summary>
    public const string Schema = "Club12";

    // ── Shared column types ───────────────────────────────────────────────────

    /// <summary>
    /// SQL Server datetime2 column type.
    /// </summary>
    public const string DateTime2 = "datetime2";

    /// <summary>
    /// SQL Server decimal(18,4) column type.
    /// </summary>
    public const string Decimal18x4 = "decimal(18,4)";

    // ── Tables ────────────────────────────────────────────────────────────────

    public static class Tables
    {
        public const string BlogPost        = "BlogPosts";
        public const string Division        = "Divisions";
        public const string Match           = "Matches";
        public const string Player          = "Players";
        public const string PlayerSanction  = "PlayerSanctions";
        public const string PlayerStatistic = "PlayersStatistics";   // matches existing migration
        public const string Position        = "Positions";
        public const string Scorer          = "Scorers";
        public const string Stage           = "Stages";
        public const string StageTeamMatch  = "StageTeamMatches";
        public const string Staff           = "Staffs";
        public const string Team            = "Teams";
        public const string Tournament      = "Tournaments";
        public const string User            = "Users";
        public const string Venue           = "Venues";
    }

    // ── Indexes ───────────────────────────────────────────────────────────────

    public static class Indexes
    {
        public const string UniqueStageNameDivision = "CONSTRAINT_UNIQUE_STAGE_NAME_AND_DIVISIONID";
    }

    // ── Check constraints ─────────────────────────────────────────────────────

    public static class CheckConstraints
    {
        public const string TournamentDeadlineBeforeStart = "CK_Tournament_DeadlineBeforeStart";
    }

    // ── Index filter expressions ──────────────────────────────────────────────

    public static class Filters { }
}
