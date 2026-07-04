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
import { ProductFormDialog } from "@/features/admin/products/ProductFormDialog"
import { useResourceListState } from "@/hooks/useResourceListState"
import { useDeleteProduct, useProductsQuery } from "@/hooks/useProducts"
import type { ProductResultDto } from "@/types/product.types"

const priceFormatter = new Intl.NumberFormat("tr-TR", {
  style: "currency",
  currency: "TRY",
})

export function ProductsListPage() {
  const {
    pagination,
    setPagination,
    sorting,
    setSorting,
    searchTerm,
    setSearchTerm,
    queryParams,
  } = useResourceListState({ initialPageSize: 10, enableSorting: true })

  const { data, isLoading } = useProductsQuery(queryParams)
  const deleteProduct = useDeleteProduct()

  const [formState, setFormState] = useState<{
    open: boolean
    product?: ProductResultDto
  }>({ open: false })
  const [deleteTarget, setDeleteTarget] = useState<ProductResultDto | null>(
    null
  )

  const columns: ColumnDef<ProductResultDto>[] = [
    { accessorKey: "name", header: "İsim" },
    { accessorKey: "categoryName", header: "Kategori" },
    {
      accessorKey: "price",
      header: "Fiyat",
      cell: ({ row }) => priceFormatter.format(row.original.price),
    },
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
            <PermissionGuard permission="Products.Update">
              <DropdownMenuItem
                onClick={() =>
                  setFormState({ open: true, product: row.original })
                }
              >
                <PencilIcon /> Düzenle
              </DropdownMenuItem>
            </PermissionGuard>
            <PermissionGuard permission="Products.Delete">
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
        <h1 className="text-2xl font-semibold tracking-tight">Ürünler</h1>
        <PermissionGuard permission="Products.Create">
          <Button onClick={() => setFormState({ open: true })}>
            <PlusIcon /> Yeni Ürün
          </Button>
        </PermissionGuard>
      </div>

      <DataTable
        columns={columns}
        data={data?.data?.data ?? []}
        pageCount={data?.data?.totalPageCount ?? 0}
        pagination={pagination}
        onPaginationChange={setPagination}
        sorting={sorting}
        onSortingChange={setSorting}
        searchValue={searchTerm}
        onSearchChange={setSearchTerm}
        searchPlaceholder="Ürün ara..."
        isLoading={isLoading}
        emptyMessage="Ürün bulunamadı."
      />

      <ProductFormDialog
        open={formState.open}
        onOpenChange={(open) => setFormState((state) => ({ ...state, open }))}
        product={formState.product}
      />

      <ConfirmDeleteDialog
        open={!!deleteTarget}
        onOpenChange={(open) => !open && setDeleteTarget(null)}
        title="Ürünü sil"
        description={`"${deleteTarget?.name}" ürününü silmek istediğinize emin misiniz?`}
        isPending={deleteProduct.isPending}
        onConfirm={() => {
          if (!deleteTarget) return
          deleteProduct.mutate(deleteTarget.id, {
            onSuccess: () => setDeleteTarget(null),
          })
        }}
      />
    </div>
  )
}
