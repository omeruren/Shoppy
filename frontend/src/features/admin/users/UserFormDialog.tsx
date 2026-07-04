import { zodResolver } from "@hookform/resolvers/zod"
import { useForm } from "react-hook-form"
import { z } from "zod"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { useCreateUser, useUpdateUser } from "@/hooks/useUsers"
import type { UserProfileDto } from "@/types/user.types"

// The `password` field only applies to create — the backend silently ignores
// it on update — but it stays on one schema (via superRefine) so the inferred
// form type doesn't fork into a create/update union.
function buildUserSchema(isEditing: boolean) {
  return z
    .object({
      firstName: z.string().min(1, "Ad zorunludur."),
      lastName: z.string().min(1, "Soyad zorunludur."),
      userName: z.string().min(1, "Kullanıcı adı zorunludur."),
      email: z.string().email("Geçerli bir e-posta girin."),
      password: z.string().optional(),
    })
    .superRefine((values, ctx) => {
      if (!isEditing && (!values.password || values.password.length < 6)) {
        ctx.addIssue({
          code: "custom",
          path: ["password"],
          message: "Şifre en az 6 karakter olmalıdır.",
        })
      }
    })
}

type UserFormValues = z.infer<ReturnType<typeof buildUserSchema>>

interface UserFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  user?: UserProfileDto
}

export function UserFormDialog({
  open,
  onOpenChange,
  user,
}: UserFormDialogProps) {
  const isEditing = !!user
  const createUser = useCreateUser()
  const updateUser = useUpdateUser()

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<UserFormValues>({
    resolver: zodResolver(buildUserSchema(isEditing)),
    defaultValues: user
      ? {
          firstName: user.firstName,
          lastName: user.lastName,
          userName: user.userName,
          email: user.email,
        }
      : { firstName: "", lastName: "", userName: "", email: "", password: "" },
  })

  const isPending = createUser.isPending || updateUser.isPending

  const onSubmit = (values: UserFormValues) => {
    if (isEditing) {
      updateUser.mutate(
        { id: user.id, ...values },
        { onSuccess: () => onOpenChange(false) }
      )
    } else {
      createUser.mutate(
        { ...values, password: values.password! },
        { onSuccess: () => onOpenChange(false) }
      )
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>
            {isEditing ? "Kullanıcıyı Düzenle" : "Yeni Kullanıcı"}
          </DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4" noValidate>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="firstName">Ad</Label>
            <Input id="firstName" aria-invalid={!!errors.firstName} {...register("firstName")} />
            {errors.firstName && (
              <p className="text-sm text-danger">{errors.firstName.message}</p>
            )}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="lastName">Soyad</Label>
            <Input id="lastName" aria-invalid={!!errors.lastName} {...register("lastName")} />
            {errors.lastName && (
              <p className="text-sm text-danger">{errors.lastName.message}</p>
            )}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="userName">Kullanıcı Adı</Label>
            <Input id="userName" aria-invalid={!!errors.userName} {...register("userName")} />
            {errors.userName && (
              <p className="text-sm text-danger">{errors.userName.message}</p>
            )}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="email">E-posta</Label>
            <Input id="email" type="email" aria-invalid={!!errors.email} {...register("email")} />
            {errors.email && (
              <p className="text-sm text-danger">{errors.email.message}</p>
            )}
          </div>

          {!isEditing && (
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="password">Şifre</Label>
              <Input
                id="password"
                type="password"
                aria-invalid={!!errors.password}
                {...register("password")}
              />
              {errors.password && (
                <p className="text-sm text-danger">{errors.password.message}</p>
              )}
            </div>
          )}

          <DialogFooter>
            <Button type="submit" disabled={isPending}>
              {isPending ? "Kaydediliyor..." : "Kaydet"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
