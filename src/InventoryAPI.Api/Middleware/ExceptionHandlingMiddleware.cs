using System.Net;
using System.Text.Json;
using FluentValidation;
using InventoryAPI.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Api.Middleware;

/// <summary>
/// Global exception handling middleware.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            if (exception is DomainException
                or FluentValidation.ValidationException
                or UnauthorizedAccessException
                or BadHttpRequestException
                or DbUpdateConcurrencyException
                or DbUpdateException)
            {
                _logger.LogWarning(
                    exception,
                    "Request failed with a handled application exception. TraceId: {TraceId}",
                    context.TraceIdentifier);
            }
            else
            {
                _logger.LogError(
                    exception,
                    "Unhandled request exception. TraceId: {TraceId}",
                    context.TraceIdentifier);
            }

            await HandleExceptionAsync(context, exception);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/problem+json";

        var errorResponse = new ErrorResponse
        {
            Message = exception.Message,
            TraceId = context.TraceIdentifier
        };

        switch (exception)
        {
            case NotFoundException:
                response.StatusCode = StatusCodes.Status404NotFound;
                break;

            case InventoryAPI.Domain.Exceptions.ValidationException validationException:
                response.StatusCode = StatusCodes.Status400BadRequest;
                errorResponse.Errors = validationException.Errors;
                break;

            case FluentValidation.ValidationException fluentValidationException:
                response.StatusCode = StatusCodes.Status400BadRequest;
                errorResponse.Errors = fluentValidationException.Errors
                    .GroupBy(error => error.PropertyName)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(error => error.ErrorMessage).ToArray());
                break;

            case IdempotencyConflictException:
            case ConcurrencyConflictException:
                response.StatusCode = StatusCodes.Status409Conflict;
                break;

            case BusinessRuleViolationException:
                response.StatusCode = StatusCodes.Status400BadRequest;
                break;

            case UnauthorizedAccessException:
                response.StatusCode = StatusCodes.Status401Unauthorized;
                errorResponse.Message = "Authentication failed.";
                break;

            case BadHttpRequestException badHttpRequestException:
                response.StatusCode = badHttpRequestException.StatusCode;
                errorResponse.Message = badHttpRequestException.StatusCode == StatusCodes.Status413PayloadTooLarge
                    ? "Request body exceeds the allowed size."
                    : "The request could not be processed.";
                break;

            case DbUpdateConcurrencyException:
                response.StatusCode = StatusCodes.Status409Conflict;
                errorResponse.Message = "The record was modified by another user. Refresh and retry.";
                break;

            case DbUpdateException:
                response.StatusCode = StatusCodes.Status409Conflict;
                errorResponse.Message = "The requested change conflicts with the current database state.";
                break;

            default:
                response.StatusCode = StatusCodes.Status500InternalServerError;
                errorResponse.Message = "An internal server error occurred.";
                break;
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await response.WriteAsync(JsonSerializer.Serialize(errorResponse, jsonOptions));
    }
}

/// <summary>
/// Standard error response format.
/// </summary>
public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public string? TraceId { get; set; }
    public IDictionary<string, string[]>? Errors { get; set; }
}
