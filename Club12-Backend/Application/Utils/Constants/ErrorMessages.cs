namespace Application.Utils.Constants;

/// <summary>
/// Centralized user-facing and exception message text, so a message never needs to be typed twice and a wording change happens in one place.
/// </summary>
public static class ErrorMessages
{
    public static class Media
    {
        public const string InvalidImageFile = "El archivo de la foto debe ser una imagen JPEG/PNG válida.";
    }

    public static class Auth
    {
        public const string EmailAlreadyExists = "Ya existe un usuario con ese email.";
        public const string InvalidCredentials = "Credenciales inválidas.";
        public const string AccountDeactivated = "Esta cuenta está desactivada.";
        public const string NoAccountForEmail = "No se encontró ninguna cuenta para ese email.";
        public const string InvalidMagicLink = "El magic-link no es válido.";
        public const string MagicLinkAlreadyUsed = "El magic-link no es válido o ya fue usado.";
        public const string InvalidPasswordResetRequest = "Solicitud de restablecimiento de contraseña inválida.";
        public const string InvalidRefreshToken = "El refresh token no es válido.";
        public const string RefreshTokenExpired = "El refresh token venció. Iniciá sesión de nuevo.";
        public const string RoleClaimMissing = "Falta el claim de rol en el token.";
        public const string IdClaimMissing = "Falta el claim de id en el token.";
        public const string AccessDenied = "Acceso denegado.";

        public static string UserCreationFailed(string errors)
        {
            return $"No se pudo crear el usuario: {errors}";
        }

        public static string RoleNotAllowedToCreate(string callerRole, string targetRole)
        {
            return $"El rol '{callerRole}' no puede crear usuarios con rol '{targetRole}'.";
        }
    }

    public static class User
    {
        public const string InsufficientPermissionsToListUsers = "No tenés permisos suficientes para listar usuarios.";
        public const string CurrentPasswordRequired = "Debés indicar la contraseña actual para cambiar tu propia contraseña.";
        public const string CannotChangeOwnActiveState = "No podés cambiar el estado activo de tu propia cuenta.";
        public const string InsufficientPermissionsToDelete = "No tenés permisos suficientes para eliminar este usuario.";
        public const string PasswordResetRestricted = "Solo Admins y Owners (para sus subordinados) pueden blanquear contraseñas.";
        public const string CannotChangeOwnRole = "No podés cambiar tu propio rol.";
        public const string InsufficientPermissionsToChangeRole = "No tenés permisos suficientes para cambiar el rol de este usuario.";

        public static string NotFound(string userId)
        {
            return $"No se encontró el usuario '{userId}'.";
        }

        public static string InvalidRole(object role)
        {
            return $"'{role}' no es un rol válido.";
        }

        public static string RoleNotAllowedToAssign(string callerRole, string targetRole)
        {
            return $"El rol '{callerRole}' no puede asignar el rol '{targetRole}'.";
        }
    }

    public static class Tournament
    {
        /// <summary>
        /// Blocks reverting an Ongoing tournament that already has results.
        /// </summary>
        public const string CannotRevertWithPlayedMatches =
            "No se puede revertir a borrador: el torneo ya tiene partidos jugados. " +
            "Sólo se puede revertir un torneo en curso sin resultados cargados.";

        public static string NotFound(System.Guid tournamentId)
        {
            return $"No existe un torneo con id: {tournamentId}.";
        }

        public static string InvalidStatusTransition(
            Domain.Enums.TournamentStatus from, Domain.Enums.TournamentStatus to)
        {
            return $"No se puede cambiar el estado del torneo de '{from}' a '{to}'. " +
                "El flujo permitido es Scheduled -> OpenForRegistration -> RegistrationClosed -> " +
                "Ongoing -> Finished; un torneo puede pasar a Canceled desde cualquier estado no terminal, " +
                "y Finished/Canceled son terminales.";
        }

