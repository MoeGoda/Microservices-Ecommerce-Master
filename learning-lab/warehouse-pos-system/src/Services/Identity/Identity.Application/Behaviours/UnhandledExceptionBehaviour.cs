using MediatR;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Behaviours
{
    // Logs *which command/query* blew up with full context before rethrowing,
    // so the exception still reaches BuildingBlocks' global exception
    // middleware (A2) to become a ProblemDetails response — this behaviour
    // only adds a log line, it never swallows the error.
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
            catch (Exception ex) when (ex is not Exceptions.ValidationException and not Exceptions.AuthenticationException)
            {
                var requestName = typeof(TRequest).Name;
                _logger.LogError(ex, "Unhandled exception for request {RequestName} {@Request}", requestName, request);
                throw;
            }
        }
    }
}
