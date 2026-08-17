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
        public const string TeamManagerMustUseMagicLink = "TeamManager accounts must authenticate via the magic-link flow.";
        public const string MagicLinkOnlyForTeamManager = "Magic-link is only available for TeamManager accounts.";

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

        public static string NotFoundById(System.Guid id)
        {
            return $"Stage with id {id} not found.";
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

        public static string TeamsNotDistributableAcrossGroups(int registeredTeams, int totalGroups)
        {
            return $"Registered teams ({registeredTeams}) cannot be distributed evenly across {totalGroups} groups.";
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
    }

    public static class Backup
    {
        public const string RetentionCountNegative = "Retention count cannot be negative.";
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
