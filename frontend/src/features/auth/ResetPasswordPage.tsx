import { zodResolver } from "@hookform/resolvers/zod"
import { useMutation } from "@tanstack/react-query"
import { useState } from "react"
import { useForm } from "react-hook-form"
import { Link, useNavigate, useSearchParams } from "react-router-dom"
import { z } from "zod"
import { resetPassword } from "@/api/auth.api"
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

const resetPasswordSchema = z
  .object({
    email: z.string().email("Geçerli bir e-posta adresi girin."),
    code: z.string().length(6, "Kod 6 haneli olmalıdır."),
    newPassword: z.string().min(6, "Şifre en az 6 karakter olmalıdır."),
    confirmNewPassword: z.string(),
  })
  .refine((values) => values.newPassword === values.confirmNewPassword, {
    message: "Şifreler eşleşmiyor.",
    path: ["confirmNewPassword"],
  })

type ResetPasswordFormValues = z.infer<typeof resetPasswordSchema>

export function ResetPasswordPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const [formErrors, setFormErrors] = useState<string[] | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ResetPasswordFormValues>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: { email: searchParams.get("email") ?? "" },
  })

  const mutation = useMutation({
    mutationFn: resetPassword,
    onSuccess: (result) => {
      if (!result.isSuccessful) {
        setFormErrors(
          result.errorMessages.length
            ? result.errorMessages
            : ["Şifre sıfırlama başarısız."]
        )
        return
      }
      navigate("/login", { replace: true })
    },
    onError: (error) => {
      setFormErrors(extractErrorMessages(error))
    },
  })

  const onSubmit = (values: ResetPasswordFormValues) => {
    setFormErrors(null)
    mutation.mutate({
      email: values.email,
      code: values.code,
      newPassword: values.newPassword,
    })
  }

  return (
    <div className="flex min-h-screen items-center justify-center px-4">
      <Card className="w-full max-w-sm">
        <CardHeader>
          <CardTitle>Şifre Sıfırlama</CardTitle>
          <CardDescription>
            E-postanıza gönderilen kodu ve yeni şifrenizi girin.
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

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="code">Doğrulama Kodu</Label>
              <Input
                id="code"
                inputMode="numeric"
                maxLength={6}
                aria-invalid={!!errors.code}
                {...register("code")}
              />
              {errors.code && (
                <p className="text-sm text-danger">{errors.code.message}</p>
              )}
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="newPassword">Yeni Şifre</Label>
              <Input
                id="newPassword"
                type="password"
                autoComplete="new-password"
                aria-invalid={!!errors.newPassword}
                {...register("newPassword")}
              />
              {errors.newPassword && (
                <p className="text-sm text-danger">
                  {errors.newPassword.message}
                </p>
              )}
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="confirmNewPassword">Yeni Şifre (Tekrar)</Label>
              <Input
                id="confirmNewPassword"
                type="password"
                autoComplete="new-password"
                aria-invalid={!!errors.confirmNewPassword}
                {...register("confirmNewPassword")}
              />
              {errors.confirmNewPassword && (
                <p className="text-sm text-danger">
                  {errors.confirmNewPassword.message}
                </p>
              )}
            </div>

            <Button type="submit" disabled={mutation.isPending}>
              {mutation.isPending ? "Kaydediliyor..." : "Şifreyi Sıfırla"}
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
