namespace Shoppy.Business.Permissions;

/// <summary>
/// Static permission constants used throughout the application.
/// Each permission is formatted as "Group.Action" and is embedded
/// into JWT tokens as claims, enabling fine-grained access control.
/// </summary>
public static class Permissions
{
    public static class Users
    {
        public const string Read           = "Users.Read";
        public const string Create         = "Users.Create";
        public const string Update         = "Users.Update";
        public const string Delete         = "Users.Delete";
        public const string UpdateSelf     = "Users.UpdateSelf";
        public const string ChangePassword = "Users.ChangePassword";
    }

    public static class Roles
    {
        public const string Read   = "Roles.Read";
        public const string Create = "Roles.Create";
        public const string Update = "Roles.Update";
        public const string Delete = "Roles.Delete";
    }

    public static class Orders
    {
        public const string Read   = "Orders.Read";
        public const string Create = "Orders.Create";
        public const string Update = "Orders.Update";
        public const string Delete = "Orders.Delete";
    }

    public static class OrderItems
    {
        public const string Read   = "OrderItems.Read";
        public const string Create = "OrderItems.Create";
        public const string Update = "OrderItems.Update";
        public const string Delete = "OrderItems.Delete";
    }

    public static class Products
    {
        public const string Read   = "Products.Read";
        public const string Create = "Products.Create";
        public const string Update = "Products.Update";
        public const string Delete = "Products.Delete";
    }

    public static class Categories
    {
        public const string Read   = "Categories.Read";
        public const string Create = "Categories.Create";
        public const string Update = "Categories.Update";
        public const string Delete = "Categories.Delete";
    }

    public static class UserRoles
    {
        public const string Read   = "UserRoles.Read";
        public const string Create = "UserRoles.Create";
        public const string Delete = "UserRoles.Delete";
    }

    /// <summary>
    /// Returns all defined permissions as a flat list.
    /// Used for seeding RolePermission mappings.
    /// </summary>
    public static IReadOnlyList<string> GetAll() =>
    [
        Users.Read, Users.Create, Users.Update, Users.Delete,
        Users.UpdateSelf, Users.ChangePassword,

        Roles.Read, Roles.Create, Roles.Update, Roles.Delete,

        Orders.Read, Orders.Create, Orders.Update, Orders.Delete,

        OrderItems.Read, OrderItems.Create, OrderItems.Update, OrderItems.Delete,

        Products.Read, Products.Create, Products.Update, Products.Delete,

        Categories.Read, Categories.Create, Categories.Update, Categories.Delete,

        UserRoles.Read, UserRoles.Create, UserRoles.Delete,
    ];

    /// <summary>
    /// Permissions assigned to the built-in Admin role.
    /// Admins get everything.
    /// </summary>
    public static IReadOnlyList<string> GetAdminPermissions() => GetAll();

    /// <summary>
    /// Permissions assigned to the built-in Customer role.
    /// Customers can browse products/categories and manage their own orders.
    /// </summary>
    public static IReadOnlyList<string> GetCustomerPermissions() =>
    [
        Products.Read,
        Categories.Read,
        Orders.Read, Orders.Create,
        OrderItems.Read, OrderItems.Create,
        Users.UpdateSelf, Users.ChangePassword,
    ];
}
