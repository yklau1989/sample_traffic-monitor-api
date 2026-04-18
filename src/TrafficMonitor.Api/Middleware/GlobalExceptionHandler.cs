using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TrafficMonitor.Application.Exceptions;

namespace TrafficMonitor.Api.Middleware;

internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private const string BadRequestType = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1";
    private const string InternalServerErrorType = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1";
    private const string ValidationErrorTitle = "One or more validation errors occurred.";
    private const string InternalServerErrorTitle = "Internal Server Error";
    private const string ValidationErrorDetail = "See the errors property for details.";
    private const string UnexpectedErrorDetail = "An unexpected error occurred while processing the request.";
    private const string DefaultValidationMemberName = "value";
    private const string ErrorsExtensionName = "errors";
    private const string TraceIdExtensionName = "traceId";

    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IProblemDetailsService problemDetailsService)
    {
        _logger = logger;
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ProblemDetails problemDetails;

        if (exception is ValidationException validationException)
        {
            problemDetails = CreateValidationProblemDetails(validationException, httpContext);
        }
        else if (exception is InvalidSortFieldException invalidSortFieldException)
        {
            problemDetails = new ProblemDetails
            {
                Type = BadRequestType,
                Title = "Invalid sort field",
                Status = StatusCodes.Status400BadRequest,
                Detail = invalidSortFieldException.Message
            };

            problemDetails.Extensions[TraceIdExtensionName] = GetTraceId(httpContext);
        }
        else if (exception is ArgumentException argumentException)
        {
            problemDetails = CreateBadRequestProblemDetails(argumentException, httpContext);
        }
        else
        {
            var traceId = GetTraceId(httpContext);
            _logger.LogError(exception, "Unhandled exception for traceId {TraceId}.", traceId);
            problemDetails = CreateInternalServerErrorProblemDetails(traceId);
        }

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        await _problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails
        });

        return true;
    }

    private static ProblemDetails CreateValidationProblemDetails(ValidationException exception, HttpContext httpContext)
    {
        var validationResult = exception.ValidationResult;
        var memberNames = validationResult?.MemberNames
            .Where(static memberName => !string.IsNullOrWhiteSpace(memberName))
            .ToList() ?? [];
        var key = memberNames.Count > 0 ? memberNames[0] : DefaultValidationMemberName;
        var message = validationResult?.ErrorMessage;

        if (string.IsNullOrWhiteSpace(message))
        {
            message = "Validation failed.";
        }

        var problemDetails = new ProblemDetails
        {
            Type = BadRequestType,
            Title = ValidationErrorTitle,
            Status = StatusCodes.Status400BadRequest,
            Detail = ValidationErrorDetail
        };

        problemDetails.Extensions[TraceIdExtensionName] = GetTraceId(httpContext);
        problemDetails.Extensions[ErrorsExtensionName] = new Dictionary<string, string[]>
        {
            [key] = [message]
        };

        return problemDetails;
    }

    private static ProblemDetails CreateBadRequestProblemDetails(ArgumentException exception, HttpContext httpContext)
    {
        var problemDetails = new ProblemDetails
        {
            Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.20",
            Title = "Unprocessable Entity",
            Status = StatusCodes.Status422UnprocessableEntity,
            Detail = exception.Message
        };

        problemDetails.Extensions[TraceIdExtensionName] = GetTraceId(httpContext);

        return problemDetails;
    }

    private static ProblemDetails CreateInternalServerErrorProblemDetails(string traceId)
    {
        var problemDetails = new ProblemDetails
        {
            Type = InternalServerErrorType,
            Title = InternalServerErrorTitle,
            Status = StatusCodes.Status500InternalServerError,
            Detail = UnexpectedErrorDetail
        };

        problemDetails.Extensions[TraceIdExtensionName] = traceId;

        return problemDetails;
    }

    private static string GetTraceId(HttpContext httpContext)
    {
        return httpContext.TraceIdentifier;
    }
}
