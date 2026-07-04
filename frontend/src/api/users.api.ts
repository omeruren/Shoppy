import { client } from "@/api/client"
import type { ApiResult, PaginationResultDto } from "@/types/api.types"
import type {
  UserCreateDto,
  UserProfileDto,
  UserUpdateDto,
} from "@/types/user.types"

export interface UsersListParams {
  pageNumber: number
  pageSize: number
  searchTerm?: string
}

export async function getUsers(params: UsersListParams) {
  const { data } = await client.get<
    ApiResult<PaginationResultDto<UserProfileDto>>
  >("/users", { params })
  return data
}

export async function createUser(request: UserCreateDto) {
  const { data } = await client.post<ApiResult<string>>("/users", request)
  return data
}

export async function updateUser(request: UserUpdateDto) {
  const { data } = await client.put<ApiResult<string>>(
    `/users/${request.id}`,
    request
  )
  return data
}

export async function deleteUser(id: string) {
  const { data } = await client.delete<ApiResult<string>>(`/users/${id}`)
  return data
}