        public static string StructuralEditNotAllowed(Domain.Enums.TournamentStatus status)
        {
            return "Los cambios estructurales (divisiones e inscripción de equipos) solo se permiten " +
                $"mientras el torneo está OpenForRegistration; este torneo está '{status}'.";
        }

        public static string CategoryMismatch(
            Domain.Enums.TournamentCategory divisionCategory,
            Domain.Enums.TournamentCategory tournamentCategory)
        {
            return $"Una división '{divisionCategory}' no puede pertenecer a un torneo '{tournamentCategory}' " +
                "(HU-48): la competencia femenina es un torneo aparte y no puede mezclarse con la masculina. " +
                "Creá la división dentro de un torneo de la misma categoría.";
        }

        public static string NotCompletable(string issueSummaries)
        {
            return "No se puede iniciar el torneo porque no está completo en su estado actual (HU-109): " +
                $"{issueSummaries}. Resolvé todos los problemas antes de iniciar.";
        }

        public static string UnenrollNotAllowed(Domain.Enums.TournamentStatus status)
        {
            return "Los equipos solo se pueden dar de baja mientras el torneo está OpenForRegistration o " +
                $"RegistrationClosed; este torneo está '{status}'.";
        }

        /// <summary>
        /// User-facing Spanish message blocking deletion of a tournament that already started or has played matches, to avoid erasing competitive history.
        /// </summary>
        public const string HasHistoryCannotDelete =
            "No se puede eliminar el torneo: tiene partidos jugados o ya arrancó.";
    }

    public static class Division
    {
        /// <summary>
        /// User-facing Spanish message blocking deletion of a division that already has played matches or point deductions, to avoid cascading away that competitive history.
        /// </summary>
        public const string HasHistoryCannotDelete =
            "No se puede eliminar la división: tiene partidos jugados o deducciones de puntos registradas.";

        /// <summary>
        /// User-facing Spanish message blocking deletion of a division whose tournament fixture is already generated.
        /// </summary>
        public const string StructureLockedTournamentStarted =
            "No se puede eliminar la división: el torneo ya arrancó o fue cancelado.";

        public static string ConflictingRosterEnrollment(string teamIds)
        {
            return $"No se puede inscribir el/los equipo(s) {teamIds} en esta división: ya está(n) inscripto(s) en otra división del mismo torneo.";
        }
    }

    public static class Team
    {
        public static string NotFound(System.Guid teamId)
        {
            return $"No existe un equipo con id: {teamId}.";
        }

        /// <summary>
        /// User-facing Spanish message blocking deletion of a team that carries competitive history including match participations, sanctions, deductions or registrations.
        /// </summary>
        public const string HasHistoryCannotDelete =
            "No se puede eliminar el equipo: tiene historial de partidos, sanciones, deducciones o inscripciones a torneos.";

        /// <summary>
        /// User-facing Spanish message blocking edits to Team.Name or Team.ThreeLetterCode while the team plays in an Ongoing tournament.
        /// </summary>
        public const string IdentityFrozenWhileOngoing =
            "No se puede cambiar el nombre ni la sigla del equipo mientras participa en un torneo en curso. " +
            "Podés editar los colores y el escudo.";

        public static string NotInTournament(System.Guid teamId)
        {
            return $"El equipo '{teamId}' no está inscripto en ningún torneo actualmente, así que no se pueden registrar jugadores en él.";
        }

        public static string AlreadyEnrolled(System.Guid teamId, System.Guid tournamentId)
        {
            return $"El equipo '{teamId}' ya está inscripto en el torneo '{tournamentId}'. Un equipo solo puede inscribirse una vez por torneo.";
        }

        public static string NotEnrolled(System.Guid teamId, System.Guid tournamentId)
        {
            return $"El equipo '{teamId}' no está inscripto en el torneo '{tournamentId}'.";
        }
    }

    public static class Roster
    {
        public static string PlayerAlreadyInAnotherTeam(System.Guid playerId, System.Guid tournamentId)
        {
            return $"El jugador '{playerId}' ya está registrado en otro equipo del torneo '{tournamentId}'. Un jugador no puede estar en dos equipos del mismo torneo.";
        }

