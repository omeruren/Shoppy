import { useAuthStore } from "@/stores/auth.store"

export function useAuth() {
  const accessToken = useAuthStore((state) => state.accessToken)
  const user = useAuthStore((state) => state.user)
  const logout = useAuthStore((state) => state.logout)

  return {
    user,
    isAuthenticated: accessToken !== null && user !== null,
    logout,
  }
}

export function useHasPermission(permission: string): boolean {
  return useAuthStore(
    (state) => state.user?.permissions.includes(permission) ?? false
  )
}

export function useHasRole(role: string): boolean {
  return useAuthStore((state) => state.user?.roles.includes(role) ?? false)
}
