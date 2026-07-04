import { createBrowserRouter } from "react-router-dom"
import { ProtectedRoute } from "@/components/guards/ProtectedRoute"
import { AdminDashboardPage } from "@/features/admin/AdminDashboardPage"
import { ForgotPasswordPage } from "@/features/auth/ForgotPasswordPage"
import { LoginPage } from "@/features/auth/LoginPage"
import { ResetPasswordPage } from "@/features/auth/ResetPasswordPage"
import { HomePage } from "@/features/home/HomePage"

export const router = createBrowserRouter([
  { path: "/login", element: <LoginPage /> },
  { path: "/forgot-password", element: <ForgotPasswordPage /> },
  { path: "/reset-password", element: <ResetPasswordPage /> },
  {
    element: <ProtectedRoute />,
    children: [
      { path: "/", element: <HomePage /> },
      { path: "/admin/dashboard", element: <AdminDashboardPage /> },
    ],
  },
])
