using API.Controllers;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System.Linq;
using System.Reflection;

namespace API.Tests;

/// <summary>
/// Guards the multipart-upload contract (QA wave 1, Bug 1): every controller
/// action that accepts a file-bearing request DTO must bind it from the form
/// (<c>[FromForm]</c>). Under <c>[ApiController]</c>, binding-source inference
/// treats a user-defined complex type as <c>[FromBody]</c> (JSON) unless it is
/// explicitly annotated — so a missing <c>[FromForm]</c> makes the endpoint
/// reject the frontend's <c>multipart/form-data</c> upload with HTTP 415
/// (Unsupported Media Type). These endpoints cannot be exercised through the
/// full HTTP host (their controllers take a constructor-injected SupabaseHelper
/// whose ctor opens a live Supabase connection — the same pre-existing
/// testability gap documented in SupabaseDependentControllerNotFoundTests), so
/// the contract is verified at the binding-metadata layer instead.
/// </summary>
public class UploadFromFormBindingTests
{
    public static readonly TheoryData<System.Type, string, string> UploadActions = new()
    {
        { typeof(TeamController), nameof(TeamController.CreateTeam), "teamRequest" },
        { typeof(TeamController), nameof(TeamController.UpdateTeamLogo), "logoRequest" },
        { typeof(BlogPostController), nameof(BlogPostController.CreateBlogPost), "blogPostRequest" },
        { typeof(BlogPostController), nameof(BlogPostController.UpdateBlogPostPhoto), "photoRequest" },
        { typeof(VenueController), nameof(VenueController.CreateVenue), "venueRequest" },
        { typeof(MedicalRecordController), nameof(MedicalRecordController.UploadMedicalRecord), "request" },
    };

    [Theory]
    [MemberData(nameof(UploadActions))]
    public void UploadAction_FileBearingParameter_IsBoundFromForm(
        System.Type controllerType, string actionName, string parameterName)
    {
        MethodInfo action = controllerType.GetMethod(actionName)!;
        Assert.NotNull(action);

        ParameterInfo parameter = action.GetParameters().Single(p => p.Name == parameterName);

        // The request DTO carries an IFormFile property, so the endpoint only
        // works when the parameter is bound from the multipart form.
        Assert.Contains(parameter.ParameterType.GetProperties(),
            property => property.PropertyType == typeof(IFormFile));

        FromFormAttribute? fromForm = parameter.GetCustomAttribute<FromFormAttribute>();
        Assert.True(fromForm is not null,
            $"{controllerType.Name}.{actionName}({parameterName}) must be [FromForm] to accept multipart/form-data uploads (otherwise ApiController infers [FromBody] and returns HTTP 415).");
    }
}
