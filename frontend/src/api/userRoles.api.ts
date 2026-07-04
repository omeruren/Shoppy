import { client } from "@/api/client"
import type { ApiResult } from "@/types/api.types"
import type { UserRoleDto } from "@/types/userRole.types"

export async function getUserRoles() {
  const { data } = await client.get<ApiResult<UserRoleDto[]>>("/user-roles")
  return data
}

export async function assignUserRole(request: UserRoleDto) {
  const { data } = await client.post<ApiResult<string>>(
    "/user-roles",
    request
  )
  return data
}

export async function removeUserRole(userId: string, roleId: string) {
  const { data } = await client.delete<ApiResult<string>>(
    `/user-roles/${userId}/${roleId}`
  )
  return data
}
