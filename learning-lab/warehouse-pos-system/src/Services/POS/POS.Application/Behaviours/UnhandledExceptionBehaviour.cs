using Common.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace POS.Application.Behaviours
{
    // Same pattern as Identity.Application's and Warehouse.Application's
    // behaviour of the same name — logs which command/query blew up with
    // full context, then rethrows so the exception still reaches
    // GlobalExceptionHandler (A2) to become a ProblemDetails response.
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
            catch (Exception ex) when (ex is not IHasStatusCode)
            {
                var requestName = typeof(TRequest).Name;
                _logger.LogError(ex, "Unhandled exception for request {RequestName} {@Request}", requestName, request);
                throw;
            }
        }
    }
}
