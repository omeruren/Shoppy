namespace Shoppy.Business.Users.DataTransferObjects;

public sealed record UserUpdateDto(
    Guid Id,
    string FirstName,
    string LastName,
    string UserName,
    string Email,
    string Password);