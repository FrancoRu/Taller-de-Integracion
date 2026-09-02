using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using Application.DTOs.Auth.Request;
using Application.DTOs.Player.Request;
using Application.DTOs.User.Request;

namespace API.Tests;

/// <summary>
/// Pins the email / phone DataAnnotations on the request DTOs that collect
/// contact data. The [ApiController] filter turns any of these validation
/// failures into a 400, so exercising the attributes directly is enough to
/// guarantee the endpoints reject malformed input and accept plausible input.
/// The accepted phone shape mirrors the frontend validator
/// (src/modules/core/utils/validators.ts).
/// </summary>
public class ContactFieldValidationTests
{
    private static IReadOnlyList<ValidationResult> Validate(object model)
    {
        List<ValidationResult> results = new();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    private static bool MemberInvalid(object model, string memberName)
    {
        foreach (ValidationResult result in Validate(model))
        {
            foreach (string member in result.MemberNames)
            {
                if (member == memberName)
                {
                    return true;
                }
            }
        }

        return false;
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("no-at-sign.com")]
    [InlineData("@no-local.com")]
    public void RegisterUserRequest_InvalidEmail_IsRejected(string email)
    {
        RegisterUserRequest request = new()
        {
            Email = email,
            Username = "validuser",
            Role = "ADMIN",
        };

        Assert.True(MemberInvalid(request, nameof(RegisterUserRequest.Email)));
    }

    [Theory]
    [InlineData("abc12345")]        // letters not allowed
    [InlineData("123")]             // too few digits
    [InlineData("12345678901234567")] // too many digits
    [InlineData("+541123456789")]   // +54 country code not accepted — local-only app
    [InlineData("01123456789")]     // 0 long-distance prefix not accepted
    public void RegisterUserRequest_InvalidPhone_IsRejected(string phone)
    {
        RegisterUserRequest request = new()
        {
            Email = "user@example.com",
            Username = "validuser",
            Phone = phone,
            Role = "ADMIN",
        };

        Assert.True(MemberInvalid(request, nameof(RegisterUserRequest.Phone)));
    }

    [Theory]
    [InlineData(null)]                  // optional — absent is fine
    [InlineData("11 2345-6789")]
    [InlineData("1123456789")]
    public void RegisterUserRequest_ValidContactData_IsAccepted(string? phone)
    {
        RegisterUserRequest request = new()
        {
            Email = "user@example.com",
            Username = "validuser",
            Phone = phone,
            Role = "ADMIN",
        };

        Assert.False(MemberInvalid(request, nameof(RegisterUserRequest.Email)));
        Assert.False(MemberInvalid(request, nameof(RegisterUserRequest.Phone)));
    }

    [Theory]
    [InlineData("bad-phone")]
    [InlineData("12")]
    [InlineData("+541123456789")]
    public void CreatePlayerRequest_InvalidPhone_IsRejected(string phone)
    {
        CreatePlayerRequest request = NewPlayer(phone);

        Assert.True(MemberInvalid(request, nameof(CreatePlayerRequest.PhoneNumber)));
    }

    [Theory]
    [InlineData("1123456789")]
    [InlineData("343 555-1234")]
    public void CreatePlayerRequest_ValidPhone_IsAccepted(string phone)
    {
        CreatePlayerRequest request = NewPlayer(phone);

        Assert.False(MemberInvalid(request, nameof(CreatePlayerRequest.PhoneNumber)));
    }

    [Fact]
    public void UpdateUserRequest_InvalidEmailAndPhone_AreRejected()
    {
        UpdateUserRequest request = new()
        {
            Email = "nope",
            Phone = "letters-here",
        };

        Assert.True(MemberInvalid(request, nameof(UpdateUserRequest.Email)));
        Assert.True(MemberInvalid(request, nameof(UpdateUserRequest.Phone)));
    }

    [Fact]
    public void UpdateUserRequest_NullOptionalFields_AreAccepted()
    {
        UpdateUserRequest request = new();

        Assert.Empty(Validate(request));
    }

    [Theory]
    [InlineData("3435551234")] // 10-digit national number
    [InlineData("343 555-1234")]
    public void UpdateUserRequest_ArgentinePhoneShapes_AreAccepted(string phone)
    {
        UpdateUserRequest request = new() { Phone = phone };

        Assert.False(MemberInvalid(request, nameof(UpdateUserRequest.Phone)));
    }

    [Theory]
    [InlineData("93435551234")] // 11 digits — the 9 mobile marker is not accepted
    [InlineData("03435551234")] // 11 digits — the 0 trunk prefix is not accepted
    [InlineData("543435551234")] // 12 digits — the 54 country code is not accepted
    [InlineData("5493435551234")] // 13 digits — +54 9 is not accepted
    public void UpdateUserRequest_ImplausibleArgentinePhoneShapes_AreRejected(string phone)
    {
        UpdateUserRequest request = new() { Phone = phone };

        Assert.True(MemberInvalid(request, nameof(UpdateUserRequest.Phone)));
    }

    private static CreatePlayerRequest NewPlayer(string phone) => new()
    {
        FirstName = "Test",
        LastName = "Player",
        DocumentNumber = "12345678",
        BirthDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        PhoneNumber = phone,
        SocialSecurity = "OSDE",
        TeamId = Guid.NewGuid(),
    };
}
