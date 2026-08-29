using Application.DTOs.MedicalRecord.Response;

using Domain.Entities.Models;
using Domain.Enums;

namespace API.Tests;

/// <summary>
/// Pure, fixture-free unit tests for the file-backed habilitación rule (Part 2
/// of the medical-records-storage-eligibility change): a registration is
/// "habilitado" only when its medical record is <c>Approved</c> AND it carries
/// a real (non-legacy) stored file reference. Covers the single Domain
/// predicate (<see cref="PlayerTeamRegistration.IsStoredReference"/>), the two
/// computed <c>IsHabilitado</c> properties that consume it
/// (<see cref="PlayerTeamRegistration"/> and the transient
/// <see cref="Player"/> carrier), and the DTO projection
/// (<see cref="MedicalRecordResponse.FromRegistration"/>). No DB, no fixture —
/// these are plain object-graph assertions.
/// </summary>
public class MedicalRecordHabilitacionTests
{
    // ---------- PlayerTeamRegistration.IsStoredReference ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("medical-records/some/object/path.pdf")]
    public void IsStoredReference_NullWhitespaceOrLegacyPrefix_IsFalse(string? fileReference)
    {
        Assert.False(PlayerTeamRegistration.IsStoredReference(fileReference));
    }

    [Fact]
    public void IsStoredReference_NewSchemeReference_IsTrue()
    {
        string reference = $"{Guid.NewGuid()}/{Guid.NewGuid()}/x.pdf";

        Assert.True(PlayerTeamRegistration.IsStoredReference(reference));
    }

    // ---------- PlayerTeamRegistration.IsHabilitado truth table ----------

    [Fact]
    public void Registration_ApprovedWithStoredFile_IsHabilitado()
    {
        PlayerTeamRegistration registration = BuildRegistration(
            MedicalRecordStatus.Approved, $"{Guid.NewGuid()}/{Guid.NewGuid()}/ficha.pdf");

        Assert.True(registration.IsHabilitado);
    }

    [Fact]
    public void Registration_ApprovedWithLegacyReference_IsNotHabilitado()
    {
        PlayerTeamRegistration registration = BuildRegistration(
            MedicalRecordStatus.Approved, "medical-records/legacy/object.pdf");

        Assert.False(registration.IsHabilitado);
    }

    [Fact]
    public void Registration_ApprovedWithNullReference_IsNotHabilitado()
    {
        PlayerTeamRegistration registration = BuildRegistration(MedicalRecordStatus.Approved, null);

        Assert.False(registration.IsHabilitado);
    }

    [Fact]
    public void Registration_PendingWithStoredFile_IsNotHabilitado()
    {
        PlayerTeamRegistration registration = BuildRegistration(
            MedicalRecordStatus.Pending, $"{Guid.NewGuid()}/{Guid.NewGuid()}/ficha.pdf");

        Assert.False(registration.IsHabilitado);
    }

    // ---------- Player.IsHabilitado truth table (transient carrier) ----------

    [Fact]
    public void Player_ApprovedWithHasMedicalRecordFileTrue_IsHabilitado()
    {
        Player player = BuildPlayer(MedicalRecordStatus.Approved, hasMedicalRecordFile: true);

        Assert.True(player.IsHabilitado);
    }

    [Fact]
    public void Player_ApprovedWithHasMedicalRecordFileFalse_IsNotHabilitado()
    {
        Player player = BuildPlayer(MedicalRecordStatus.Approved, hasMedicalRecordFile: false);

        Assert.False(player.IsHabilitado);
    }

    [Fact]
    public void Player_PendingWithHasMedicalRecordFileTrue_IsNotHabilitado()
    {
        Player player = BuildPlayer(MedicalRecordStatus.Pending, hasMedicalRecordFile: true);

        Assert.False(player.IsHabilitado);
    }

    [Fact]
    public void Player_NoSeasonContext_DefaultsToNotHabilitado()
    {
        Player player = BuildPlayer(medicalRecordStatus: null, hasMedicalRecordFile: false);

        Assert.False(player.IsHabilitado);
    }

    // ---------- MedicalRecordResponse.FromRegistration ----------

    [Fact]
    public void FromRegistration_ApprovedWithLegacyReference_IsHabilitadoFalse()
    {
        PlayerTeamRegistration registration = BuildRegistration(
            MedicalRecordStatus.Approved, "medical-records/legacy/object.pdf");

        MedicalRecordResponse response = MedicalRecordResponse.FromRegistration(registration);

        Assert.False(response.IsHabilitado);
    }

    [Fact]
    public void FromRegistration_ApprovedWithStoredFile_IsHabilitadoTrue()
    {
        PlayerTeamRegistration registration = BuildRegistration(
            MedicalRecordStatus.Approved, $"{Guid.NewGuid()}/{Guid.NewGuid()}/ficha.pdf");

        MedicalRecordResponse response = MedicalRecordResponse.FromRegistration(registration);

        Assert.True(response.IsHabilitado);
    }

    private static PlayerTeamRegistration BuildRegistration(MedicalRecordStatus status, string? fileReference)
    {
        return new PlayerTeamRegistration
        {
            PlayerId = Guid.NewGuid(),
            TeamId = Guid.NewGuid(),
            TournamentId = Guid.NewGuid(),
            MedicalRecordStatus = status,
            MedicalRecordFileUrl = fileReference,
            CreatedBy = "test",
        };
    }

    private static Player BuildPlayer(MedicalRecordStatus? medicalRecordStatus, bool hasMedicalRecordFile)
    {
        Team team = new()
        {
            Name = $"Team-{Guid.NewGuid()}",
            Slug = $"team-{Guid.NewGuid()}",
            ThreeLetterCode = "ABC",
            LogoUrl = "https://example.test/logo.png",
            ShirtColor = "Blue",
            Players = [],
            CreatedBy = "test",
        };

        return new Player
        {
            FirstName = "Test",
            LastName = "Player",
            Slug = $"player-{Guid.NewGuid()}",
            DocumentNumber = Guid.NewGuid().ToString("N")[..10],
            IsSanctioned = false,
            BirthDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SocialSecurity = "OSDE",
            Team = team,
            TeamId = team.Id,
            CreatedBy = "test",
            MedicalRecordStatus = medicalRecordStatus,
            HasMedicalRecordFile = hasMedicalRecordFile,
        };
    }
}
