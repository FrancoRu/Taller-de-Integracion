using Domain.Enums;

using System;

namespace Domain.Entities.Models;

/// <summary>
/// Links a Player to a Team for exactly one season (Tournament). This is the
/// source of truth for roster membership: unlike <see cref="Player.TeamId"/>
/// (a denormalized "current team" convenience pointer, always reflecting the
/// player's latest registration), a registration row never changes meaning
/// after it is written. When a Team is reused across seasons by repointing
/// its own <see cref="Team.TournamentId"/> (see TeamService.RegisterTeamsToTournamentAsync),
/// existing registrations keep pointing at the season they were created for,
/// so a player's historical roster membership is never silently carried over
/// to a new season. A player may have at most one registration per
/// Tournament (enforced by a unique index on PlayerId+TournamentId).
/// </summary>
public class PlayerTeamRegistration : EntityBase
{
    public required Guid PlayerId { get; set; }
    public Player? Player { get; set; }

    public required Guid TeamId { get; set; }
    public Team? Team { get; set; }

    /// <summary>
    /// The season this registration belongs to. Captured at registration
    /// time from the Team's TournamentId at that moment — it is NOT a live
    /// reference to Team.TournamentId, so reassigning the Team to a new
    /// tournament later does not retroactively move this registration.
    /// </summary>
    public required Guid TournamentId { get; set; }
    public Tournament? Tournament { get; set; }

    /// <summary>
    /// The player's jersey number (dorsal) for THIS season registration
    /// (HU-54). Lives here — on the season-scoped roster membership — rather
    /// than on <see cref="Player"/> because a dorsal is only meaningful within
    /// a specific team + tournament, and the same player can wear different
    /// numbers across teams/seasons. Unique within the same team + tournament
    /// (enforced by a filtered unique index and validated on registration).
    /// Null when no number has been assigned yet.
    /// </summary>
    public int? JerseyNumber { get; set; }

    /// <summary>
    /// Medical-record / eligibility status for THIS season registration
    /// (HU-57). Defaults to <see cref="MedicalRecordStatus.Pending"/> so a new
    /// registration starts un-habilitado and never inherits a previous
    /// season's approval (HU-59). Only <see cref="MedicalRecordStatus.Approved"/>
    /// makes the player habilitado for this team+tournament.
    /// </summary>
    public MedicalRecordStatus MedicalRecordStatus { get; set; } = MedicalRecordStatus.Pending;

    /// <summary>
    /// Storage reference (object path in the medical-records bucket/prefix) of
    /// the uploaded medical-record file (PDF) for this season (HU-55). Null
    /// until a file is uploaded. Uploading alone does NOT habilitate — the
    /// record still has to be Approved (HU-57).
    /// </summary>
    public string? MedicalRecordFileUrl { get; set; }

    /// <summary>
    /// Original file name of the uploaded medical-record file, kept for display.
    /// </summary>
    public string? MedicalRecordFileName { get; set; }

    /// <summary>
    /// Optional reason recorded when the medical record is rejected (HU-58).
    /// </summary>
    public string? MedicalRecordReviewReason { get; set; }

    /// <summary>
    /// When the medical record was last approved or rejected (HU-58). Null
    /// while still Pending.
    /// </summary>
    public DateTime? MedicalRecordReviewedAt { get; set; }
}
