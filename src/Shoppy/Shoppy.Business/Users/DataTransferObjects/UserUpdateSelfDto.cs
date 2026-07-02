namespace Shoppy.Business.Users.DataTransferObjects;

/// <summary>
/// Used by a user to update their own profile.
/// Does NOT allow changing email — admin-only via UserUpdateDto.
/// </summary>
public sealed record UserUpdateSelfDto(
    string FirstName,
    string LastName,
    string UserName);
