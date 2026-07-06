import { zodResolver } from "@hookform/resolvers/zod"
import { useEffect, useState } from "react"
import { useForm } from "react-hook-form"
import { z } from "zod"
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
import { Skeleton } from "@/components/ui/skeleton"
import { extractErrorMessages } from "@/lib/api-error"
import { useChangePassword, useMeQuery, useUpdateMe } from "@/hooks/useProfile"

const profileSchema = z.object({
  firstName: z.string().min(1, "Ad zorunludur."),
  lastName: z.string().min(1, "Soyad zorunludur."),
  userName: z.string().min(1, "Kullanıcı adı zorunludur."),
})

type ProfileFormValues = z.infer<typeof profileSchema>

const passwordSchema = z
  .object({
    currentPassword: z.string().min(1, "Mevcut şifre zorunludur."),
    newPassword: z.string().min(6, "Yeni şifre en az 6 karakter olmalıdır."),
    confirmNewPassword: z.string().min(1, "Şifre tekrarı zorunludur."),
  })
  .refine((values) => values.newPassword === values.confirmNewPassword, {
    message: "Yeni şifre ve tekrarı eşleşmiyor.",
    path: ["confirmNewPassword"],
  })

type PasswordFormValues = z.infer<typeof passwordSchema>

function ProfileForm() {
  const { data, isLoading } = useMeQuery()
  const updateMe = useUpdateMe()
  const [formErrors, setFormErrors] = useState<string[] | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<ProfileFormValues>({ resolver: zodResolver(profileSchema) })

  const profile = data?.data

  useEffect(() => {
    if (profile) {
      reset({
        firstName: profile.firstName,
        lastName: profile.lastName,
        userName: profile.userName,
      })
    }
  }, [profile, reset])

  if (isLoading) {
    return <Skeleton className="h-48 w-full" />
  }

  const onSubmit = (values: ProfileFormValues) => {
    setFormErrors(null)
    updateMe.mutate(values, { onError: (error) => setFormErrors(extractErrorMessages(error)) })
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4" noValidate>
      {formErrors && (
        <div role="alert" className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger">
          {formErrors.map((message) => (
            <p key={message}>{message}</p>
          ))}
        </div>
      )}

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="firstName">Ad</Label>
        <Input id="firstName" aria-invalid={!!errors.firstName} {...register("firstName")} />
        {errors.firstName && <p className="text-sm text-danger">{errors.firstName.message}</p>}
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="lastName">Soyad</Label>
        <Input id="lastName" aria-invalid={!!errors.lastName} {...register("lastName")} />
        {errors.lastName && <p className="text-sm text-danger">{errors.lastName.message}</p>}
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="userName">Kullanıcı Adı</Label>
        <Input id="userName" aria-invalid={!!errors.userName} {...register("userName")} />
        {errors.userName && <p className="text-sm text-danger">{errors.userName.message}</p>}
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="email">E-posta</Label>
        <Input id="email" value={profile?.email ?? ""} disabled />
      </div>

      <Button type="submit" disabled={updateMe.isPending} className="self-start">
        {updateMe.isPending ? "Kaydediliyor..." : "Kaydet"}
      </Button>
    </form>
  )
}

function ChangePasswordForm() {
  const changePassword = useChangePassword()
  const [formErrors, setFormErrors] = useState<string[] | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<PasswordFormValues>({ resolver: zodResolver(passwordSchema) })

  const onSubmit = (values: PasswordFormValues) => {
    setFormErrors(null)
    changePassword.mutate(values, {
      onSuccess: () => reset(),
      onError: (error) => setFormErrors(extractErrorMessages(error)),
    })
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4" noValidate>
      {formErrors && (
        <div role="alert" className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger">
          {formErrors.map((message) => (
            <p key={message}>{message}</p>
          ))}
        </div>
      )}

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="currentPassword">Mevcut Şifre</Label>
        <Input
          id="currentPassword"
          type="password"
          aria-invalid={!!errors.currentPassword}
          {...register("currentPassword")}
        />
        {errors.currentPassword && (
          <p className="text-sm text-danger">{errors.currentPassword.message}</p>
        )}
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="newPassword">Yeni Şifre</Label>
        <Input
          id="newPassword"
          type="password"
          aria-invalid={!!errors.newPassword}
          {...register("newPassword")}
        />
        {errors.newPassword && (
          <p className="text-sm text-danger">{errors.newPassword.message}</p>
        )}
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="confirmNewPassword">Yeni Şifre (Tekrar)</Label>
        <Input
          id="confirmNewPassword"
          type="password"
          aria-invalid={!!errors.confirmNewPassword}
          {...register("confirmNewPassword")}
        />
        {errors.confirmNewPassword && (
          <p className="text-sm text-danger">{errors.confirmNewPassword.message}</p>
        )}
      </div>

      <Button type="submit" disabled={changePassword.isPending} className="self-start">
        {changePassword.isPending ? "Değiştiriliyor..." : "Şifreyi Değiştir"}
      </Button>
    </form>
  )
}

export function ProfilePage() {
  return (
    <div className="flex max-w-xl flex-col gap-6">
      <h1 className="text-2xl font-semibold tracking-tight">Profilim</h1>

      <Card>
        <CardHeader>
          <CardTitle>Profil Bilgileri</CardTitle>
          <CardDescription>Ad, soyad ve kullanıcı adınızı güncelleyin.</CardDescription>
        </CardHeader>
        <CardContent>
          <ProfileForm />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Şifre Değiştir</CardTitle>
          <CardDescription>Hesap şifrenizi güncelleyin.</CardDescription>
        </CardHeader>
        <CardContent>
          <ChangePasswordForm />
        </CardContent>
      </Card>
    </div>
  )
}