        public static string RosterFull(System.Guid teamId, int maxPlayers)
        {
            return $"El equipo '{teamId}' ya tiene el máximo de {maxPlayers} jugadores para este torneo.";
        }

        public static string DuplicateJerseyNumber(int jerseyNumber, System.Guid teamId, System.Guid tournamentId)
        {
            return $"El dorsal {jerseyNumber} ya lo usa otro jugador del equipo '{teamId}' en el torneo '{tournamentId}'.";
        }
    }

    public static class Player
    {
        /// <summary>
        /// User-facing Spanish message blocking deletion of a player who has match statistics, scorer records or sanctions, to avoid orphaning historical records.
        /// </summary>
        public const string HasHistoryCannotDelete =
            "No se puede eliminar: el jugador tiene estadísticas o sanciones registradas.";

        /// <summary>
        /// User-facing Spanish message blocking a mid-season team move for a player who already has match statistics, scorer records or sanctions that season.
        /// </summary>
        public const string CannotMoveTeamWithHistory =
            "No se puede cambiar de equipo a un jugador que ya tiene estadísticas o sanciones registradas en esta temporada.";

        /// <summary>
        /// User-facing Spanish message returned when a player is created or updated with a DocumentNumber another player already has.
        /// </summary>
        public static string DuplicateDocumentNumber(string documentNumber)
        {
            return $"Ya existe un jugador registrado con el documento {documentNumber}.";
        }
    }

    public static class Venue
    {
        /// <summary>
        /// User-facing Spanish message blocking deletion of a venue that is referenced by one or more matches.
        /// </summary>
        public const string ReferencedByMatches =
            "No se puede eliminar: la cancha tiene partidos asociados.";
    }

    public static class Stage
    {
        public const string NotFoundGeneric = "No se encontró la fase.";
        public const string DivisionNotFound = "No se encontró la división.";
        public const string DivisionAlreadyHasStages = "No se puede procesar la solicitud porque la división ya tiene alguna fase.";
        public const string GenerateMatchesBeforeSeeding = "Generá los partidos de esta fase antes de sembrarla.";
        public const string AlreadySeeded = "Esta fase ya fue sembrada.";
        public const string InvalidStageType = "Tipo de fase inválido.";
        public const string SeedMissingStandings = "No se puede sembrar: todavía no todos los equipos asignados a esta fase tienen una posición de la fase de grupos finalizada.";

        /// <summary>
        /// User-facing Spanish message blocking adding or removing a stage once the division's tournament has already started.
        /// </summary>
        public const string StructureLockedTournamentStarted =
            "No se pueden agregar o quitar fases: el torneo ya arrancó o fue cancelado.";

        public static string NotFoundById(System.Guid id)
        {
            return $"No se encontró la fase con id {id}.";
        }

        public static string NotFoundById(string idOrSlug)
        {
            return $"No se encontró la fase con id o slug {idOrSlug}.";
        }

        public static string AlreadyExistsInDivision(string stageName)
        {
            return $"Ya existe una fase con el nombre '{stageName}' en la división actual.";
        }

        public static string MaxTeamsReached(int maxTeams)
        {
            return $"Esta fase ya tiene el máximo de {maxTeams} equipos.";
        }

        public static string NotEnoughSlots(int requested, int available)
        {
            return $"No se pueden agregar {requested} equipos. Solo hay {available} lugares disponibles.";
        }

        public static string InvalidTournamentSize(int registeredTeams, string validSizes)
        {
            return $"Cantidad inválida de equipos inscriptos: {registeredTeams}. Los tamaños válidos son {validSizes} equipos.";
        }

        public static string TeamsNotDivisibleForGroups(int registeredTeams, int groupSize)
        {
            return $"La cantidad de equipos inscriptos ({registeredTeams}) debe ser divisible por {groupSize} para generar las fases de grupos.";
        }

