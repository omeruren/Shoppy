import { MoreHorizontalIcon, PencilIcon, PlusIcon, TrashIcon } from "lucide-react"
import { useState } from "react"
import { ConfirmDeleteDialog } from "@/components/guards/ConfirmDeleteDialog"
import { PermissionGuard } from "@/components/guards/PermissionGuard"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Input } from "@/components/ui/input"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { RoleFormDialog } from "@/features/admin/roles/RoleFormDialog"
import { useDeleteRole, useRolesQuery } from "@/hooks/useRoles"
import type { RoleResultDto } from "@/types/role.types"

export function RolesListPage() {
  const { data, isLoading } = useRolesQuery()
  const deleteRole = useDeleteRole()

  const [searchTerm, setSearchTerm] = useState("")
  const [formState, setFormState] = useState<{
    open: boolean
    role?: RoleResultDto
  }>({ open: false })
  const [deleteTarget, setDeleteTarget] = useState<RoleResultDto | null>(null)

  const roles = (data?.data ?? []).filter((role) =>
    role.name.toLowerCase().includes(searchTerm.toLowerCase())
  )

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold tracking-tight">Roller</h1>
        <PermissionGuard permission="Roles.Create">
          <Button onClick={() => setFormState({ open: true })}>
            <PlusIcon /> Yeni Rol
          </Button>
        </PermissionGuard>
      </div>

      <Input
        value={searchTerm}
        onChange={(event) => setSearchTerm(event.target.value)}
        placeholder="Rol ara..."
        className="max-w-xs"
      />

      <div className="rounded-xl border border-border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>İsim</TableHead>
              <TableHead />
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell colSpan={2} className="h-24 text-center text-muted-foreground">
                  Yükleniyor...
                </TableCell>
              </TableRow>
            ) : roles.length === 0 ? (
              <TableRow>
                <TableCell colSpan={2} className="h-24 text-center text-muted-foreground">
                  Rol bulunamadı.
                </TableCell>
              </TableRow>
            ) : (
              roles.map((role) => (
                <TableRow key={role.id}>
                  <TableCell>{role.name}</TableCell>
                  <TableCell className="w-10">
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <Button variant="ghost" size="icon-sm">
                          <MoreHorizontalIcon />
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end">
                        <PermissionGuard permission="Roles.Update">
                          <DropdownMenuItem
                            onClick={() => setFormState({ open: true, role })}
                          >
                            <PencilIcon /> Düzenle
                          </DropdownMenuItem>
                        </PermissionGuard>
                        <PermissionGuard permission="Roles.Delete">
                          <DropdownMenuItem
                            variant="destructive"
                            onClick={() => setDeleteTarget(role)}
                          >
                            <TrashIcon /> Sil
                          </DropdownMenuItem>
                        </PermissionGuard>
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      <RoleFormDialog
        open={formState.open}
        onOpenChange={(open) => setFormState((state) => ({ ...state, open }))}
        role={formState.role}
      />

      <ConfirmDeleteDialog
        open={!!deleteTarget}
        onOpenChange={(open) => !open && setDeleteTarget(null)}
        title="Rolü sil"
        description={`"${deleteTarget?.name}" rolünü silmek istediğinize emin misiniz?`}
        isPending={deleteRole.isPending}
        onConfirm={() => {
          if (!deleteTarget) return
          deleteRole.mutate(deleteTarget.id, {
            onSuccess: () => setDeleteTarget(null),
          })
        }}
      />
    </div>
  )
}
