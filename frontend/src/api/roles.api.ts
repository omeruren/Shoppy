import { client } from "@/api/client"
import type { ResourceListQueryParams } from "@/hooks/useResourceListState"
import type { ApiResult, PaginationResultDto } from "@/types/api.types"
import type {
  PermissionCatalogItem,
  RoleCreateDto,
  RoleResultDto,
  RoleUpdateDto,
} from "@/types/role.types"

export async function getRoles(params: ResourceListQueryParams) {
  const { data } = await client.get<ApiResult<PaginationResultDto<RoleResultDto>>>(
    "/roles",
    { params }
  )
  return data
}

export async function createRole(request: RoleCreateDto) {
  const { data } = await client.post<ApiResult<string>>("/roles", request)
  return data
}

export async function updateRole(request: RoleUpdateDto) {
  const { data } = await client.put<ApiResult<string>>(
    `/roles/${request.id}`,
    request
  )
  return data
}

export async function deleteRole(id: string) {
  const { data } = await client.delete<ApiResult<string>>(`/roles/${id}`)
  return data
}

export async function getRolePermissions(roleId: string) {
  const { data } = await client.get<ApiResult<string[]>>(
    `/roles/${roleId}/permissions`
  )
  return data
}

export async function updateRolePermissions(
  roleId: string,
  permissions: string[]
) {
  const { data } = await client.put<ApiResult<string>>(
    `/roles/${roleId}/permissions`,
    { permissions }
  )
  return data
}

export async function getPermissionCatalog() {
  const { data } =
    await client.get<ApiResult<PermissionCatalogItem[]>>("/permissions")
  return data
}
