import { zodResolver } from "@hookform/resolvers/zod"
import { useMutation } from "@tanstack/react-query"
import { useForm } from "react-hook-form"
import { Link, useNavigate } from "react-router-dom"
import { z } from "zod"
import { forgotPassword } from "@/api/auth.api"
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

const forgotPasswordSchema = z.object({
  email: z.string().email("Geçerli bir e-posta adresi girin."),
})

type ForgotPasswordFormValues = z.infer<typeof forgotPasswordSchema>

export function ForgotPasswordPage() {
  const navigate = useNavigate()

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ForgotPasswordFormValues>({
    resolver: zodResolver(forgotPasswordSchema),
  })

  const mutation = useMutation({
    mutationFn: forgotPassword,
    // Backend always returns success here regardless of whether the email
    // exists, to prevent user enumeration — the frontend must not distinguish either.
    onSettled: (_data, _error, variables) => {
      navigate(
        `/reset-password?email=${encodeURIComponent(variables.email)}`
      )
    },
  })

  const onSubmit = (values: ForgotPasswordFormValues) => {
    mutation.mutate(values)
  }

  return (
    <div className="flex min-h-screen items-center justify-center px-4">
      <Card className="w-full max-w-sm">
        <CardHeader>
          <CardTitle>Şifremi Unuttum</CardTitle>
          <CardDescription>
            E-posta adresinizi girin, size bir doğrulama kodu gönderelim.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form
            onSubmit={handleSubmit(onSubmit)}
            className="flex flex-col gap-4"
            noValidate
          >
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="email">E-posta</Label>
              <Input
                id="email"
                type="email"
                autoComplete="email"
                aria-invalid={!!errors.email}
                {...register("email")}
              />
              {errors.email && (
                <p className="text-sm text-danger">{errors.email.message}</p>
              )}
            </div>

            <Button type="submit" disabled={mutation.isPending}>
              {mutation.isPending ? "Gönderiliyor..." : "Kod Gönder"}
            </Button>

            <Link
              to="/login"
              className="text-center text-sm text-text-secondary hover:text-accent"
            >
              Girişe dön
            </Link>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
