import { Navigate, Outlet } from "react-router-dom"
import { useHasPermission } from "@/hooks/useAuth"

export function RequirePermission({ permission }: { permission: string }) {
  const hasPermission = useHasPermission(permission)

  if (!hasPermission) {
    return <Navigate to="/403" replace />
  }

  return <Outlet />
}
