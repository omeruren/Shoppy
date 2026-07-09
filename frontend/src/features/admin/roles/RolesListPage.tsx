import type { ColumnDef } from "@tanstack/react-table"
import {
  KeyRoundIcon,
  MoreHorizontalIcon,
  PencilIcon,
  PlusIcon,
  TrashIcon,
} from "lucide-react"
import { useState } from "react"
import { DataTable } from "@/components/data-table/DataTable"
import { ConfirmDeleteDialog } from "@/components/guards/ConfirmDeleteDialog"
import { PermissionGuard } from "@/components/guards/PermissionGuard"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { RoleFormDialog } from "@/features/admin/roles/RoleFormDialog"
import { RolePermissionsDialog } from "@/features/admin/roles/RolePermissionsDialog"
import { useDeleteRole, useRolesQuery } from "@/hooks/useRoles"
import { useResourceListState } from "@/hooks/useResourceListState"
import type { RoleResultDto } from "@/types/role.types"

export function RolesListPage() {
  const { pagination, setPagination, searchTerm, setSearchTerm, queryParams } =
    useResourceListState({ initialPageSize: 10 })

  const { data, isLoading } = useRolesQuery(queryParams)
  const deleteRole = useDeleteRole()

  const [formState, setFormState] = useState<{
    open: boolean
    role?: RoleResultDto
  }>({ open: false })
  const [deleteTarget, setDeleteTarget] = useState<RoleResultDto | null>(null)
  const [permissionsTarget, setPermissionsTarget] =
    useState<RoleResultDto | null>(null)

  const columns: ColumnDef<RoleResultDto>[] = [
    { accessorKey: "name", header: "İsim" },
    {
      id: "actions",
      header: "",
      cell: ({ row }) => (
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon-sm">
              <MoreHorizontalIcon />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <PermissionGuard permission="Roles.Update">
              <DropdownMenuItem
                onClick={() => setFormState({ open: true, role: row.original })}
              >
                <PencilIcon /> Düzenle
              </DropdownMenuItem>
            </PermissionGuard>
            <PermissionGuard permission="Roles.Update">
              <DropdownMenuItem
                onClick={() => setPermissionsTarget(row.original)}
              >
                <KeyRoundIcon /> Yetkileri Düzenle
              </DropdownMenuItem>
            </PermissionGuard>
            <PermissionGuard permission="Roles.Delete">
              <DropdownMenuItem
                variant="destructive"
                onClick={() => setDeleteTarget(row.original)}
              >
                <TrashIcon /> Sil
              </DropdownMenuItem>
            </PermissionGuard>
          </DropdownMenuContent>
        </DropdownMenu>
      ),
    },
  ]

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

      <DataTable
        columns={columns}
        data={data?.data?.data ?? []}
        pageCount={data?.data?.totalPageCount ?? 0}
        pagination={pagination}
        onPaginationChange={setPagination}
        searchValue={searchTerm}
        onSearchChange={setSearchTerm}
        searchPlaceholder="Rol ara..."
        isLoading={isLoading}
        emptyMessage="Rol bulunamadı."
      />

      <RoleFormDialog
        open={formState.open}
        onOpenChange={(open) => setFormState((state) => ({ ...state, open }))}
        role={formState.role}
      />

      <RolePermissionsDialog
        open={!!permissionsTarget}
        onOpenChange={(open) => !open && setPermissionsTarget(null)}
        role={permissionsTarget}
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
