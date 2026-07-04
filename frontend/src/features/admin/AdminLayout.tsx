import { useState } from "react"
import {
  LayoutDashboardIcon,
  MenuIcon,
  PackageIcon,
  ShieldIcon,
  ShoppingCartIcon,
  TagIcon,
  UsersIcon,
} from "lucide-react"
import type { ComponentType } from "react"
import { NavLink, Outlet } from "react-router-dom"
import { Button } from "@/components/ui/button"
import { Sheet, SheetContent, SheetTrigger } from "@/components/ui/sheet"
import { useAuth } from "@/hooks/useAuth"
import { cn } from "@/lib/utils"

interface NavItem {
  label: string
  to: string
  icon: ComponentType<{ className?: string }>
  permission?: string
}

const NAV_ITEMS: NavItem[] = [
  { label: "Dashboard", to: "/admin/dashboard", icon: LayoutDashboardIcon },
  {
    label: "Ürünler",
    to: "/admin/products",
    icon: PackageIcon,
    permission: "Products.Read",
  },
  {
    label: "Kategoriler",
    to: "/admin/categories",
    icon: TagIcon,
    permission: "Categories.Read",
  },
  {
    label: "Siparişler",
    to: "/admin/orders",
    icon: ShoppingCartIcon,
    permission: "Orders.Read",
  },
  {
    label: "Kullanıcılar",
    to: "/admin/users",
    icon: UsersIcon,
    permission: "Users.Read",
  },
  {
    label: "Roller",
    to: "/admin/roles",
    icon: ShieldIcon,
    permission: "Roles.Read",
  },
]

function AdminNav({
  items,
  onNavigate,
}: {
  items: NavItem[]
  onNavigate?: () => void
}) {
  return (
    <nav className="flex flex-col gap-1">
      {items.map((item) => (
        <NavLink
          key={item.to}
          to={item.to}
          onClick={onNavigate}
          className={({ isActive }) =>
            cn(
              "flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium transition-colors",
              isActive
                ? "bg-accent/10 text-accent"
                : "text-text-secondary hover:bg-bg-tertiary hover:text-text-primary"
            )
          }
        >
          <item.icon className="size-4" />
          {item.label}
        </NavLink>
      ))}
    </nav>
  )
}

export function AdminLayout() {
  const { user, logout } = useAuth()
  const [mobileOpen, setMobileOpen] = useState(false)

  const visibleItems = NAV_ITEMS.filter(
    (item) => !item.permission || user?.permissions.includes(item.permission)
  )

  return (
    <div className="flex min-h-screen">
      <aside className="hidden w-60 shrink-0 flex-col gap-4 border-r border-border p-4 md:flex">
        <span className="px-3 text-lg font-semibold">Shoppy Admin</span>
        <AdminNav items={visibleItems} />
        <Button variant="outline" size="sm" className="mt-auto" onClick={logout}>
          Çıkış Yap
        </Button>
      </aside>

      <div className="flex flex-1 flex-col">
        <header className="flex items-center justify-between border-b border-border p-3 md:hidden">
          <span className="text-lg font-semibold">Shoppy Admin</span>
          <Sheet open={mobileOpen} onOpenChange={setMobileOpen}>
            <SheetTrigger asChild>
              <Button variant="outline" size="icon-sm">
                <MenuIcon />
              </Button>
            </SheetTrigger>
            <SheetContent side="left" className="flex flex-col gap-4 p-4">
              <span className="text-lg font-semibold">Shoppy Admin</span>
              <AdminNav
                items={visibleItems}
                onNavigate={() => setMobileOpen(false)}
              />
              <Button
                variant="outline"
                size="sm"
                className="mt-auto"
                onClick={logout}
              >
                Çıkış Yap
              </Button>
            </SheetContent>
          </Sheet>
        </header>

        <main className="flex-1 p-4 md:p-6">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
