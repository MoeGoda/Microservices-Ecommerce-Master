using FluentValidation.Results;

namespace Common.Exceptions
{
    // Thrown by every service's MediatR ValidationBehaviour when
    // FluentValidation rejects a command before its handler runs. Grouped by
    // property name so the client gets a field-by-field 400, not one flat
    // error string it has to parse itself.
    public class ValidationException : Exception, IHasStatusCode
    {
        public int StatusCode => 400;

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
