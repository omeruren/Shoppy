import type { ColumnDef } from "@tanstack/react-table"
import {
  MoreHorizontalIcon,
  PencilIcon,
  PlusIcon,
  ShieldIcon,
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
import { ManageUserRolesDialog } from "@/features/admin/users/ManageUserRolesDialog"
import { UserFormDialog } from "@/features/admin/users/UserFormDialog"
import { useHasRole } from "@/hooks/useAuth"
import { useResourceListState } from "@/hooks/useResourceListState"
import { useDeleteUser, useUsersQuery } from "@/hooks/useUsers"
import type { UserProfileDto } from "@/types/user.types"

export function UsersListPage() {
  const { pagination, setPagination, searchTerm, setSearchTerm, queryParams } =
    useResourceListState({ initialPageSize: 10 })

  const { data, isLoading } = useUsersQuery(queryParams)
  const deleteUser = useDeleteUser()
  const canManageRoles = useHasRole("Admin")

  const [formState, setFormState] = useState<{
    open: boolean
    user?: UserProfileDto
  }>({ open: false })
  const [rolesTarget, setRolesTarget] = useState<UserProfileDto | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<UserProfileDto | null>(null)

  const columns: ColumnDef<UserProfileDto>[] = [
    { accessorKey: "fullName", header: "Ad Soyad" },
    { accessorKey: "userName", header: "Kullanıcı Adı" },
    { accessorKey: "email", header: "E-posta" },
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
            <PermissionGuard permission="Users.Update">
              <DropdownMenuItem
                onClick={() => setFormState({ open: true, user: row.original })}
              >
                <PencilIcon /> Düzenle
              </DropdownMenuItem>
            </PermissionGuard>
            {canManageRoles && (
              <DropdownMenuItem onClick={() => setRolesTarget(row.original)}>
                <ShieldIcon /> Rolleri Yönet
              </DropdownMenuItem>
            )}
            <PermissionGuard permission="Users.Delete">
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
        <h1 className="text-2xl font-semibold tracking-tight">Kullanıcılar</h1>
        <PermissionGuard permission="Users.Create">
          <Button onClick={() => setFormState({ open: true })}>
            <PlusIcon /> Yeni Kullanıcı
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
        searchPlaceholder="Kullanıcı ara..."
        isLoading={isLoading}
        emptyMessage="Kullanıcı bulunamadı."
      />

      <UserFormDialog
        key={formState.user?.id ?? "create"}
        open={formState.open}
        onOpenChange={(open) => setFormState((state) => ({ ...state, open }))}
        user={formState.user}
      />

      {canManageRoles && (
        <ManageUserRolesDialog
          open={!!rolesTarget}
          onOpenChange={(open) => !open && setRolesTarget(null)}
          user={rolesTarget}
        />
      )}

      <ConfirmDeleteDialog
        open={!!deleteTarget}
        onOpenChange={(open) => !open && setDeleteTarget(null)}
        title="Kullanıcıyı sil"
        description={`"${deleteTarget?.fullName}" kullanıcısını silmek istediğinize emin misiniz?`}
        isPending={deleteUser.isPending}
        onConfirm={() => {
          if (!deleteTarget) return
          deleteUser.mutate(deleteTarget.id, {
            onSuccess: () => setDeleteTarget(null),
          })
        }}
      />
    </div>
  )
}
