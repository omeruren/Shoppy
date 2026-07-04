export interface LoginRequestDto {
  userName: string
  password: string
}

export interface LoginResponseDto {
  accessToken: string
  refreshToken: string
  expiresAt: string
}

export interface RefreshTokenRequestDto {
  refreshToken: string
}

export interface ForgotPasswordRequestDto {
  email: string
}

export interface ResetPasswordRequestDto {
  email: string
  code: string
  newPassword: string
}

export interface AuthUser {
  id: string
  userName: string
  fullName: string
  email: string
  roles: string[]
  permissions: string[]
}

/** Raw JWT payload shape as emitted by JwtProvider.CreateToken (Shoppy.Business/Auth/JwtProvider.cs). */
export interface DecodedAccessToken {
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier": string
  userName: string
  fullName: string
  email: string
  role?: string | string[]
  permission?: string | string[]
  exp: number
}
