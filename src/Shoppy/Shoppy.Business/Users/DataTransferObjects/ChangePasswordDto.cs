namespace Shoppy.Business.Users.DataTransferObjects;

/// <summary>
/// Used by a user to change their own password.
/// Requires the current password to prevent unauthorized changes.
/// </summary>
public sealed record ChangePasswordDto(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword);
