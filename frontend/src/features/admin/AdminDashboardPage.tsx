import { useAuth } from "@/hooks/useAuth"
import { Button } from "@/components/ui/button"

export function AdminDashboardPage() {
  const { user, logout } = useAuth()

  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-4 px-4">
      <p className="text-lg font-medium">Admin Paneli — {user?.fullName}</p>
      <p className="text-sm text-text-secondary">
        İzinler: {user?.permissions.length ?? 0}
      </p>
      <Button variant="outline" onClick={logout}>
        Çıkış Yap
      </Button>
    </div>
  )
}
