using Application.DTOs.Club.Response;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Constants;
using Application.Utils.Helper.Slug;

using Domain.Constants;
using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services;

/// <summary>
/// Manages the stable cross-season club identity.
/// </summary>
public class ClubService(IUnitOfWork unitOfWork) : IClubService
{
    private readonly IClubRepository _clubRepository = unitOfWork.ClubRepository;
    private readonly ITeamRepository _teamRepository = unitOfWork.TeamRepository;
    private readonly ITeamTournamentRegistrationRepository _tournamentRegistrationRepository = unitOfWork.TeamTournamentRegistrationRepository;
    private readonly ITournamentRepository _tournamentRepository = unitOfWork.TournamentRepository;

    /// <inheritdoc />
    public async Task<ClubBackfillResult> BackfillClubsAsync()
    {
        List<Team> unlinkedTeams = [.. (await _teamRepository.GetAllAsync()).Where(team => team.ClubId is null)];

        if (unlinkedTeams.Count == 0)
        {
            return new ClubBackfillResult { ClubsCreated = 0, TeamsLinked = 0 };
        }

        // Slug is the stable identity key: same-named teams generate the same slug and therefore collapse onto a single club. Existing clubs are reused so a partial or previous backfill is never duplicated.
        Dictionary<string, Club> clubsBySlug = (await _clubRepository.GetAllAsync())
            .GroupBy(club => club.Slug)
            .ToDictionary(group => group.Key, group => group.First());

        Dictionary<Guid, string> slugByTeam = [];
        List<Club> clubsToCreate = [];

        foreach (Team team in unlinkedTeams)
        {
            string slug = ClubSlugForTeam(team);
            slugByTeam[team.Id] = slug;

            if (!clubsBySlug.ContainsKey(slug))
            {
                Club club = new()
                {
                    Id = Guid.Empty,
                    Name = !string.IsNullOrWhiteSpace(team.Name) ? team.Name.Trim() : team.ThreeLetterCode,
                    Slug = slug,
                    LogoUrl = string.IsNullOrWhiteSpace(team.LogoUrl) ? null : team.LogoUrl,
                    DateCreated = DateTime.UtcNow,
                    CreatedBy = AuditConstants.SystemUser,
                };

                clubsBySlug[slug] = club;
                clubsToCreate.Add(club);
            }
        }

        if (clubsToCreate.Count > 0)
        {
            // Persist first so EF assigns each new club an Id before it is referenced by a team's ClubId FK.
            await _clubRepository.AddRangeAsync(clubsToCreate);
        }

        foreach (Team team in unlinkedTeams)
        {
            team.ClubId = clubsBySlug[slugByTeam[team.Id]].Id;
        }

        await _teamRepository.UpdateRangeAsync(unlinkedTeams);

        return new ClubBackfillResult
        {
            ClubsCreated = clubsToCreate.Count,
            TeamsLinked = unlinkedTeams.Count,
        };
    }

    /// <inheritdoc />
    public async Task EnsureTeamLinkedToClubAsync(Team team)
    {
        if (team.ClubId is not null)
        {
            return;
        }

        string slug = ClubSlugForTeam(team);

        Club? club = (await _clubRepository.FindAsync(candidate => candidate.Slug == slug)).FirstOrDefault();

        if (club is null)
        {
            club = new Club
            {
                Id = Guid.Empty,
                Name = !string.IsNullOrWhiteSpace(team.Name) ? team.Name.Trim() : team.ThreeLetterCode,
                Slug = slug,
                LogoUrl = string.IsNullOrWhiteSpace(team.LogoUrl) ? null : team.LogoUrl,
                DateCreated = DateTime.UtcNow,
                CreatedBy = AuditConstants.SystemUser,
            };
            await _clubRepository.AddAsync(club);
        }

        team.ClubId = club.Id;
        await _teamRepository.UpdateAsync(team);
    }

    /// <summary>
    /// The stable identity key shared by BackfillClubsAsync and EnsureTeamLinkedToClubAsync.
    /// </summary>
    private static string ClubSlugForTeam(Team team)
    {
        string key = !string.IsNullOrWhiteSpace(team.Name) ? team.Name.Trim() : team.ThreeLetterCode;
        string slug = SlugGenerator.GenerateSlug(key);

        // No alphanumeric characters to slug — fall back to the team id so the club still gets a unique, non-null slug.
        return string.IsNullOrEmpty(slug) ? team.Id.ToString() : slug;
    }

