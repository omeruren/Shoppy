import type { ColumnDef } from "@tanstack/react-table"
import { MoreHorizontalIcon, PencilIcon, PlusIcon, TrashIcon } from "lucide-react"
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
import { CategoryFormDialog } from "@/features/admin/categories/CategoryFormDialog"
import { useCategoriesQuery, useDeleteCategory } from "@/hooks/useCategories"
import { useResourceListState } from "@/hooks/useResourceListState"
import type { CategoryResultDto } from "@/types/category.types"

export function CategoriesListPage() {
  const { pagination, setPagination, searchTerm, setSearchTerm, queryParams } =
    useResourceListState({ initialPageSize: 10 })

  const { data, isLoading } = useCategoriesQuery(queryParams)
  const deleteCategory = useDeleteCategory()

  const [formState, setFormState] = useState<{
    open: boolean
    category?: CategoryResultDto
  }>({ open: false })
  const [deleteTarget, setDeleteTarget] = useState<CategoryResultDto | null>(
    null
  )

  const columns: ColumnDef<CategoryResultDto>[] = [
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
            <PermissionGuard permission="Categories.Update">
              <DropdownMenuItem
                onClick={() =>
                  setFormState({ open: true, category: row.original })
                }
              >
                <PencilIcon /> Düzenle
              </DropdownMenuItem>
            </PermissionGuard>
            <PermissionGuard permission="Categories.Delete">
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
        <h1 className="text-2xl font-semibold tracking-tight">Kategoriler</h1>
        <PermissionGuard permission="Categories.Create">
          <Button onClick={() => setFormState({ open: true })}>
            <PlusIcon /> Yeni Kategori
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
        searchPlaceholder="Kategori ara..."
        isLoading={isLoading}
        emptyMessage="Kategori bulunamadı."
      />

      <CategoryFormDialog
        open={formState.open}
        onOpenChange={(open) => setFormState((state) => ({ ...state, open }))}
        category={formState.category}
      />

      <ConfirmDeleteDialog
        open={!!deleteTarget}
        onOpenChange={(open) => !open && setDeleteTarget(null)}
        title="Kategoriyi sil"
        description={`"${deleteTarget?.name}" kategorisini silmek istediğinize emin misiniz?`}
        isPending={deleteCategory.isPending}
        onConfirm={() => {
          if (!deleteTarget) return
          deleteCategory.mutate(deleteTarget.id, {
            onSuccess: () => setDeleteTarget(null),
          })
        }}
      />
    </div>
  )
}
