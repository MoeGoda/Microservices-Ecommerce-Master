using FluentValidation;
using MediatR;

namespace Identity.Application.Behaviours
{
    // A MediatR "pipeline behaviour" wraps every command/query handler like
    // middleware wraps an HTTP request. This one runs all FluentValidation
    // validators registered for TRequest *before* calling the real handler
    // (`next`) — so CheckoutOrderCommandHandler-style handlers never have to
    // start with a wall of manual "if (string.IsNullOrEmpty(...))" checks.
    public class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (_validators.Any())
            {
                var context = new ValidationContext<TRequest>(request);

                var validationResults = await Task.WhenAll(
                    _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

                var failures = validationResults
                    .SelectMany(r => r.Errors)
                    .Where(f => f != null)
                    .ToList();

                if (failures.Count != 0)
                {
                    throw new Exceptions.ValidationException(failures);
                }
            }

            return await next();
        }
    }
}