        public static string ConflictingTeamAssignment(string teamIds)
        {
            return $"No se puede asignar el/los equipo(s) {teamIds} a esta división: ya está(n) asignado(s) a otra división del mismo torneo.";
        }

        public static string TeamNotEnrolledInDivision(string teamIds)
        {
            return $"No se puede asignar el/los equipo(s) {teamIds} a esta fase: no está(n) inscripto(s) en el roster de la división.";
        }

        public static string SeedTeamCountOutOfRange(int assignedCount, int slotCapacity)
        {
            return $"No se puede sembrar: hay {assignedCount} equipo(s) asignado(s) a esta fase, se esperaba entre 2 y {slotCapacity}. " +
            "Una cantidad de equipos menor al cuadro completo está bien (los mejores seeds pasan con bye), pero no puede superar los lugares generados.";
        }

        /// <summary>
        /// User-facing Spanish message blocking a playoffs-only draw preview or commit on a division that still has a group phase.
        /// </summary>
        public const string DrawRequiresGrouplessDivision =
            "El sorteo de llave es solo para divisiones sin fase de grupos; esta división ya tiene una fase de grupos.";

        /// <summary>
        /// User-facing Spanish message rejecting a draw commit whose token is missing, tampered, expired, or does not match the stage or roster.
        /// </summary>
        public const string InvalidDrawToken =
            "El token del sorteo no es válido o no corresponde a esta fase. Volvé a previsualizar el sorteo.";

        /// <summary>
        /// User-facing Spanish message rejecting a manual seeding order that is not exactly a permutation of the division's enrolled roster.
        /// </summary>
        public const string ManualOrderNotRosterPermutation =
            "El orden manual debe incluir exactamente a los equipos inscriptos en el roster de la división, sin repetir ni omitir ninguno.";

        /// <summary>
        /// User-facing Spanish message blocking a bracket draw or re-draw once a real match of that bracket has already been played.
        /// </summary>
        public const string BracketAlreadyPlayed =
            "No se puede sortear ni resortear esta llave: ya hay al menos un partido jugado en este cuadro.";

        /// <summary>
        /// User-facing Spanish message rejecting a sub-group count below 1.
        /// </summary>
        public const string SubGroupCountMustBePositive =
            "La cantidad de sub-grupos debe ser mayor o igual a 1.";

        /// <summary>
        /// User-facing Spanish message rejecting a manual reassignment for a team not currently placed in the source sub-group.
        /// </summary>
        public const string TeamNotPlacedInSubGroup =
            "El equipo no está ubicado en el sub-grupo de origen indicado.";

        /// <summary>
        /// User-facing Spanish message rejecting a manual reassignment across two sub-groups of different divisions.
        /// </summary>
        public const string ReassignmentAcrossDivisionsNotAllowed =
            "No se puede reasignar un equipo entre sub-grupos de distintas divisiones.";

        /// <summary>
        /// User-facing Spanish message blocking sub-groups from combining with a position-range playoff cup, since a cup's position range has no defined meaning across multiple independent sub-group tables.
        /// </summary>
        public const string SubGroupsIncompatibleWithPositionRangeCups =
            "No se pueden combinar sub-grupos con una copa configurada por rango de posiciones: la tabla de posiciones combinada no está definida para varios sub-grupos independientes. Usá un solo sub-grupo o quitá el mapeo de playoff antes de continuar.";

        public static string SubGroupTooFewTeams(int teamCount, int subGroupCount)
        {
            return $"No se pueden crear {subGroupCount} sub-grupos con {teamCount} equipo(s) inscripto(s): cada sub-grupo necesita como mínimo 4 equipos. Elegí una cantidad de sub-grupos menor.";
        }

        public static string SubGroupReassignmentBelowMinimum(int remainingTeams)
        {
            return $"No se puede mover el equipo: el sub-grupo de origen quedaría con {remainingTeams} equipo(s), por debajo del mínimo de 4.";
        }
    }

