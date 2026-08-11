using FluentValidation.Results;

namespace Identity.Application.Exceptions
{
    // Thrown by the MediatR ValidationBehaviour (see Behaviours/) when
    // FluentValidation rejects a command *before* the handler ever runs.
    // Errors is grouped by property name so the API layer can turn this into
    // a field-by-field 400 response instead of one flat error string.
    public class ValidationException : Exception
    {
        public IDictionary<string, string[]> Errors { get; }

        public ValidationException()
            : base("One or more validation failures occurred.")
        {
            Errors = new Dictionary<string, string[]>();
        }

        public ValidationException(IEnumerable<ValidationFailure> failures) : this()
        {
            Errors = failures
                .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
                .ToDictionary(g => g.Key, g => g.ToArray());
        }
    }
}
