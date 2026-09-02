# Modelo de dominio — Backend

Diagrama de clases de las entidades del backend (`Club12-Backend/Domain/Entities/Models/`). 22 clases. Todas heredan de `EntityBase` (Id + audit fields) salvo `Position` y `AppliedPointDeduction`, que son DTOs de solo lectura calculados en memoria, no persistidos. La herencia no se dibuja arco por arco para no saturar el diagrama con 20 líneas repetidas hacia el mismo nodo — `EntityBase` queda suelta, ver su clase al pie.

> Muestra **todos los campos escalares mapeados** de cada entidad. No incluye propiedades `[NotMapped]` (computadas en memoria: `Player.FullName`, `Player.IsHabilitado`, `Player.JerseyNumber`, `PlayerTeamRegistration.IsHabilitado`, etc.) ni navegaciones de colección inversas redundantes con la relación ya dibujada. Regenerar si cambian las entidades; el color de cada clase indica su área funcional.

## Diagrama de clases

```mermaid
classDiagram
  direction TB

  class Season:::comp {
    +string Name
    +string Slug
    +int? Year
  }
  class Tournament:::comp {
    +string Description
    +string Name
    +string Slug
    +DateTime TeamRegistrationDeadline
    +DateTime StartDate
    +TournamentStatus Status
    +TournamentCategory Category
    +Guid? SeasonId
  }
  class Division:::comp {
    +string Name
    +string Slug
    +bool IsFinished
    +Guid TournamentId
    +TournamentCategory Category
    +bool IsCrossDivisionCup
    +int PointsForWin
    +int PointsForLoss
    +int QualifiersPerGroup
  }
  class DivisionPlayoffMapping:::comp {
    +Guid DivisionId
    +int FromPosition
    +int ToPosition
    +string Destination
  }
  class Stage:::comp {
    +string Name
    +string Slug
    +string? Description
    +StageType StageType
    +bool IsActive
    +bool IsElimination
    +DateTime StartDate
    +DateTime EndDate
    +Guid DivisionId
    +int Order
    +string? BracketName
    +int BestOf
    +int RoundRobinLegs
  }
  class StageTeamMatch:::comp {
    +Guid StageId
    +Guid TeamId
  }
  class TeamPointDeduction:::comp {
    +Guid DivisionId
    +Guid TeamId
    +int Points
    +string Reason
  }

  class Club:::team {
    +string Name
    +string Slug
    +string? LogoUrl
  }
  class Team:::team {
    +string Name
    +string Slug
    +string ThreeLetterCode
    +string LogoUrl
    +string ShirtColor
    +string JerseyStyle
    +string? ShirtSecondaryColor
    +Guid? TournamentId
    +Guid? ClubId
  }
  class TeamTournamentRegistration:::team {
    +Guid TeamId
    +Guid TournamentId
  }
  class Venue:::team {
    +string Name
    +string Slug
    +string Address
    +string? PhotoUrl
    +double? Latitude
    +double? Longitude
  }

  class Player:::player {
    +string FirstName
    +string? SecondName
    +string LastName
    +string Slug
    +string DocumentNumber
    +bool IsSanctioned
    +string? PhoneNumber
    +DateTime BirthDate
    +string SocialSecurity
    +Guid TeamId
  }
  class PlayerTeamRegistration:::player {
    +Guid PlayerId
    +Guid TeamId
    +Guid TournamentId
    +int? JerseyNumber
    +MedicalRecordStatus MedicalRecordStatus
    +string? MedicalRecordFileUrl
    +string? MedicalRecordFileName
    +string? MedicalRecordReviewReason
    +DateTime? MedicalRecordReviewedAt
  }
  class PlayerSanction:::player {
    +int Duration
    +DateTime IssuedDate
    +string Description
    +SanctionSubjectType SubjectType
    +Guid? PlayerId
    +Guid? TeamId
    +string? StaffName
    +Guid MatchId
    +string Slug
    +SanctionAppealStatus AppealStatus
    +string? AppealReason
    +DateTime? AppealDate
    +string? AppealResolution
    +DateTime? AppealResolvedDate
  }

  class Match:::match {
    +DateTime MatchDate
    +int? Round
    +MatchType Type
    +string Slug
    +Guid? HomeTeamId
    +Guid? VisitorTeamId
    +int? HomeScore
    +int? VisitorScore
    +bool IsFinished
    +MatchStatus Status
    +Guid? WinningTeamId
    +Guid StageId
    +Guid? VenueId
    +Guid? SeriesId
    +int? GameNumber
  }
  class MatchSeries:::match {
    +Guid StageId
    +Guid HomeTeamId
    +Guid VisitorTeamId
    +int BestOf
    +Guid? WinningTeamId
  }
  class PlayerStatistic:::match {
    +int Value
    +StatisticType Type
    +Guid MatchId
    +Guid PlayerId
  }
  class Scorer:::match {
    +Guid PlayerId
    +int Points
    +Guid MatchId
  }

  class Position:::calc {
    <<not persisted>>
    +Guid TeamId
    +string TeamName
    +string LogoUrl
    +int MatchesPlayed
    +int Wins
    +int Losses
    +int PointsFor
    +int PointsAgainst
    +int PointsDifference
    +int Points
    +TiebreakerCriterion? ResolvedBy
    +AppliedPointDeduction? PointDeduction
  }
  class AppliedPointDeduction:::calc {
    <<not persisted>>
    +int Points
    +string Reason
  }

  class BlogPost:::cfg {
    +string Author
    +string Title
    +string Slug
    +int Views
    +string? PhotoUrl
    +string MarkdownText
    +bool IsPublished
  }
  class AuditLog:::cfg {
    +AuditAction Action
    +string Actor
    +string? TargetType
    +string? TargetId
    +string? Detail
  }
  class BackupRecord:::cfg {
    +string StoragePath
    +long SizeBytes
    +BackupOrigin Origin
  }

  class EntityBase {
    <<abstract>>
    +Guid Id
    +DateTime DateCreated
    +DateTime? DateUpdated
    +string CreatedBy
    +string? UpdatedBy
  }

  Season "1" o-- "*" Tournament : Tournaments
  Tournament "1" o-- "*" Division : Divisions
  Tournament "1" o-- "*" Team : Teams
  Division "1" *-- "*" Stage : Stages
  Division "1" *-- "*" DivisionPlayoffMapping : PlayoffMappings
  Division "1" o-- "*" TeamPointDeduction
  Division "*" --> "0..1" Tournament

  Stage "1" *-- "*" Match : Matches
  Stage "1" *-- "*" StageTeamMatch
  Stage "1" *-- "*" MatchSeries
  StageTeamMatch "*" --> "1" Team

  Club "1" o-- "*" Team : Teams
  Team "*" --> "0..1" Club
  Team "*" --> "0..1" Tournament
  Team "1" o-- "*" TeamTournamentRegistration
  Team "1" o-- "*" PlayerTeamRegistration
  Team "1" o-- "*" Player : Players (current)

  Player "*" --> "1" Team : current team
  Player "1" o-- "*" PlayerTeamRegistration
  Player "1" o-- "*" Scorer
  PlayerTeamRegistration "*" --> "1" Tournament

  Match "*" --> "0..1" Team : HomeTeam
  Match "*" --> "0..1" Team : VisitorTeam
  Match "*" --> "0..1" Team : WinningTeam
  Match "*" --> "0..1" Venue
  Match "*" --> "0..1" MatchSeries : Series
  Match "1" o-- "*" PlayerStatistic
  Match "1" o-- "*" Scorer
  Match "1" o-- "0..1" PlayerSanction : per subject

  MatchSeries "*" --> "1" Team : HomeTeam
  MatchSeries "*" --> "1" Team : VisitorTeam
  MatchSeries "*" --> "0..1" Team : WinningTeam

  PlayerStatistic "*" --> "1" Match
  PlayerStatistic "*" --> "1" Player
  Scorer "*" --> "1" Match
  Scorer "*" --> "1" Player

  PlayerSanction "*" --> "0..1" Player
  PlayerSanction "*" --> "0..1" Team
  PlayerSanction "*" --> "1" Match

  TeamPointDeduction "*" --> "1" Team
  Position "0..1" *-- "0..1" AppliedPointDeduction : PointDeduction

  classDef comp   fill:#dedcf6,stroke:#6f6ac0,color:#2c2870
  classDef team   fill:#d7ead9,stroke:#5a9160,color:#1f4526
  classDef player fill:#fde9c8,stroke:#c98a2e,color:#6a4a12
  classDef match  fill:#cfe8ea,stroke:#3f9aa0,color:#12454a
  classDef calc   fill:#f4d9df,stroke:#bd6b7d,color:#6a2231
  classDef cfg    fill:#e4e5ea,stroke:#8b90a0,color:#33384a
```

