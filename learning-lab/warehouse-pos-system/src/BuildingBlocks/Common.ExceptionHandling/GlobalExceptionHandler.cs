using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Common.Exceptions;

namespace Common.ExceptionHandling
{
    // ASP.NET Core 8's IExceptionHandler: registered once per service via
    // AddCommonExceptionHandling(), then app.UseExceptionHandler() dispatches
    // every unhandled exception here instead of each controller needing its
    // own try/catch. One handler, every controller in every microservice
    // that references this library gets the same consistent error shape.
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IProblemDetailsService _problemDetailsService;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetailsService)
        {
            _logger = logger;
            _problemDetailsService = problemDetailsService;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var statusCode = exception is IHasStatusCode hasStatusCode
                ? hasStatusCode.StatusCode
                : StatusCodes.Status500InternalServerError;

            // Anything below 500 is an expected, "the caller did something
            // wrong" outcome (bad input, not found, bad credentials) — worth
            // a log line, not a paged-alert-worthy error. 500+ means our own
            // code broke in a way nobody anticipated, so it gets the full
            // exception (stack trace included) logged server-side.
            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "Unhandled exception processing {Method} {Path}",
                    httpContext.Request.Method, httpContext.Request.Path);
            }
            else
            {
                _logger.LogWarning("{ExceptionType} handling {Method} {Path}: {Message}",
                    exception.GetType().Name, httpContext.Request.Method, httpContext.Request.Path, exception.Message);
            }

            httpContext.Response.StatusCode = statusCode;

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = exception.GetType().Name,
                // Never echo the raw message of a genuinely unexpected (500)
                // exception back to the caller — it can contain internal
                // details (connection strings in an exception message,
                // stack-adjacent state) that have no business leaving the
                // server. Expected exceptions (400/401/404) carry messages
                // that were written to be client-safe in the first place.
                Detail = statusCode >= StatusCodes.Status500InternalServerError
                    ? "An unexpected error occurred. Please try again later."
                    : exception.Message,
                Instance = httpContext.Request.Path
            };

            if (exception is ValidationException validationException)
            {
                problemDetails.Extensions["errors"] = validationException.Errors;
            }

            // WriteAsync returns ValueTask, not ValueTask<bool> — it doesn't
            // report whether it actually wrote a body (e.g. a HEAD request
            // has none). Returning true tells UseExceptionHandler() "this
            // exception has been handled, don't fall through to the default
            // 500 page."
            await _problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problemDetails,
                Exception = exception
            });

            return true;
        }
    }
}
