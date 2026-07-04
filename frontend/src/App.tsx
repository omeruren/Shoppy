import { RouterProvider } from "react-router-dom"
import { Toaster } from "sonner"
import { QueryProvider } from "@/providers/QueryProvider"
import { ThemeProvider } from "@/providers/ThemeProvider"
import { router } from "@/routes"

function App() {
  return (
    <QueryProvider>
      <ThemeProvider>
        <RouterProvider router={router} />
        <Toaster richColors position="top-right" />
      </ThemeProvider>
    </QueryProvider>
  )
}

export default App
