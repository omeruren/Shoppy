namespace Shoppy.Business.Roles.DataTransferObjects;

public sealed record RolePermissionsDto(Guid RoleId, List<string> Permissions);

public sealed record UpdateRolePermissionsDto(List<string> Permissions);

public sealed record PermissionCatalogDto(string Name, string Group);
