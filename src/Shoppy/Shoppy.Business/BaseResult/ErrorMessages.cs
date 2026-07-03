namespace Shoppy.Business.BaseResult;

public static class ErrorMessages
{
    public static class Product
    {
        public const string NotFound = "Product not found.";
        public const string AlreadyExists = "Product already exists.";
    }

    public static class Category
    {
        public const string NotFound = "Category not found.";
        public const string AlreadyExists = "Category already exists.";
    }

    public static class Order
    {
        public const string NotFound = "Order not found.";
    }

    public static class OrderItem
    {
        public const string NotFound = "Order item not found.";
    }

    public static class Role
    {
        public const string NotFound = "Role not found.";
        public const string AlreadyExists = "Role already exists.";
        public const string NameAlreadyExists = "Role name already exists.";
    }

    public static class User
    {
        public const string NotFound = "User not found.";
        public const string EmailAlreadyTaken = "This email is already taken.";
    }

    public static class UserRole
    {
        public const string NotFound = "User role not found.";
    }

    public static class Auth
    {
        public const string InvalidCredentials = "User name or password is incorrect.";
        public const string InvalidRefreshToken = "Invalid refresh token.";
        public const string RefreshTokenRevoked = "Refresh token has been revoked.";
        public const string RefreshTokenFamilyRevoked = "Refresh token reuse detected; all sessions in this family have been revoked.";
        public const string RefreshTokenExpired = "Refresh token has expired.";
        public const string AccountDeactivated = "User account is deactivated.";
        public const string AccountLockedOut = "Account is temporarily locked due to multiple failed login attempts. Please try again later.";
        public const string InvalidResetCode = "Invalid reset code.";
        public const string PasswordMismatch = "New password and confirmation do not match.";
        public const string EmailSendFailure = "Failed to send password reset email. Please try again later.";
    }
}