## Áreas

| Color | Área | Entidades |
|---|---|---|
| 🟣 | Competencia (torneo/estructura) | Season, Tournament, Division, DivisionPlayoffMapping, Stage, StageTeamMatch, TeamPointDeduction |
| 🟢 | Equipos & canchas | Club, Team, TeamTournamentRegistration, Venue |
| 🟠 | Jugadores | Player, PlayerTeamRegistration, PlayerSanction |
| 🔵 | Partidos | Match, MatchSeries, PlayerStatistic, Scorer |
| 🔴 | Cálculo (no persistido) | Position, AppliedPointDeduction |
| ⚪ | Config / sistema | BlogPost, AuditLog, BackupRecord |

## Invariantes que el diagrama no muestra

- **`Team.TournamentId` es un puntero denormalizado, no la fuente de verdad** — un mismo `Team` se reutiliza entre temporadas repuntando este campo; el historial real de participación vive en `TeamTournamentRegistration` (una fila por temporada, nunca se reescribe). Lo mismo pasa entre `Player.TeamId` (puntero al equipo actual) y `PlayerTeamRegistration` (fuente de verdad del roster por temporada).
- **Ficha médica ("habilitado") es por temporada, no por jugador** — `PlayerTeamRegistration.MedicalRecordStatus` arranca en `Pending` en cada registración nueva; nunca hereda la aprobación de una temporada anterior (HU-59). Un jugador está habilitado solo si el estado es `Approved` **y** hay un archivo real cargado (no un ref legacy) — dos condiciones, no una.
- **`Club` es identidad estable; `Team` es una fila por temporada** — "Colón SF 2026" y "Colón SF 2027" son dos `Team` distintos, opcionalmente enlazados al mismo `Club` vía `Team.ClubId` (FK opcional, aditiva). Sin `Club`, todo sigue funcionando como antes.
- **El bracket de playoffs se arma vía `DivisionPlayoffMapping`, no a mano** — mapea rangos de posición final de la fase de grupos a un destino (`Stage.BracketName`, ej. "Copa Oro"/"Copa Plata"); los rangos no se solapan y una posición fuera de todo rango no clasifica.
- **`MatchSeries.BestOf` se copia del `Stage` al crear la serie** — un cambio posterior en `Stage.BestOf` nunca reescribe retroactivamente una serie en curso o ya decidida.
- **`Match.Round` (jornada) es la clave de agrupación del fixture, no la fecha calendario** — editar la fecha de un partido (HU-68) nunca cambia su ronda; con número impar de equipos, un equipo queda libre cada ronda.
- **`PlayerSanction.SubjectType` decide qué FK es válida** — la sanción apunta a `Player`, `Team` o (sin entidad propia todavía) un `StaffName` de texto libre, exactamente una de las tres según el tipo de sujeto.
- **`Position` y `AppliedPointDeduction` no son tablas** — son objetos calculados en memoria por el servicio de posiciones a partir de `Match` + `TeamPointDeduction`, expuestos solo como DTO de lectura.
- **Slugs son inmutables desde la creación** — casi toda entidad pública (`Team`, `Player`, `Tournament`, `Division`, `Stage`... — no en el diagrama por brevedad salvo donde aparece el campo) genera su `Slug` una sola vez a partir del nombre; renombrar la entidad después nunca rompe un link ya compartido.