    /// <summary>
    /// Resolves a club by id or slug and assembles its cross-season team history.
    /// </summary>
    public async Task<ClubHistoryResponse?> GetClubHistoryAsync(string idOrSlug)
    {
        Club? club = Guid.TryParse(idOrSlug, out Guid clubId)
            ? await _clubRepository.GetByIdAsync(clubId)
            : (await _clubRepository.FindAsync(candidate => candidate.Slug == idOrSlug)).FirstOrDefault();

        if (club is null)
        {
            return null;
        }

        List<Team> teams = [.. await _teamRepository.FindAsync(team => team.ClubId == club.Id)];
        List<Guid> teamIds = [.. teams.Select(team => team.Id)];

        // Season lookups are batched to avoid N+1 queries: all registrations for these teams, then the tournaments they point at, resolved to names in memory.
        List<TeamTournamentRegistration> registrations = teamIds.Count == 0
            ? []
            : [.. await _tournamentRegistrationRepository.FindAsync(
                registration => teamIds.Contains(registration.TeamId))];

        List<Guid> tournamentIds = [.. registrations.Select(registration => registration.TournamentId).Distinct()];

        Dictionary<Guid, Tournament> tournamentsById = tournamentIds.Count == 0
            ? []
            : (await _tournamentRepository.FindAsync(tournament => tournamentIds.Contains(tournament.Id)))
                .ToDictionary(tournament => tournament.Id);

        ILookup<Guid, TeamTournamentRegistration> registrationsByTeam = registrations.ToLookup(r => r.TeamId);

        ClubSummaryResponse? parentClub = club.ParentClubId is null
            ? null
            : ToSummary(await _clubRepository.GetByIdAsync(club.ParentClubId.Value));

        List<ClubSummaryResponse> childClubs = [.. (await _clubRepository.FindAsync(candidate => candidate.ParentClubId == club.Id))
            .Select(candidate => ToSummary(candidate)!)];

        return new ClubHistoryResponse
        {
            Id = club.Id,
            Name = club.Name,
            Slug = club.Slug,
            LogoUrl = club.LogoUrl,
            ParentClub = parentClub,
            ChildClubs = childClubs,
            Teams = [.. teams.Select(team => new ClubTeamSeasonResponse
            {
                TeamId = team.Id,
                Name = team.Name,
                Slug = team.Slug,
                ThreeLetterCode = team.ThreeLetterCode,
                Seasons = [.. registrationsByTeam[team.Id]
                    .Select(registration =>
                    {
                        tournamentsById.TryGetValue(registration.TournamentId, out Tournament? tournament);
                        return new ClubSeasonResponse
                        {
                            TournamentId = registration.TournamentId,
                            TournamentName = tournament?.Name,
                            StartDate = tournament?.StartDate ?? DateTime.MinValue,
                        };
                    })
                    // Newest season first; the history page flattens these across all teams and re-sorts, but ordering here keeps any single-team consumer correct too.
                    .OrderByDescending(season => season.StartDate)],
            })],
        };
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ClubSummaryResponse>> GetAllClubsAsync()
    {
        IEnumerable<Club> clubs = await _clubRepository.GetAllAsync();

        return [.. clubs
            .OrderBy(club => club.Name)
            .Select(club => ToSummary(club)!)];
    }

    /// <inheritdoc />
    public async Task<ClubHistoryResponse> LinkClubToParentAsync(Guid childClubId, Guid parentClubId)
    {
        if (childClubId == parentClubId)
        {
            throw new InvalidOperationException(ErrorMessages.Club.CannotLinkToItself);
        }

        Club child = await _clubRepository.GetByIdAsync(childClubId)
            ?? throw new KeyNotFoundException(ErrorMessages.Club.NotFound(childClubId));

        Club parent = await _clubRepository.GetByIdAsync(parentClubId)
            ?? throw new KeyNotFoundException(ErrorMessages.Club.NotFound(parentClubId));

        // Flat, one level deep: the parent can't itself be someone else's squad,
        // and the child can't already have squads of its own — either would
        // create a chain longer than institution -> squads.
        if (parent.ParentClubId is not null)
        {
            throw new InvalidOperationException(ErrorMessages.Club.ParentAlreadyHasParent);
        }

        bool childAlreadyHasSquads = await _clubRepository.ExistsAsync(candidate => candidate.ParentClubId == childClubId);
        if (childAlreadyHasSquads)
        {
            throw new InvalidOperationException(ErrorMessages.Club.CannotBecomeChildWithExistingSquads);
        }

        child.ParentClubId = parentClubId;
        await _clubRepository.UpdateAsync(child);

        return await GetClubHistoryAsync(childClubId.ToString())
            ?? throw new KeyNotFoundException(ErrorMessages.Club.NotFound(childClubId));
    }

    /// <inheritdoc />
    public async Task<ClubHistoryResponse> UnlinkClubParentAsync(Guid childClubId)
    {
        Club child = await _clubRepository.GetByIdAsync(childClubId)
            ?? throw new KeyNotFoundException(ErrorMessages.Club.NotFound(childClubId));

        child.ParentClubId = null;
        await _clubRepository.UpdateAsync(child);

        return await GetClubHistoryAsync(childClubId.ToString())
            ?? throw new KeyNotFoundException(ErrorMessages.Club.NotFound(childClubId));
    }

    /// <inheritdoc />
    public async Task<ClubHistoryResponse> RenameClubAsync(Guid clubId, string name)
    {
        string trimmedName = name?.Trim() ?? string.Empty;
        if (trimmedName.Length == 0)
        {
            throw new InvalidOperationException(ErrorMessages.Club.NameRequired);
        }

        Club club = await _clubRepository.GetByIdAsync(clubId)
            ?? throw new KeyNotFoundException(ErrorMessages.Club.NotFound(clubId));

        club.Name = trimmedName;
        await _clubRepository.UpdateAsync(club);

        return await GetClubHistoryAsync(clubId.ToString())
            ?? throw new KeyNotFoundException(ErrorMessages.Club.NotFound(clubId));
    }

    private static ClubSummaryResponse? ToSummary(Club? club) => club is null
        ? null
        : new ClubSummaryResponse
        {
            Id = club.Id,
            Name = club.Name,
            Slug = club.Slug,
            LogoUrl = club.LogoUrl,
        };
}
