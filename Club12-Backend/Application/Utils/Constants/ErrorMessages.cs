namespace Application.Utils.Constants;

/// <summary>
/// Centralized user-facing and exception message text, so a message never
/// needs to be typed twice and a wording change happens in one place.
/// </summary>
public static class ErrorMessages
{
    public static class Media
    {
        public const string InvalidImageFile = "The photo file must be a valid JPEG/PNG image.";
    }

    public static class Auth
    {
        public const string EmailAlreadyExists = "A user with this email already exists.";
        public const string InvalidCredentials = "Invalid credentials.";
        public const string AccountDeactivated = "This account is deactivated.";
        public const string NoAccountForEmail = "No account found for that email.";
        public const string InvalidMagicLink = "Invalid magic-link.";
        public const string MagicLinkAlreadyUsed = "Magic-link is invalid or has already been used.";
        public const string InvalidPasswordResetRequest = "Invalid password reset request.";
        public const string InvalidRefreshToken = "Refresh token is invalid.";
        public const string RefreshTokenExpired = "Refresh token has expired. Please log in again.";
        public const string RoleClaimMissing = "Role claim is missing from the token.";
        public const string IdClaimMissing = "Id claim is missing from the token.";
        public const string AccessDenied = "Access denied.";

        public static string UserCreationFailed(string errors)
        {
            return $"User creation failed: {errors}";
        }

        public static string RoleNotAllowedToCreate(string callerRole, string targetRole)
        {
            return $"Role '{callerRole}' is not allowed to create users with role '{targetRole}'.";
        }
    }

    public static class User
    {
        public const string InsufficientPermissionsToListUsers = "Insufficient permissions to list users.";
        public const string CurrentPasswordRequired = "CurrentPassword is required when changing your own password.";
        public const string CannotChangeOwnActiveState = "You cannot change the active state of your own account.";
        public const string InsufficientPermissionsToDelete = "Insufficient permissions to delete this user.";
        public const string PasswordResetRestricted = "Only Admins and Owners (for their own subordinates) can reset passwords.";
        public const string CannotChangeOwnRole = "You cannot change your own role.";
        public const string InsufficientPermissionsToChangeRole = "Insufficient permissions to change this user's role.";

        public static string NotFound(string userId)
        {
            return $"User '{userId}' not found.";
        }

        public static string InvalidRole(object role)
        {
            return $"'{role}' is not a valid role.";
        }

        public static string RoleNotAllowedToAssign(string callerRole, string targetRole)
        {
            return $"Role '{callerRole}' is not allowed to assign role '{targetRole}'.";
        }
    }

    public static class Tournament
    {
        public static string NotFound(System.Guid tournamentId)
        {
            return $"There is no Tournament with id: {tournamentId}.";
        }

        public static string InvalidStatusTransition(
            Domain.Enums.TournamentStatus from, Domain.Enums.TournamentStatus to)
        {
            return $"Cannot change tournament status from '{from}' to '{to}'. " +
                "Allowed flow is Scheduled -> OpenForRegistration -> RegistrationClosed -> " +
                "Ongoing -> Finished; a tournament may be Canceled from any non-terminal state, " +
                "and Finished/Canceled are terminal.";
        }

        public static string StructuralEditNotAllowed(Domain.Enums.TournamentStatus status)
        {
            return $"Structural changes (divisions and team registrations) are only allowed while " +
                $"the tournament is OpenForRegistration; this tournament is '{status}'.";
        }

        public static string CategoryMismatch(
            Domain.Enums.TournamentCategory divisionCategory,
            Domain.Enums.TournamentCategory tournamentCategory)
        {
            return $"A '{divisionCategory}' division cannot belong to a '{tournamentCategory}' tournament " +
                "(HU-48): the feminine competition is a separate tournament and cannot be mixed with the " +
                "masculine one. Create the division under a tournament of the same category.";
        }

        public static string NotCompletable(string issueSummaries)
        {
            return "Cannot start the tournament because it is not completable in its current state (HU-109): " +
                $"{issueSummaries}. Resolve every issue before starting.";
        }

        public static string UnenrollNotAllowed(Domain.Enums.TournamentStatus status)
        {
            return "Teams can only be removed while the tournament is OpenForRegistration or " +
                $"RegistrationClosed; this tournament is '{status}'.";
        }
    }

    public static class Team
    {
        public static string NotFound(System.Guid teamId)
        {
            return $"There is no Team with id: {teamId}.";
        }

        public static string NotInTournament(System.Guid teamId)
        {
            return $"Team '{teamId}' is not currently registered to any tournament, so players cannot be registered to it.";
        }

        public static string AlreadyEnrolled(System.Guid teamId, System.Guid tournamentId)
        {
            return $"Team '{teamId}' is already enrolled in tournament '{tournamentId}'. A team can be enrolled only once per tournament.";
        }

