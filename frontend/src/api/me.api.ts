import { client } from "@/api/client"
import type { ApiResult } from "@/types/api.types"
import type {
  ChangePasswordDto,
  UserProfileDto,
  UserUpdateSelfDto,
} from "@/types/user.types"

export async function getMe() {
  const { data } = await client.get<ApiResult<UserProfileDto>>("/users/me")
  return data
}

export async function updateMe(request: UserUpdateSelfDto) {
  const { data } = await client.put<ApiResult<string>>("/users/me", request)
  return data
}

export async function changePassword(request: ChangePasswordDto) {
  const { data } = await client.post<ApiResult<string>>(
    "/users/me/change-password",
    request
  )
  return data
}
