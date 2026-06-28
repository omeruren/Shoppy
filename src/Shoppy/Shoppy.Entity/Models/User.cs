using Microsoft.AspNetCore.Identity;

namespace Shoppy.Entity.Models;

public sealed class User : IdentityUser<Guid>
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string FullName { get; set; } = default!;

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }


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
}
