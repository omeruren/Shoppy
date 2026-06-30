namespace Shoppy.Business.Auth.DataTransferObjects;

public sealed record ResetPasswordRequestDto(string Email, string Code, string NewPassword);
