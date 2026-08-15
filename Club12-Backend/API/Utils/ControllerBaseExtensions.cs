using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Utils;

/// <summary>
/// Shared <see cref="ControllerBase"/> extensions for consistent HTTP responses across
/// controllers.
/// </summary>
public static class ControllerBaseExtensions
{
    /// <summary>
    /// Builds a 404 response whose <c>ProblemDetails</c> body matches the shape emitted by
    /// <see cref="GlobalExceptionHandler"/> for unhandled exceptions (title/detail/status,
    /// plus an auto-injected <c>traceId</c> extension from <c>ProblemDetailsFactory</c>).
    /// </summary>
    /// <param name="controller">The controller issuing the response.</param>
    /// <param name="entity">The entity type that could not be found (e.g. <c>nameof(Player)</c>).</param>
    /// <param name="id">The identifier that was looked up.</param>
    /// <returns>An <see cref="ObjectResult"/> with a 404 status and ProblemDetails body.</returns>
    public static ObjectResult NotFoundProblem(this ControllerBase controller, string entity, object id) =>
        controller.Problem(
            detail: $"{entity} with id {id} not found.",
            statusCode: StatusCodes.Status404NotFound,
            title: "Not Found: The specified resource could not be found.");
}
