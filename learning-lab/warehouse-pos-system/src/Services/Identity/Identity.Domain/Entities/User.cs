using Identity.Domain.Common;

namespace Identity.Domain.Entities
{
    public class User : EntityBase
    {
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;

        // Never store or log the plain password. PasswordHash is produced by
        // Microsoft's PasswordHasher<T> (PBKDF2 under the hood) in the
        // Infrastructure layer — the Domain layer only ever sees the hash.
        public string PasswordHash { get; set; } = null!;

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;
    }
}
