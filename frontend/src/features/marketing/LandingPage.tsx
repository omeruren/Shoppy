import {
  MoonIcon,
  PackageSearchIcon,
  ShieldCheckIcon,
  SunIcon,
  TruckIcon,
} from "lucide-react"
import { Link, Navigate } from "react-router-dom"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { useAuth } from "@/hooks/useAuth"
import { useTheme } from "@/providers/ThemeProvider"

const FEATURES = [
  {
    icon: PackageSearchIcon,
    title: "Geniş Ürün Yelpazesi",
    description:
      "Elektronikten modaya, kitaplardan ev eşyalarına kadar ihtiyacınız olan her şey tek adreste.",
  },
  {
    icon: TruckIcon,
    title: "Hızlı Teslimat",
    description: "Siparişleriniz özenle hazırlanır ve en kısa sürede kapınıza gelir.",
  },
  {
    icon: ShieldCheckIcon,
    title: "Güvenli Ödeme",
    description: "Alışverişiniz baştan sona güvenli ve şeffaf bir şekilde tamamlanır.",
  },
]

export function LandingPage() {
  const { isAuthenticated } = useAuth()
  const { theme, toggleTheme } = useTheme()

  if (isAuthenticated) {
    return <Navigate to="/products" replace />
  }

  return (
    <div className="flex min-h-screen flex-col">
      <header className="flex items-center justify-between border-b border-border p-3">
        <span className="text-lg font-semibold">Shoppy</span>
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="icon"
            onClick={toggleTheme}
            aria-label="Temayı değiştir"
          >
            {theme === "dark" ? <SunIcon /> : <MoonIcon />}
          </Button>
          <Button asChild size="sm">
            <Link to="/login">Giriş Yap</Link>
          </Button>
        </div>
      </header>

      <main className="flex-1">
        <section className="mx-auto flex max-w-3xl flex-col items-center gap-4 px-4 py-20 text-center">
          <h1 className="text-4xl font-semibold tracking-tight sm:text-5xl">
            Shoppy&apos;de alışveriş çok daha kolay
          </h1>
          <p className="max-w-xl text-lg text-text-secondary">
            Geniş ürün yelpazesi, hızlı teslimat ve güvenli ödeme ile
            ihtiyacınız olan her şeye tek bir yerden ulaşın.
          </p>
          <Button asChild size="lg" className="mt-2">
            <Link to="/login">Giriş Yap</Link>
          </Button>
        </section>

        <section className="mx-auto grid max-w-5xl grid-cols-1 gap-4 px-4 pb-20 sm:grid-cols-3">
          {FEATURES.map((feature) => (
            <Card key={feature.title}>
              <CardHeader>
                <feature.icon className="size-6 text-accent" />
                <CardTitle>{feature.title}</CardTitle>
              </CardHeader>
              <CardContent className="text-text-secondary">
                {feature.description}
              </CardContent>
            </Card>
          ))}
        </section>
      </main>

      <footer className="border-t border-border p-4 text-center text-sm text-text-secondary">
        © {new Date().getFullYear()} Shoppy
      </footer>
    </div>
  )
}