        public static string NotEnrolled(System.Guid teamId, System.Guid tournamentId)
        {
            return $"Team '{teamId}' is not enrolled in tournament '{tournamentId}'.";
        }
    }

    public static class Roster
    {
        public static string PlayerAlreadyInAnotherTeam(System.Guid playerId, System.Guid tournamentId)
        {
            return $"Player '{playerId}' is already registered to another team in tournament '{tournamentId}'. A player cannot be registered to two teams in the same tournament.";
        }

        public static string RosterFull(System.Guid teamId, int maxPlayers)
        {
            return $"Team '{teamId}' already has the maximum of {maxPlayers} players for this tournament.";
        }

        public static string DuplicateJerseyNumber(int jerseyNumber, System.Guid teamId, System.Guid tournamentId)
        {
            return $"Jersey number {jerseyNumber} is already used by another player in team '{teamId}' for tournament '{tournamentId}'.";
        }
    }

    public static class Stage
    {
        public const string NotFoundGeneric = "Stage not found.";
        public const string DivisionNotFound = "Division not found.";
        public const string DivisionAlreadyHasStages = "Cannot process the current request because the current division already has some stage.";
        public const string GenerateMatchesBeforeSeeding = "Generate this stage's matches before seeding it.";
        public const string AlreadySeeded = "This stage has already been seeded.";
        public const string InvalidStageType = "Invalid stage type";
        public const string SeedMissingStandings = "Cannot seed: not every team assigned to this stage has a finished-group-stage position yet.";
        public const string GroupStageAlreadyExistsInDivision = "This division already has a Group stage. A division can only have one Group stage.";

        public static string NotFoundById(System.Guid id)
        {
            return $"Stage with id {id} not found.";
        }

        public static string NotFoundById(string idOrSlug)
        {
            return $"Stage with id or slug {idOrSlug} not found.";
        }

        public static string AlreadyExistsInDivision(string stageName)
        {
            return $"Stage with name '{stageName}' already exists in the current division.";
        }

        public static string MaxTeamsReached(int maxTeams)
        {
            return $"This Stage already has the maximum of {maxTeams} teams.";
        }

        public static string NotEnoughSlots(int requested, int available)
        {
            return $"Cannot add {requested} teams. Only {available} slots available.";
        }

        public static string InvalidTournamentSize(int registeredTeams, string validSizes)
        {
            return $"Invalid number of registered teams: {registeredTeams}. Valid sizes are {validSizes} teams.";
        }

        public static string TeamsNotDivisibleForGroups(int registeredTeams, int groupSize)
        {
            return $"The number of registered teams ({registeredTeams}) must be divisible by {groupSize} to generate group stages.";
        }

        public static string ConflictingTeamAssignment(string teamIds)
        {
            return $"Cannot assign team(s) {teamIds} to this division: already assigned to another division of the same tournament.";
        }

        public static string SeedTeamCountOutOfRange(int assignedCount, int slotCapacity)
        {
            return $"Cannot seed: {assignedCount} team(s) assigned to this stage, expected between 2 and {slotCapacity}. " +
            "A team count below the full bracket is fine (the strongest seeds get a bye), but it cannot exceed the generated slots.";
        }
    }

    public static class Match
    {
        public const string CannotUpdateStartedOrFinished = "Cannot update a match that has already started or finished.";
        public const string TeamsNotAssignedToStage = "Cannot update match date because one or both teams are not assigned to the stage.";
        public const string StageAlreadyHasMatches = "Cannot process the current request because the current stage already has some matches.";
        public const string NoGroupStagesForDivision = "No group stages found for the division.";
        public const string NoTeamsRegistered = "No teams are registered in the tournament.";
        public const string NotEnoughTeamsPerGroup = "At least 2 teams per group are required to generate matches.";
        public const string InvalidKnockoutStageType = "Invalid knockout stage type.";
        public const string MatchCountMustBePositive = "Match count must be greater than zero.";
        public const string EndDateBeforeStartDate = "End date must be after start date.";
        public const string StageTypeNotSupportedForAutomatedCreation = "Stage type not supported for automated match creation.";

        // HU-70: basketball has no draws — a played match must have a winner.
        public const string GroupStageTieNotAllowed =
            "A group-stage match cannot end tied; basketball has no draws. Load a decisive score with a winner.";
        public const string PlayoffTieNotAllowed =
            "A playoff match cannot end tied; it must be resolved by overtime. Load the final decisive score with a winner.";

        // HU-73: walkover.
        public const string WalkOverTeamNotInMatch =
            "The present team must be either the home or the visitor team of this match.";

        public static string TeamsNotDistributableAcrossGroups(int registeredTeams, int totalGroups)
        {
            return $"Registered teams ({registeredTeams}) cannot be distributed evenly across {totalGroups} groups.";
        }
    }

