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
import { CheckoutPage } from "@/features/customer/CheckoutPage"
import { CustomerLayout } from "@/features/customer/CustomerLayout"
import { MyOrdersPage } from "@/features/customer/MyOrdersPage"
import { ProductCatalogPage } from "@/features/customer/ProductCatalogPage"
import { ProductDetailPage } from "@/features/customer/ProductDetailPage"
import { ProfilePage } from "@/features/customer/ProfilePage"
import { LandingPage } from "@/features/marketing/LandingPage"

export const router = createBrowserRouter([
  { path: "/", element: <LandingPage /> },
  { path: "/login", element: <LoginPage /> },
  { path: "/forgot-password", element: <ForgotPasswordPage /> },
  { path: "/reset-password", element: <ResetPasswordPage /> },
  {
    element: <ProtectedRoute />,
    children: [
      { path: "/403", element: <ForbiddenPage /> },
      {
        element: <CustomerLayout />,
        children: [
          { path: "products", element: <ProductCatalogPage /> },
          { path: "products/:id", element: <ProductDetailPage /> },
          { path: "checkout", element: <CheckoutPage /> },
          { path: "orders", element: <MyOrdersPage /> },
          { path: "profile", element: <ProfilePage /> },
        ],
      },
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
