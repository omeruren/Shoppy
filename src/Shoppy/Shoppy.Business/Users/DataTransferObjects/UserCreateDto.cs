namespace Shoppy.Business.Users.DataTransferObjects;

public sealed record UserCreateDto(
    string FirstName,
    string LastName,
    string UserName,
    string Email,
    string Password);
