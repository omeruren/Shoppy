import { zodResolver } from "@hookform/resolvers/zod"
import { useMutation } from "@tanstack/react-query"
import { useState } from "react"
import { useForm } from "react-hook-form"
import { Link, useLocation, useNavigate } from "react-router-dom"
import { z } from "zod"
import { login } from "@/api/auth.api"
import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { extractErrorMessages } from "@/lib/api-error"
import { useAuthStore } from "@/stores/auth.store"

const loginSchema = z.object({
  userName: z.string().min(1, "Kullanıcı adı zorunludur."),
  password: z.string().min(1, "Şifre zorunludur."),
})

type LoginFormValues = z.infer<typeof loginSchema>

export function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const [formErrors, setFormErrors] = useState<string[] | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormValues>({ resolver: zodResolver(loginSchema) })

  const mutation = useMutation({
    mutationFn: login,
    onSuccess: (result) => {
      if (!result.isSuccessful || !result.data) {
        setFormErrors(
          result.errorMessages.length
            ? result.errorMessages
            : ["Giriş başarısız."]
        )
        return
      }

      setFormErrors(null)
      useAuthStore
        .getState()
        .setSession(result.data.accessToken, result.data.refreshToken)

      const user = useAuthStore.getState().user
      const from = (location.state as { from?: Location })?.from?.pathname
      const redirectTo = user?.roles.includes("Admin")
        ? "/admin/dashboard"
        : (from ?? "/")
      navigate(redirectTo, { replace: true })
    },
    onError: (error) => {
      setFormErrors(extractErrorMessages(error))
    },
  })

  const onSubmit = (values: LoginFormValues) => {
    setFormErrors(null)
    mutation.mutate(values)
  }

  return (
    <div className="flex min-h-screen items-center justify-center px-4">
      <Card className="w-full max-w-sm">
        <CardHeader>
          <CardTitle>Giriş Yap</CardTitle>
          <CardDescription>
            Devam etmek için hesabınıza giriş yapın.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form
            onSubmit={handleSubmit(onSubmit)}
            className="flex flex-col gap-4"
            noValidate
          >
            {formErrors && (
              <div
                role="alert"
                className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger"
              >
                {formErrors.map((message) => (
                  <p key={message}>{message}</p>
                ))}
              </div>
            )}

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="userName">Kullanıcı Adı</Label>
              <Input
                id="userName"
                autoComplete="username"
                aria-invalid={!!errors.userName}
                {...register("userName")}
              />
              {errors.userName && (
                <p className="text-sm text-danger">
                  {errors.userName.message}
                </p>
              )}
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="password">Şifre</Label>
              <Input
                id="password"
                type="password"
                autoComplete="current-password"
                aria-invalid={!!errors.password}
                {...register("password")}
              />
              {errors.password && (
                <p className="text-sm text-danger">
                  {errors.password.message}
                </p>
              )}
            </div>

            <Button type="submit" disabled={mutation.isPending}>
              {mutation.isPending ? "Giriş yapılıyor..." : "Giriş Yap"}
            </Button>

            <Link
              to="/forgot-password"
              className="text-center text-sm text-text-secondary hover:text-accent"
            >
              Şifremi Unuttum
            </Link>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
