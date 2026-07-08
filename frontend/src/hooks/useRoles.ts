import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import {
  createRole,
  deleteRole,
  getPermissionCatalog,
  getRolePermissions,
  getRoles,
  updateRole,
  updateRolePermissions,
} from "@/api/roles.api"
import { handleApiError } from "@/lib/handle-api-error"

const roleKeys = {
  all: ["roles"] as const,
  permissions: (roleId: string) => ["roles", roleId, "permissions"] as const,
}

const permissionCatalogKey = ["permissions", "catalog"] as const

export function useRolesQuery() {
  return useQuery({
    queryKey: roleKeys.all,
    queryFn: getRoles,
  })
}

export function useCreateRole() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: createRole,
    onSuccess: () => {
      toast.success("Rol oluşturuldu.")
      queryClient.invalidateQueries({ queryKey: roleKeys.all })
    },
    onError: handleApiError,
  })
}

export function useUpdateRole() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: updateRole,
    onSuccess: () => {
      toast.success("Rol güncellendi.")
      queryClient.invalidateQueries({ queryKey: roleKeys.all })
    },
    onError: handleApiError,
  })
}

export function useDeleteRole() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: deleteRole,
    onSuccess: () => {
      toast.success("Rol silindi.")
      queryClient.invalidateQueries({ queryKey: roleKeys.all })
    },
    onError: handleApiError,
  })
}

export function usePermissionCatalogQuery() {
  return useQuery({
    queryKey: permissionCatalogKey,
    queryFn: getPermissionCatalog,
    staleTime: Infinity,
  })
}

export function useRolePermissionsQuery(roleId: string | undefined) {
  return useQuery({
    queryKey: roleKeys.permissions(roleId ?? ""),
    queryFn: () => getRolePermissions(roleId!),
    enabled: !!roleId,
  })
}

export function useUpdateRolePermissions() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({
      roleId,
      permissions,
    }: {
      roleId: string
      permissions: string[]
    }) => updateRolePermissions(roleId, permissions),
    onSuccess: (_data, variables) => {
      toast.success("Yetkiler güncellendi.")
      queryClient.invalidateQueries({
        queryKey: roleKeys.permissions(variables.roleId),
      })
    },
    onError: handleApiError,
  })
}
