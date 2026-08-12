using FluentValidation;
using MediatR;
using ValidationException = Common.Exceptions.ValidationException;

namespace Warehouse.Application.Behaviours
{
    // Same pattern as Identity.Application's behaviour of the same name
    // (A1) — runs every registered FluentValidation validator for TRequest
    // before the real handler, so handlers stay free of manual
    // "if (string.IsNullOrEmpty(...))" input-shape checks. Existence/business
    // checks (does this Sku already exist, does this Category exist) stay
    // in the handler, same as Identity's RegisterCommandHandler — this
    // behaviour only ever rejects malformed input, never queries the database.
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
                    throw new ValidationException(failures);
                }
            }

            return await next();
        }
    }
}
