namespace Identity.Application.Exceptions
{
    // Deliberately vague message ("invalid username or password") regardless
    // of *which* check failed (unknown user vs wrong password). Reporting
    // "user not found" vs "wrong password" separately would let an attacker
    // enumerate valid usernames — a real security consideration, not
    // theoretical.
    public class AuthenticationException : Exception
    {
        public AuthenticationException(string message) : base(message) { }
    }
}
