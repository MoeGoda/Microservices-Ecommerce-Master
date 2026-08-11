namespace Common.Exceptions
{
    // For business-rule authentication/authorization failures raised inside
    // a command handler (e.g. "wrong password", "cashier can't void a sale
    // over $500 without a manager override"). Framework-level auth failures
    // ([Authorize] rejecting a request before it reaches a handler at all)
    // never go through this — ASP.NET Core's authentication/authorization
    // middleware returns 401/403 directly and this exception is never thrown.
    public class UnauthorizedException : Exception, IHasStatusCode
    {
        public int StatusCode => 401;

        public UnauthorizedException(string message) : base(message)
        {
        }
    }
}
