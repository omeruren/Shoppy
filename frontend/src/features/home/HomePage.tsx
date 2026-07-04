import { useAuth } from "@/hooks/useAuth"
import { Button } from "@/components/ui/button"

export function HomePage() {
  const { user, logout } = useAuth()

  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-4 px-4">
      <p className="text-lg font-medium">Hoş geldin, {user?.fullName}</p>
      <p className="text-sm text-text-secondary">
        Roller: {user?.roles.join(", ") || "—"}
      </p>
      <Button variant="outline" onClick={logout}>
        Çıkış Yap
      </Button>
    </div>
  )
}
