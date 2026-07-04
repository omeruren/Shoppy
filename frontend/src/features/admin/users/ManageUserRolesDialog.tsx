import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Checkbox } from "@/components/ui/checkbox"
import { Label } from "@/components/ui/label"
import { Skeleton } from "@/components/ui/skeleton"
import { useRolesQuery } from "@/hooks/useRoles"
import {
  useAssignUserRole,
  useRemoveUserRole,
  useUserRolesQuery,
} from "@/hooks/useUserRoles"
import type { UserProfileDto } from "@/types/user.types"

interface ManageUserRolesDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  user: UserProfileDto | null
}

export function ManageUserRolesDialog({
  open,
  onOpenChange,
  user,
}: ManageUserRolesDialogProps) {
  const { data: rolesResult, isLoading: rolesLoading } = useRolesQuery()
  const { data: userRolesResult, isLoading: userRolesLoading } =
    useUserRolesQuery()
  const assignRole = useAssignUserRole()
  const removeRole = useRemoveUserRole()

  if (!user) return null

  const roles = rolesResult?.data ?? []
  const assignedRoleIds = new Set(
    (userRolesResult?.data ?? [])
      .filter((userRole) => userRole.userId === user.id)
      .map((userRole) => userRole.roleId)
  )

  const isLoading = rolesLoading || userRolesLoading
  const isMutating = assignRole.isPending || removeRole.isPending

  const toggleRole = (roleId: string, checked: boolean) => {
    if (checked) {
      assignRole.mutate({ userId: user.id, roleId })
    } else {
      removeRole.mutate({ userId: user.id, roleId })
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{user.fullName} — Rolleri Yönet</DialogTitle>
        </DialogHeader>

        <div className="flex flex-col gap-3">
          {isLoading ? (
            Array.from({ length: 3 }).map((_, index) => (
              <Skeleton key={index} className="h-5 w-full" />
            ))
          ) : roles.length === 0 ? (
            <p className="text-sm text-text-secondary">
              Henüz rol tanımlanmamış.
            </p>
          ) : (
            roles.map((role) => (
              <div key={role.id} className="flex items-center gap-2">
                <Checkbox
                  id={`role-${role.id}`}
                  checked={assignedRoleIds.has(role.id)}
                  disabled={isMutating}
                  onCheckedChange={(checked) =>
                    toggleRole(role.id, checked === true)
                  }
                />
                <Label htmlFor={`role-${role.id}`}>{role.name}</Label>
              </div>
            ))
          )}
        </div>
      </DialogContent>
    </Dialog>
  )
}
