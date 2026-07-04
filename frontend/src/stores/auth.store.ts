import { create } from "zustand"
import { decodeAccessToken } from "@/lib/jwt"
import type { AuthUser } from "@/types/auth.types"

export const REFRESH_TOKEN_STORAGE_KEY = "shoppy.refreshToken"

interface AuthState {
  accessToken: string | null
  user: AuthUser | null
  setSession: (accessToken: string, refreshToken: string) => void
  logout: () => void
}

export const useAuthStore = create<AuthState>((set) => ({
  accessToken: null,
  user: null,

  setSession: (accessToken, refreshToken) => {
    localStorage.setItem(REFRESH_TOKEN_STORAGE_KEY, refreshToken)
    set({ accessToken, user: decodeAccessToken(accessToken) })
  },

  logout: () => {
    localStorage.removeItem(REFRESH_TOKEN_STORAGE_KEY)
    set({ accessToken: null, user: null })
  },
}))

export function getStoredRefreshToken(): string | null {
  return localStorage.getItem(REFRESH_TOKEN_STORAGE_KEY)
}
