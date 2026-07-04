import type { ReactNode } from "react"
import { useHasPermission } from "@/hooks/useAuth"

interface PermissionGuardProps {
  permission: string
  children: ReactNode
  fallback?: ReactNode
}

export function PermissionGuard({
  permission,
  children,
  fallback = null,
}: PermissionGuardProps) {
  const hasPermission = useHasPermission(permission)
  return hasPermission ? <>{children}</> : <>{fallback}</>
}
