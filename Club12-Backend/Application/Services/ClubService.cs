using Application.DTOs.Club.Response;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Helper.Slug;

using Domain.Constants;
using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services;

/// <summary>
/// Service for the stable cross-season club identity (HU-99). The club layer is
/// purely additive: it never mutates a team's season-scoped state, only sets the
/// optional <see cref="Team.ClubId"/> link and reads history back through the
/// existing <see cref="TeamTournamentRegistration"/> join.
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

        // Slug is the stable identity key: same-named teams generate the same
        // slug and therefore collapse onto a single club. Existing clubs are
        // reused so a partial/previous backfill is never duplicated.
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
            // Persist first so EF assigns each new club an Id before it is
            // referenced by a team's ClubId FK.
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
    /// The stable identity key shared by <see cref="BackfillClubsAsync"/> and
    /// <see cref="EnsureTeamLinkedToClubAsync"/>: same-named teams generate the
    /// same slug and therefore collapse onto a single club.
    /// </summary>
    private static string ClubSlugForTeam(Team team)
    {
        string key = !string.IsNullOrWhiteSpace(team.Name) ? team.Name.Trim() : team.ThreeLetterCode;
        string slug = SlugGenerator.GenerateSlug(key);

        // No alphanumeric characters to slug — fall back to the team id so
        // the club still gets a unique, non-null slug.
        return string.IsNullOrEmpty(slug) ? team.Id.ToString() : slug;
    }

    /// <inheritdoc />
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

        // Batched season lookups (avoid N+1): all registrations for these teams,
        // then the tournaments they point at, resolved to names in memory.
        List<TeamTournamentRegistration> registrations = teamIds.Count == 0
            ? []
            : [.. await _tournamentRegistrationRepository.FindAsync(
                registration => teamIds.Contains(registration.TeamId))];

        List<Guid> tournamentIds = [.. registrations.Select(registration => registration.TournamentId).Distinct()];

        Dictionary<Guid, string> tournamentNames = tournamentIds.Count == 0
            ? []
            : (await _tournamentRepository.FindAsync(tournament => tournamentIds.Contains(tournament.Id)))
                .ToDictionary(tournament => tournament.Id, tournament => tournament.Name);

        ILookup<Guid, TeamTournamentRegistration> registrationsByTeam = registrations.ToLookup(r => r.TeamId);

        return new ClubHistoryResponse
        {
            Id = club.Id,
            Name = club.Name,
            Slug = club.Slug,
            LogoUrl = club.LogoUrl,
            Teams = [.. teams.Select(team => new ClubTeamSeasonResponse
            {
                TeamId = team.Id,
                Name = team.Name,
                Slug = team.Slug,
                ThreeLetterCode = team.ThreeLetterCode,
                Seasons = [.. registrationsByTeam[team.Id]
                    .Select(registration => new ClubSeasonResponse
                    {
                        TournamentId = registration.TournamentId,
                        TournamentName = tournamentNames.GetValueOrDefault(registration.TournamentId),
                    })],
            })],
        };
    }
}
