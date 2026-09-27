using LithographERP.Application.Modules.Authentication;
using LithographERP.Application.Modules.Calculator;
using Microsoft.AspNetCore.Diagnostics;

namespace LithographERP.Api.Errors;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is AuthException authException)
        {
            logger.LogInformation(
                "Request {Method} {Path} failed with {ErrorCode}. TraceIdentifier: {TraceIdentifier}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                authException.Code,
                httpContext.TraceIdentifier);

            httpContext.Response.StatusCode = authException.StatusCode;
            await httpContext.Response.WriteAsJsonAsync(
                new ApiErrorResponse(authException.Code, authException.Message, authException.Errors),
                cancellationToken);
            return true;
        }

        if (exception is CalculatorRequestException calculatorException)
        {
            logger.LogInformation(
                "Request {Method} {Path} failed with {ErrorCode}. TraceIdentifier: {TraceIdentifier}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                calculatorException.Code,
                httpContext.TraceIdentifier);

            httpContext.Response.StatusCode = calculatorException.StatusCode;
            await httpContext.Response.WriteAsJsonAsync(
                new ApiErrorResponse(
                    calculatorException.Code,
                    calculatorException.Message,
                    calculatorException.Errors,
                    calculatorException.Validation),
                cancellationToken);
            return true;
        }

        logger.LogError(
            exception,
            "Unhandled exception for {Method} {Path}. TraceIdentifier: {TraceIdentifier}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            httpContext.TraceIdentifier);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(
            new ApiErrorResponse("INTERNAL_SERVER_ERROR", "An unexpected error occurred."),
            cancellationToken);

        return true;
    }
}
