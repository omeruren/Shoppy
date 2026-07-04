import { client } from "@/api/client"
import type { ApiResult } from "@/types/api.types"
import type {
  RoleCreateDto,
  RoleResultDto,
  RoleUpdateDto,
} from "@/types/role.types"

export async function getRoles() {
  const { data } =
    await client.get<ApiResult<RoleResultDto[]>>("/roles")
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
