import { jwtDecode } from "jwt-decode"
import type { AuthUser, DecodedAccessToken } from "@/types/auth.types"

const NAME_IDENTIFIER_CLAIM =
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"

/** ASP.NET serializes a claim type as a single string when only one value is
 * present, and as a JSON array when the same claim type repeats (role, permission). */
function toArray(claim: string | string[] | undefined): string[] {
  if (!claim) return []
  return Array.isArray(claim) ? claim : [claim]
}

export function decodeAccessToken(accessToken: string): AuthUser {
  const payload = jwtDecode<DecodedAccessToken>(accessToken)

  return {
    id: payload[NAME_IDENTIFIER_CLAIM],
    userName: payload.userName,
    fullName: payload.fullName,
    email: payload.email,
    roles: toArray(payload.role),
    permissions: toArray(payload.permission),
  }
}

export function isTokenExpired(accessToken: string): boolean {
  const { exp } = jwtDecode<DecodedAccessToken>(accessToken)
  return exp * 1000 <= Date.now()
}
