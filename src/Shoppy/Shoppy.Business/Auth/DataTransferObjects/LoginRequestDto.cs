namespace Shoppy.Business.Auth.DataTransferObjects;

public sealed record LoginRequestDto(
    string UserName,
    string Password);
