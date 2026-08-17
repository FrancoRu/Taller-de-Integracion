/**
 * Real team pools reused from "Apertura Club 12 2026" / "Femenino Club12 La
 * Vuelta" to build the Clausura season — captured from the live dev DB
 * (GET /api/teams, GET /api/matches?stageId=...) rather than invented, per
 * the "reuse real seeded teams" requirement. Zone membership mirrors each
 * team's current Apertura zone, trimmed to 4 teams per zone below.
 *
 * Two real, independent constraints on the live backend forced that
 * trim (both confirmed by driving the actual wizard and reading the
 * resulting HTTP error bodies, not by reading the source in isolation):
 *
 * 1. `CreateTournamentRequest.MaxTeams` must be 4-32 ("Max teams must be
 *    between 4 and 32."). Apertura's own zones total 41 teams, but its
 *    stored `maxTeams` is 32 too — the extra teams were added to zones
 *    after creation, outside the wizard's own team-count check.
 * 2. `AssignTeamsToStageAsync` caps a Group-type stage at
 *    `MaxTeams.GROUP` teams — which was hardcoded to 4 regardless of
 *    zone size (see Club12-Backend/Application/Utils/Constants/Stage/
 *    MaxTeams.cs). That's a real bug: a "Group" stage is a whole zone's
 *    round-robin phase, not a fixed-size elimination bracket like
 *    QuarterFinal/SemiFinal, so it shouldn't share their fixed-capacity
 *    model. Fixed in source (now reuses TournamentFieldRange.MaxAllowedTeams,
 *    32) as part of this change, but the already-running dev backend
 *    process was intentionally NOT restarted to pick it up (task
 *    constraint: reuse the live instance, never touch the backend
 *    process) — so *this* E2E run still has to respect the old cap of 4,
 *    hence 4 teams per zone here rather than the originally-planned
 *    8/9/9/14.
 */
export const APERTURA_TOURNAMENT_NAME = 'Apertura Club 12 2026';
export const FEMENINO_TOURNAMENT_NAME = 'Femenino Club12 La Vuelta';

export const ZONE_A_TEAMS = ['LBY', 'EEP', 'LOS TANOS', 'WOLVES'];
export const ZONE_B_TEAMS = ['ESPAÑOL', 'BLACK MAMBA', 'MURGUITA', 'BALLERZ'];
export const ZONE_C_TEAMS = ['THOMPSON BT', 'PSG', 'QLSRN', 'LA SALSA'];
export const ZONE_D_TEAMS = ['ATON', 'INDIANA PANSERS', 'TONELEROS', 'TEAM PAN'];

/** One team per zone — a cross-division cup can reuse a team already in its home zone (see StageTeamAssignmentConsistencyTests). */
export const CROSS_CUP_TEAMS = ['LBY', 'ESPAÑOL', 'THOMPSON BT', 'ATON'];

export const FEMENINO_TEAMS = ['MALALAS', 'EEP FEM', 'EL EQUIPITO DEL AEC', 'UNA MAS YNJM'];

/**
 * ZONE_A-D above were re-verified live (GET /api/teams) to currently have
 * `tournamentId: null` — earlier runs of this same spec, driven while
 * pinning down the MaxTeams.GROUP bug documented above, registered and
 * then released them via the real register-teams/delete-tournament
 * endpoints (never raw SQL), which is why they're unassigned rather than
 * still tagged onto Apertura. EquiposStep only appends the "(<tournament>)"
 * suffix when a team has a current tournament (see EquiposStep.tsx), so
 * their chip label is just the plain team name.
 */
export const ZONE_TEAM_CHIP_LABELS = [
  ...ZONE_A_TEAMS,
  ...ZONE_B_TEAMS,
  ...ZONE_C_TEAMS,
  ...ZONE_D_TEAMS,
];

/** Builds the EquiposStep chip labels ("<team> (<current tournament>)") for a list of plain team names — for pools still genuinely tagged with a tournament (e.g. FEMENINO_TEAMS). */
export const withTournamentSuffix = (teamNames: string[], tournamentName: string): string[] =>
  teamNames.map(name => `${name} (${tournamentName})`);