    public static class MatchSheet
    {
        // HU-71: the sum of a team's players' points must equal the team's final score.
        public static string ScoreMismatch(int teamScore, int playersSum)
        {
            int difference = teamScore - playersSum;
            return $"The players' points do not add up to the team's score: the team scored {teamScore} " +
                $"but the loaded players sum {playersSum} (difference of {difference}). Fix the sheet before saving.";
        }

        public static string MatchNotFinished(System.Guid matchId)
        {
            return $"Cannot load the match sheet for match {matchId} because it has no final score loaded yet.";
        }

        public static string TeamNotInMatch(System.Guid teamId)
        {
            return $"Team {teamId} did not play in this match, so its players' points cannot be loaded here.";
        }

        public static string PlayerNotOnRoster(System.Guid playerId)
        {
            return $"Player {playerId} is not on this team's roster for this season, so their points cannot be loaded.";
        }

        public static string PlayerNotEligible(System.Guid playerId)
        {
            return $"Player {playerId} is not eligible (missing approved registration or under an active sanction).";
        }
    }

    public static class MedicalRecord
    {
        public const string InvalidPdfFile = "The medical-record file must be a valid PDF.";

        public static string RegistrationNotFound(
            System.Guid playerId, System.Guid teamId, System.Guid tournamentId)
        {
            return $"Player {playerId} has no registration to team {teamId} for tournament " +
                $"{tournamentId}, so a medical record cannot be attached or reviewed.";
        }
    }

    public static class MatchSeries
    {
        public const string RequiresTwoDifferentTeams = "A series requires two different teams.";
        public const string AlreadyExistsForStage = "A series between these two teams already exists for this stage.";
        public const string NotFound = "Series not found.";
        public const string AlreadyDecided = "Cannot add a game to a series that has already been decided.";

        public static string NotFoundById(System.Guid id)
        {
            return $"Series with id {id} not found.";
        }

        public static string MaxGamesReached(int bestOf)
        {
            return $"This series already has the maximum of {bestOf} games.";
        }

        public static string TeamNotAssignedToStage(System.Guid teamId)
        {
            return $"Team {teamId} is not assigned to this stage.";
        }
    }

    public static class PlayerSanction
    {
        public const string AppealAlreadyPending = "This sanction already has a pending appeal.";
        public const string NoPendingAppealToResolve = "There is no pending appeal to resolve for this sanction.";
    }

    public static class Playoff
    {
        public const string NotEnoughRankedTeams = "Seeding requires at least two ranked teams.";
        public const string EmptyDestination = "A playoff position range must have a non-empty destination.";
        public const string NoMappingsConfigured = "This division has no playoff position-range mapping configured.";

        public static string InvalidRange(int from, int to)
        {
            return $"Invalid playoff position range {from}-{to}: positions must be 1-based and 'from' must be less than or equal to 'to'.";
        }

        public static string OverlappingRanges(int firstFrom, int firstTo, int secondFrom, int secondTo)
        {
            return $"Playoff position ranges {firstFrom}-{firstTo} and {secondFrom}-{secondTo} overlap; each position must map to at most one destination.";
        }

        public static string CupStageNotFound(string destination)
        {
            return $"No unseeded first-round elimination stage found for playoff destination '{destination}'.";
        }

        public static string InvalidQualifiersPerGroup(int qualifiersPerGroup)
        {
            return $"QualifiersPerGroup must be at least 1, but was {qualifiersPerGroup}.";
        }
    }

    public static class Backup
    {
        public const string RetentionCountNegative = "Retention count cannot be negative.";
        public const string OperationInProgress = "A backup or restore operation is already in progress.";
    }

    public static class Configuration
    {
        public const string ConnectionStringMissing = "The connection string should be initialized already.";
        public const string JwtMissing = "The JWT is missing or empty in configuration.";

        public static string SmtpKeyMissing(string key)
        {
            return $"{key} is missing from configuration.";
        }

        public static string KeyNotConfigured(string key)
        {
            return $"{key} is not configured.";
        }
    }

    public static class Storage
    {
        public static string UploadFailed(string reason)
        {
            return $"Error uploading file: {reason}";
        }

        public static string DeleteFailed(string reason)
        {
            return $"Error deleting file: {reason}";
        }

        public static string ListFailed(string reason)
        {
            return $"Error listing files: {reason}";
        }

        public static string RemoveFailed(string reason)
        {
            return $"Error removing file: {reason}";
        }

        public static string DownloadFailed(string reason)
        {
            return $"Error downloading file: {reason}";
        }
    }

    public static class Query
    {
        public const string ContainsMethodNotFound = "The 'Contains' method could not be found on the string class.";
        public const string ToLowerMethodNotFound = "The 'ToLower' method could not be found on the string class.";
    }

    public static class Serialization
    {
        public static string InvalidDate(string? rawValue)
        {
            return $"Invalid date: '{rawValue}'.";
        }
    }
}
