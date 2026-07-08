import { useEffect, useState } from "react"
import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Label } from "@/components/ui/label"
import { Skeleton } from "@/components/ui/skeleton"
import {
  usePermissionCatalogQuery,
  useRolePermissionsQuery,
  useUpdateRolePermissions,
} from "@/hooks/useRoles"
import type { RoleResultDto } from "@/types/role.types"

interface RolePermissionsDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  role: RoleResultDto | null
}

export function RolePermissionsDialog({
  open,
  onOpenChange,
  role,
}: RolePermissionsDialogProps) {
  const { data: catalogResult, isLoading: catalogLoading } =
    usePermissionCatalogQuery()
  const { data: rolePermissionsResult, isLoading: rolePermissionsLoading } =
    useRolePermissionsQuery(open ? role?.id : undefined)
  const updatePermissions = useUpdateRolePermissions()

  const [selected, setSelected] = useState<Set<string>>(new Set())

  useEffect(() => {
    if (!open) return
    setSelected(new Set(rolePermissionsResult?.data ?? []))
  }, [open, rolePermissionsResult])

  if (!role) return null

  const catalog = catalogResult?.data ?? []
  const groups = catalog.reduce<Map<string, string[]>>((acc, item) => {
    const names = acc.get(item.group) ?? []
    names.push(item.name)
    acc.set(item.group, names)
    return acc
  }, new Map())

  const isLoading = catalogLoading || rolePermissionsLoading

  const togglePermission = (name: string, checked: boolean) => {
    setSelected((current) => {
      const next = new Set(current)
      if (checked) next.add(name)
      else next.delete(name)
      return next
    })
  }

  const handleSave = () => {
    updatePermissions.mutate(
      { roleId: role.id, permissions: Array.from(selected) },
      { onSuccess: () => onOpenChange(false) }
    )
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[80vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>{role.name} — Yetkileri Düzenle</DialogTitle>
        </DialogHeader>

        <div className="flex flex-col gap-4">
          {isLoading ? (
            Array.from({ length: 4 }).map((_, index) => (
              <Skeleton key={index} className="h-5 w-full" />
            ))
          ) : groups.size === 0 ? (
            <p className="text-sm text-text-secondary">
              Tanımlı yetki bulunamadı.
            </p>
          ) : (
            Array.from(groups.entries()).map(([group, names]) => (
              <div key={group} className="flex flex-col gap-2">
                <h3 className="text-sm font-semibold">{group}</h3>
                <div className="flex flex-col gap-1.5 pl-1">
                  {names.map((name) => (
                    <div key={name} className="flex items-center gap-2">
                      <Checkbox
                        id={`permission-${name}`}
                        checked={selected.has(name)}
                        onCheckedChange={(checked) =>
                          togglePermission(name, checked === true)
                        }
                      />
                      <Label htmlFor={`permission-${name}`}>{name}</Label>
                    </div>
                  ))}
                </div>
              </div>
            ))
          )}
        </div>

        <DialogFooter>
          <Button
            onClick={handleSave}
            disabled={isLoading || updatePermissions.isPending}
          >
            {updatePermissions.isPending ? "Kaydediliyor..." : "Kaydet"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
