using Common.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Behaviours
{
    // Logs *which command/query* blew up with full context before rethrowing,
    // so the exception still reaches Common.ExceptionHandling's
    // GlobalExceptionHandler (A2) to become a ProblemDetails response — this
    // behaviour only adds a log line, it never swallows the error.
    public class UnhandledExceptionBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<UnhandledExceptionBehaviour<TRequest, TResponse>> _logger;

        public UnhandledExceptionBehaviour(ILogger<UnhandledExceptionBehaviour<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            try
            {
                return await next();
            }
            // Anything implementing IHasStatusCode is an *expected* failure
            // (bad input, not found, bad credentials) that GlobalExceptionHandler
            // already knows how to turn into a clean 4xx — logging it here as
            // an "unhandled exception" would be noise. Only genuinely
            // unexpected exceptions get the ILogger.LogError treatment.
            catch (Exception ex) when (ex is not IHasStatusCode)
            {
                var requestName = typeof(TRequest).Name;
                _logger.LogError(ex, "Unhandled exception for request {RequestName} {@Request}", requestName, request);
                throw;
            }
        }
    }
}
