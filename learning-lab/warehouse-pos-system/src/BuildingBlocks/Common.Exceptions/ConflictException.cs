namespace Common.Exceptions
{
    // For "this would violate a uniqueness/state rule" failures — a
    // duplicate username, a barcode that already exists, checking out a
    // basket that's already been checked out. Distinct from ValidationException
    // (which is about the *shape* of the input) and NotFoundException (which
    // is about a missing resource) — this is about a resource that exists
    // but conflicts with the request.
    public class ConflictException : Exception, IHasStatusCode
    {
        public int StatusCode => 409;

        public ConflictException(string message) : base(message)
        {
        }
    }
}
