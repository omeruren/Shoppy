import { useAuth } from "@/hooks/useAuth"

export function AdminDashboardPage() {
  const { user } = useAuth()

  return (
    <div className="flex flex-col gap-2">
      <h1 className="text-2xl font-semibold tracking-tight">
        Hoş geldin, {user?.fullName}
      </h1>
      <p className="text-text-secondary">
        Soldaki menüden ürünleri, kategorileri, siparişleri, kullanıcıları ve
        rolleri yönetebilirsin.
      </p>
    </div>
  )
}
