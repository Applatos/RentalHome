using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sommerhus.Application.Common;

namespace Sommerhus.Api.Infrastructure;

public static class ControllerExtensions
{
    public static ActionResult<T> FromResult<T>(this ControllerBase controller, ServiceResult<T> result)
    {
        return result.Status switch
        {
            ServiceResultStatus.Success => result.Value!,
            ServiceResultStatus.NotFound => controller.NotFound(),
            ServiceResultStatus.Invalid => controller.ValidationProblem(ToValidationProblem(result.Errors)),
            ServiceResultStatus.Conflict => controller.Problem(statusCode: StatusCodes.Status409Conflict, detail: string.Join("\n", Flatten(result.Errors))),
            ServiceResultStatus.Unavailable => controller.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, detail: string.Join("\n", Flatten(result.Errors))),
            _ => throw new InvalidOperationException($"Unsupported service result status: {result.Status}")
        };
    }

    public static ActionResult FromResult(this ControllerBase controller, ServiceResult result)
    {
        return result.Status switch
        {
            ServiceResultStatus.Success => controller.NoContent(),
            ServiceResultStatus.NotFound => controller.NotFound(),
            ServiceResultStatus.Invalid => controller.ValidationProblem(ToValidationProblem(result.Errors)),
            ServiceResultStatus.Conflict => controller.Problem(statusCode: StatusCodes.Status409Conflict, detail: string.Join("\n", Flatten(result.Errors))),
            ServiceResultStatus.Unavailable => controller.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, detail: string.Join("\n", Flatten(result.Errors))),
            _ => throw new InvalidOperationException($"Unsupported service result status: {result.Status}")
        };
    }

    private static ValidationProblemDetails ToValidationProblem(IReadOnlyDictionary<string, string[]> errors)
    {
        var problem = new ValidationProblemDetails();
        foreach (var kvp in errors)
        {
            problem.Errors[kvp.Key] = kvp.Value;
        }
        return problem;
    }

    private static IEnumerable<string> Flatten(IReadOnlyDictionary<string, string[]> errors)
    {
        foreach (var pair in errors)
        {
            foreach (var message in pair.Value)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                {
                    yield return message;
                }
                else
                {
                    yield return $"{pair.Key}: {message}";
                }
            }
        }
    }
}