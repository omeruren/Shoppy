import { useEffect, useState } from "react"
import { RouterProvider } from "react-router-dom"
import { Toaster } from "sonner"
import { refresh } from "@/api/auth.api"
import { Skeleton } from "@/components/ui/skeleton"
import { QueryProvider } from "@/providers/QueryProvider"
import { ThemeProvider } from "@/providers/ThemeProvider"
import { router } from "@/routes"
import { getStoredRefreshToken, useAuthStore } from "@/stores/auth.store"

function AppBootstrap({ children }: { children: React.ReactNode }) {
  const [isBootstrapping, setIsBootstrapping] = useState(true)

  useEffect(() => {
    const refreshToken = getStoredRefreshToken()
    if (!refreshToken) {
      setIsBootstrapping(false)
      return
    }

    refresh({ refreshToken })
      .then((result) => {
        if (result.isSuccessful && result.data) {
          useAuthStore
            .getState()
            .setSession(result.data.accessToken, result.data.refreshToken)
        } else {
          useAuthStore.getState().logout()
        }
      })
      .catch(() => {
        useAuthStore.getState().logout()
      })
      .finally(() => setIsBootstrapping(false))
  }, [])

  if (isBootstrapping) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Skeleton className="h-10 w-10 rounded-full" />
      </div>
    )
  }

  return children
}

function App() {
  return (
    <QueryProvider>
      <ThemeProvider>
        <AppBootstrap>
          <RouterProvider router={router} />
          <Toaster richColors position="top-right" />
        </AppBootstrap>
      </ThemeProvider>
    </QueryProvider>
  )
}

export default App
