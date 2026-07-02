namespace Shoppy.Business.Users.DataTransferObjects;

/// <summary>
/// Read-only projection of a user's profile — returned by GET /users/me.
/// Does NOT expose PasswordHash or security-sensitive fields.
/// </summary>
public sealed record UserProfileDto(
    Guid   Id,
    string FirstName,
    string LastName,
    string FullName,
    string UserName,
    string Email);