    public static class Match
    {
        public const string CannotUpdateStartedOrFinished = "No se puede editar un partido que ya arrancó o finalizó.";
        public const string TeamsNotAssignedToStage = "No se puede actualizar la fecha del partido porque uno o ambos equipos no están asignados a la fase.";
        public const string VenueScheduleConflict =
            "Esa cancha ya tiene otro partido a menos de 2 horas de esa hora. " +
            "Elegí otro horario (mínimo 2 horas de diferencia) u otra cancha.";
        public const string StageAlreadyHasMatches = "No se puede procesar la solicitud porque la fase actual ya tiene partidos.";
        public const string NoGroupStagesForDivision = "No se encontraron fases de grupos para la división.";
        public const string NoTeamsRegistered = "No hay equipos inscriptos en el torneo.";
        public const string NotEnoughTeamsPerGroup = "Se necesitan al menos 2 equipos por grupo para generar los partidos.";
        public const string InvalidKnockoutStageType = "Tipo de fase eliminatoria inválido.";
        public const string MatchCountMustBePositive = "La cantidad de partidos debe ser mayor a cero.";
        public const string EndDateBeforeStartDate = "La fecha de fin debe ser posterior a la fecha de inicio.";
        public const string StageTypeNotSupportedForAutomatedCreation = "Este tipo de fase no admite la generación automática de partidos.";

        // Basketball has no draws — a played match must have a winner.
        public const string GroupStageTieNotAllowed =
            "Un partido de fase de grupos no puede terminar empatado; el básquet no tiene empates. Cargá un resultado con un ganador.";
        public const string PlayoffTieNotAllowed =
            "Un partido de playoff no puede terminar empatado; debe resolverse en un tiempo suplementario. Cargá el resultado final con un ganador.";

        public const string WalkOverTeamNotInMatch =
            "El equipo presente tiene que ser el local o el visitante de este partido.";

        public static string TeamsNotDistributableAcrossGroups(int registeredTeams, int totalGroups)
        {
            return $"Los equipos inscriptos ({registeredTeams}) no se pueden distribuir de forma pareja entre {totalGroups} grupos.";
        }
    }

    public static class MatchSheet
    {
        // The sum of a team's players' points must equal the team's final score.
        public static string ScoreMismatch(int teamScore, int playersSum)
        {
            int difference = teamScore - playersSum;
            return $"Los puntos de los jugadores no coinciden con el resultado del equipo: el equipo anotó {teamScore} " +
                $"pero la suma de los jugadores cargados da {playersSum} (diferencia de {difference}). Corregí la planilla antes de guardar.";
        }

        public static string MatchNotFinished(System.Guid matchId)
        {
            return $"No se puede cargar la planilla del partido {matchId} porque todavía no tiene un resultado final cargado.";
        }

        public static string MatchMissingTeams(System.Guid matchId)
        {
            return $"El partido {matchId} todavía no tiene los dos equipos asignados, así que no se le puede cargar un resultado.";
        }

        public static string TeamNotInMatch(System.Guid teamId)
        {
            return $"El equipo {teamId} no jugó este partido, así que no se pueden cargar sus puntos acá.";
        }

        public static string PlayerNotOnRosterReason(string playerLabel)
        {
            return $"{playerLabel} (no está en el plantel del equipo para esta temporada)";
        }

        public static string PlayerNotEligibleReason(string playerLabel)
        {
            return $"{playerLabel} (no está habilitado: le falta la inscripción aprobada o tiene una sanción activa)";
        }

        /// <summary>
        /// One combined error naming every ineligible or off-roster player across both teams' sheets, grouped by team.
        /// </summary>
        public static string PlayersNotEligible(
            System.Collections.Generic.IEnumerable<(string TeamName, System.Collections.Generic.List<string> Reasons)> issuesByTeam)
        {
            System.Collections.Generic.List<string> groups = [];
            foreach ((string teamName, System.Collections.Generic.List<string> reasons) in issuesByTeam)
            {
                groups.Add($"{teamName}: {string.Join(", ", reasons)}.");
            }

            return $"No se puede cargar el resultado porque hay jugadores no elegibles. {string.Join(" ", groups)}";
        }

