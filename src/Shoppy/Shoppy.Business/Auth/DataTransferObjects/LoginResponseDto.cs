namespace Shoppy.Business.Auth.DataTransferObjects;

public sealed record LoginResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt);