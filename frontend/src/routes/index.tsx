import { createBrowserRouter } from "react-router-dom"
import { ProtectedRoute } from "@/components/guards/ProtectedRoute"
import { RequirePermission } from "@/components/guards/RequirePermission"
import { AdminDashboardPage } from "@/features/admin/AdminDashboardPage"
import { AdminLayout } from "@/features/admin/AdminLayout"
import { ForbiddenPage } from "@/features/admin/ForbiddenPage"
import { CategoriesListPage } from "@/features/admin/categories/CategoriesListPage"
import { OrdersListPage } from "@/features/admin/orders/OrdersListPage"
import { ProductsListPage } from "@/features/admin/products/ProductsListPage"
import { RolesListPage } from "@/features/admin/roles/RolesListPage"
import { UsersListPage } from "@/features/admin/users/UsersListPage"
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
      { path: "/403", element: <ForbiddenPage /> },
      {
        path: "/admin",
        element: <AdminLayout />,
        children: [
          { path: "dashboard", element: <AdminDashboardPage /> },
          {
            element: <RequirePermission permission="Products.Read" />,
            children: [{ path: "products", element: <ProductsListPage /> }],
          },
          {
            element: <RequirePermission permission="Categories.Read" />,
            children: [
              { path: "categories", element: <CategoriesListPage /> },
            ],
          },
          {
            element: <RequirePermission permission="Orders.Read" />,
            children: [{ path: "orders", element: <OrdersListPage /> }],
          },
          {
            element: <RequirePermission permission="Users.Read" />,
            children: [{ path: "users", element: <UsersListPage /> }],
          },
          {
            element: <RequirePermission permission="Roles.Read" />,
            children: [{ path: "roles", element: <RolesListPage /> }],
          },
        ],
      },
    ],
  },
])
