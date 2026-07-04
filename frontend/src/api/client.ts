import axios from "axios"
import type { AxiosError, InternalAxiosRequestConfig } from "axios"
import { toast } from "sonner"
import { getStoredRefreshToken, useAuthStore } from "@/stores/auth.store"
import type { ApiResult } from "@/types/api.types"
import type { LoginResponseDto } from "@/types/auth.types"

const AUTH_REFRESH_PATH = "/auth/refresh"

declare module "axios" {
  export interface InternalAxiosRequestConfig {
    _retry?: boolean
  }
}

export const client = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5226/api/v1",
  headers: {
    "Content-Type": "application/json",
  },
})

client.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const { accessToken } = useAuthStore.getState()
  if (accessToken) {
    config.headers.set("Authorization", `Bearer ${accessToken}`)
  }
  return config
})

let isRefreshing = false
let refreshWaiters: Array<(accessToken: string | null) => void> = []

function resolveWaiters(accessToken: string | null) {
  refreshWaiters.forEach((resolve) => resolve(accessToken))
  refreshWaiters = []
}

function redirectToLogin() {
  if (window.location.pathname !== "/login") {
    window.location.assign("/login")
  }
}

client.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ApiResult<unknown>>) => {
    const { response, config } = error

    if (!response || !config) {
      return Promise.reject(error)
    }

    if (response.status === 429) {
      toast.error("Çok fazla istek gönderildi. Lütfen bekleyin.")
      return Promise.reject(error)
    }

    const isAuthEndpoint = config.url?.includes("/auth/")
    if (response.status !== 401 || config._retry || isAuthEndpoint) {
      return Promise.reject(error)
    }

    const refreshToken = getStoredRefreshToken()
    if (!refreshToken) {
      useAuthStore.getState().logout()
      redirectToLogin()
      return Promise.reject(error)
    }

    config._retry = true

    if (isRefreshing) {
      const accessToken = await new Promise<string | null>((resolve) => {
        refreshWaiters.push(resolve)
      })
      if (!accessToken) {
        return Promise.reject(error)
      }
      config.headers.set("Authorization", `Bearer ${accessToken}`)
      return client(config)
    }

    isRefreshing = true
    try {
      const { data } = await client.post<ApiResult<LoginResponseDto>>(
        AUTH_REFRESH_PATH,
        { refreshToken }
      )
      if (!data.isSuccessful || !data.data) {
        throw new Error("Refresh failed")
      }

      useAuthStore
        .getState()
        .setSession(data.data.accessToken, data.data.refreshToken)
      resolveWaiters(data.data.accessToken)

      config.headers.set("Authorization", `Bearer ${data.data.accessToken}`)
      return client(config)
    } catch (refreshError) {
      resolveWaiters(null)
      useAuthStore.getState().logout()
      redirectToLogin()
      return Promise.reject(refreshError)
    } finally {
      isRefreshing = false
    }
  }
)
