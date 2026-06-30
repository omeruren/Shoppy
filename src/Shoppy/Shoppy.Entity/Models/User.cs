using Microsoft.AspNetCore.Identity;

namespace Shoppy.Entity.Models;

public sealed class User : IdentityUser<Guid>
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string FullName { get; set; } = default!;

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public string? PasswordResetCode { get; set; }
    public DateTimeOffset? PasswordResetCodeExpires { get; set; }

    // CREATE USER
    public static User Create(string firstName, string lastName, string userName, string email)
    {
        return new User
        {
            FirstName = firstName,
            LastName = lastName,
            UserName = userName,
            Email = email,
            FullName = $"{firstName} {lastName}"
        };
    }


    // UPDATE USER
    public void Update(string firstName, string lastName, string userName, string email)
    {

        FirstName = firstName;
        LastName = lastName;
        UserName = userName;
        Email = email;
        FullName = $"{firstName} {lastName}";
    }


    // DELETE USER
    public void Delete()
    {
        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
    }


    // GENERATE PASSWORD RESET CODE
    public void GeneratePasswordResetCode()
    {
        // We'll use cryptographically secure random number generator instead of Random number generator.

        PasswordResetCode = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        PasswordResetCodeExpires = DateTimeOffset.UtcNow.AddMinutes(15);
    }

    // CLEAR PASSWORD RESET CODE
    public void ClearPasswordResetCode()
    {
        PasswordResetCode = null;
        PasswordResetCodeExpires = null;
    }
}
