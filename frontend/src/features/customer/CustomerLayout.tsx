import { useState } from "react"
import {
  MenuIcon,
  StoreIcon,
  ShoppingBagIcon,
  UserIcon,
} from "lucide-react"
import type { ComponentType } from "react"
import { NavLink, Outlet } from "react-router-dom"
import { Button } from "@/components/ui/button"
import { Sheet, SheetContent, SheetTrigger } from "@/components/ui/sheet"
import { CartDrawer } from "@/features/customer/CartDrawer"
import { useAuth } from "@/hooks/useAuth"
import { cn } from "@/lib/utils"

interface NavItem {
  label: string
  to: string
  icon: ComponentType<{ className?: string }>
  end?: boolean
}

const NAV_ITEMS: NavItem[] = [
  { label: "Ürünler", to: "/", icon: StoreIcon, end: true },
  { label: "Siparişlerim", to: "/orders", icon: ShoppingBagIcon },
  { label: "Profilim", to: "/profile", icon: UserIcon },
]

function CustomerNav({
  onNavigate,
}: {
  onNavigate?: () => void
}) {
  return (
    <nav className="flex flex-col gap-1">
      {NAV_ITEMS.map((item) => (
        <NavLink
          key={item.to}
          to={item.to}
          end={item.end}
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

export function CustomerLayout() {
  const { logout } = useAuth()
  const [mobileOpen, setMobileOpen] = useState(false)

  return (
    <div className="flex min-h-screen flex-col">
      <header className="flex items-center justify-between border-b border-border p-3">
        <div className="flex items-center gap-4">
          <Sheet open={mobileOpen} onOpenChange={setMobileOpen}>
            <SheetTrigger asChild>
              <Button variant="outline" size="icon-sm" className="md:hidden">
                <MenuIcon />
              </Button>
            </SheetTrigger>
            <SheetContent side="left" className="flex flex-col gap-4 p-4">
              <span className="text-lg font-semibold">Shoppy</span>
              <CustomerNav onNavigate={() => setMobileOpen(false)} />
            </SheetContent>
          </Sheet>

          <span className="text-lg font-semibold">Shoppy</span>

          <div className="hidden md:block">
            <nav className="flex items-center gap-1">
              {NAV_ITEMS.map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  end={item.end}
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
          </div>
        </div>

        <div className="flex items-center gap-2">
          <CartDrawer />
          <Button variant="outline" size="sm" onClick={logout}>
            Çıkış Yap
          </Button>
        </div>
      </header>

      <main className="flex-1 p-4 md:p-6">
        <Outlet />
      </main>
    </div>
  )
}
