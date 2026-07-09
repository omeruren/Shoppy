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
import type { ResourceListQueryParams } from "@/hooks/useResourceListState"
import { handleApiError } from "@/lib/handle-api-error"

const roleKeys = {
  all: ["roles"] as const,
  list: (params: ResourceListQueryParams) =>
    [...roleKeys.all, "list", params] as const,
  permissions: (roleId: string) => ["roles", roleId, "permissions"] as const,
}

const permissionCatalogKey = ["permissions", "catalog"] as const

export function useRolesQuery(params: ResourceListQueryParams) {
  return useQuery({
    queryKey: roleKeys.list(params),
    queryFn: () => getRoles(params),
    placeholderData: (previousData) => previousData,
  })
}

/** Fetches a large, unfiltered page of roles for use in checklists elsewhere
 * (e.g. the user role-assignment dialog) rather than the paginated admin list. */
export function useRoleOptions() {
  const { data, isLoading } = useRolesQuery({ pageNumber: 1, pageSize: 100 })

  return { roles: data?.data?.data ?? [], isLoading }
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
