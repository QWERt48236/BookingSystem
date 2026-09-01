using BookingSystem.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace BookingSystem.Api.Common;

public static class ResultExtensions
{
    public static IActionResult ToActionResult(this ControllerBase controller, Result result) =>
        result.Succeeded ? controller.NoContent() : MapError(controller, result.ErrorType, result.Errors);

    public static IActionResult ToActionResult<T>(
        this ControllerBase controller,
        Result<T> result,
        Func<T, IActionResult>? onSuccess = null) =>
        result.Succeeded
            ? onSuccess?.Invoke(result.Value!) ?? controller.Ok(result.Value)
            : MapError(controller, result.ErrorType, result.Errors);

    private static IActionResult MapError(ControllerBase controller, ResultErrorType type, IReadOnlyCollection<string> errors) =>
        type switch
        {
            ResultErrorType.NotFound => controller.NotFound(errors),
            ResultErrorType.Validation => controller.BadRequest(errors),
            ResultErrorType.Conflict => controller.Conflict(errors),
            ResultErrorType.Unauthorized => controller.Unauthorized(errors),
            _ => controller.BadRequest(errors)
        };
}
