import { client } from "@/api/client"
import type { ApiResult } from "@/types/api.types"
import type {
  ForgotPasswordRequestDto,
  LoginRequestDto,
  LoginResponseDto,
  RefreshTokenRequestDto,
  ResetPasswordRequestDto,
} from "@/types/auth.types"

export async function login(request: LoginRequestDto) {
  const { data } = await client.post<ApiResult<LoginResponseDto>>(
    "/auth/login",
    request
  )
  return data
}

/** Not used by the response interceptor (client.ts calls the endpoint directly
 * to avoid a circular import) — for any explicit/manual refresh call sites. */
export async function refresh(request: RefreshTokenRequestDto) {
  const { data } = await client.post<ApiResult<LoginResponseDto>>(
    "/auth/refresh",
    request
  )
  return data
}

export async function forgotPassword(request: ForgotPasswordRequestDto) {
  const { data } = await client.post<ApiResult<string>>(
    "/auth/forgot-password",
    request
  )
  return data
}

export async function resetPassword(request: ResetPasswordRequestDto) {
  const { data } = await client.post<ApiResult<string>>(
    "/auth/reset-password",
    request
  )
  return data
}