        public static string TeamRequiresWalkOver(string teamName, int habilitadoCount)
        {
            return $"{teamName} tiene {habilitadoCount} jugador(es) habilitado(s) (mínimo 4), así que este partido no se puede " +
                "cargar con un resultado normal. Cargalo como walkover.";
        }
    }

    public static class MedicalRecord
    {
        public const string InvalidPdfFile = "El archivo de la ficha médica debe ser un PDF válido.";

        /// <summary>
        /// User-facing Spanish message returned when a new ficha upload is attempted on a registration whose medical record is already Approved.
        /// </summary>
        public const string AlreadyApproved =
            "La ficha médica ya está aprobada; no se puede subir una nueva. Solo puede consultarse o descargarse.";

        /// <summary>
        /// User-facing Spanish message returned when an approve is attempted against a registration with no real stored file.
        /// </summary>
        public const string NoStoredFile =
            "No se puede aprobar la ficha médica: no hay un archivo cargado. Subí la ficha antes de aprobarla.";

        public static string RegistrationNotFound(
            System.Guid playerId, System.Guid teamId, System.Guid tournamentId)
        {
            return $"El jugador {playerId} no tiene una inscripción al equipo {teamId} para el torneo " +
                $"{tournamentId}, así que no se puede adjuntar ni revisar una ficha médica.";
        }
    }

    public static class MatchSeries
    {
        public const string RequiresTwoDifferentTeams = "Una serie requiere dos equipos distintos.";
        public const string AlreadyExistsForStage = "Ya existe una serie entre estos dos equipos para esta fase.";
        public const string NotFound = "No se encontró la serie.";
        public const string AlreadyDecided = "No se puede agregar un partido a una serie que ya se definió.";

        public static string NotFoundById(System.Guid id)
        {
            return $"No se encontró la serie con id {id}.";
        }

        public static string MaxGamesReached(int bestOf)
        {
            return $"Esta serie ya tiene el máximo de {bestOf} partidos.";
        }

        public static string TeamNotAssignedToStage(System.Guid teamId)
        {
            return $"El equipo {teamId} no está asignado a esta fase.";
        }
    }

    public static class PlayerSanction
    {
        public const string AppealAlreadyPending = "Esta sanción ya tiene una apelación pendiente.";
        public const string NoPendingAppealToResolve = "No hay ninguna apelación pendiente para resolver en esta sanción.";
    }

    public static class Playoff
    {
        public const string NotEnoughRankedTeams = "Para sembrar se necesitan al menos dos equipos rankeados.";
        public const string EmptyDestination = "Un rango de posiciones de playoff debe tener un destino no vacío.";
        public const string NoMappingsConfigured = "Esta división no tiene ningún mapeo de rango de posiciones a playoff configurado.";

        public static string InvalidRange(int from, int to)
        {
            return $"Rango de posiciones de playoff inválido {from}-{to}: las posiciones empiezan en 1 y 'desde' debe ser menor o igual a 'hasta'.";
        }

        public static string OverlappingRanges(int firstFrom, int firstTo, int secondFrom, int secondTo)
        {
            return $"Los rangos de posiciones de playoff {firstFrom}-{firstTo} y {secondFrom}-{secondTo} se superponen; cada posición debe mapear a un solo destino.";
        }

        public static string CupStageNotFound(string destination)
        {
            return $"No se encontró una fase eliminatoria de primera ronda sin sembrar para el destino de playoff '{destination}'.";
        }

        public static string InvalidQualifiersPerGroup(int qualifiersPerGroup)
        {
            return $"QualifiersPerGroup debe ser al menos 1, pero era {qualifiersPerGroup}.";
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
