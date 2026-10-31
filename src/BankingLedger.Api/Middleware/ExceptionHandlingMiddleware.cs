using System.Net;
using System.Text.Json;
using BankingLedger.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace BankingLedger.Api.Middleware;

/// <summary>
/// Global exception handling middleware producing standard RFC 7807 Problem Details.
/// 
/// Why this matters to PMs and API Clients:
/// Frontends and third-party consumers need consistent, machine-readable error responses.
/// Rather than throwing unexpected 500 HTML pages or raw stack traces, every failure
/// translates into a clear HTTP status code with explanatory error messages:
/// - 400 Bad Request: Missing or malformed inputs
/// - 404 Not Found: Account does not exist
/// - 409 Conflict: Duplicate transaction reference_id (preventing double spend)
/// - 422 Unprocessable Entity: Insufficient account balance
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        var problemDetails = new ProblemDetails
        {
            Instance = context.Request.Path,
            Status = (int)statusCode
        };

        switch (exception)
        {
            case ValidationException valEx:
                statusCode = HttpStatusCode.BadRequest;
                problemDetails.Title = "Validation Failed";
                problemDetails.Detail = "One or more validation errors occurred with your request.";
                problemDetails.Status = (int)statusCode;
                problemDetails.Extensions["errors"] = valEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => string.IsNullOrEmpty(g.Key) ? "general" : g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );
                _logger.LogWarning("Validation failure on {Path}: {Errors}", context.Request.Path, problemDetails.Extensions["errors"]);
                break;

            case AccountNotFoundException notFoundEx:
                statusCode = HttpStatusCode.NotFound;
                problemDetails.Title = "Account Not Found";
                problemDetails.Detail = notFoundEx.Message;
                problemDetails.Status = (int)statusCode;
                _logger.LogWarning("Resource not found: {Message}", notFoundEx.Message);
                break;

            case DuplicateReferenceException dupEx:
                statusCode = HttpStatusCode.Conflict;
                problemDetails.Title = "Duplicate Reference ID";
                problemDetails.Detail = dupEx.Message;
                problemDetails.Status = (int)statusCode;
                problemDetails.Extensions["referenceId"] = dupEx.ReferenceId;
                _logger.LogWarning("Duplicate transaction conflict: {Message}", dupEx.Message);
                break;

            case InsufficientBalanceException balEx:
                statusCode = HttpStatusCode.UnprocessableEntity;
                problemDetails.Title = "Insufficient Balance";
                problemDetails.Detail = balEx.Message;
                problemDetails.Status = (int)statusCode;
                problemDetails.Extensions["currentBalance"] = balEx.CurrentBalance;
                problemDetails.Extensions["requestedAmount"] = balEx.RequestedAmount;
                _logger.LogWarning("Insufficient balance: {Message}", balEx.Message);
                break;

            case SelfTransferException selfEx:
                statusCode = HttpStatusCode.BadRequest;
                problemDetails.Title = "Invalid Transfer";
                problemDetails.Detail = selfEx.Message;
                problemDetails.Status = (int)statusCode;
                _logger.LogWarning("Invalid self-transfer: {Message}", selfEx.Message);
                break;

            case InvalidTransactionException invEx:
                statusCode = HttpStatusCode.BadRequest;
                problemDetails.Title = "Invalid Transaction Data";
                problemDetails.Detail = invEx.Message;
                problemDetails.Status = (int)statusCode;
                _logger.LogWarning("Invalid transaction: {Message}", invEx.Message);
                break;

            case BadHttpRequestException badHttpEx:
                statusCode = HttpStatusCode.BadRequest;
                problemDetails.Title = "Invalid Request Body";
                problemDetails.Detail = badHttpEx.Message;
                problemDetails.Status = (int)statusCode;
                _logger.LogWarning("Bad HTTP request: {Message}", badHttpEx.Message);
                break;

            default:
                _logger.LogError(exception, "Unhandled exception occurred while processing request to {Path}", context.Request.Path);
                problemDetails.Title = "Internal Server Error";
                problemDetails.Detail = "An unexpected error occurred while processing your request. Please try again later.";
                problemDetails.Status = (int)HttpStatusCode.InternalServerError;
                break;
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        await context.Response.WriteAsync(json);
    }
}
