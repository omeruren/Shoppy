import { Link } from "react-router-dom"
import { Button } from "@/components/ui/button"

export function ForbiddenPage() {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-4 px-4 text-center">
      <p className="text-2xl font-semibold">403</p>
      <p className="text-text-secondary">Bu işlem için yetkiniz yok.</p>
      <Button asChild>
        <Link to="/products">Ana sayfaya dön</Link>
      </Button>
    </div>
  )
}
