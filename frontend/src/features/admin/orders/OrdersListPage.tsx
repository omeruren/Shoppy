import type { ColumnDef } from "@tanstack/react-table"
import { EyeIcon, MoreHorizontalIcon, TrashIcon } from "lucide-react"
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
import { OrderDetailDialog } from "@/features/admin/orders/OrderDetailDialog"
import { useDeleteOrder, useOrdersQuery } from "@/hooks/useOrders"
import { useResourceListState } from "@/hooks/useResourceListState"
import type { OrderResultDto } from "@/types/order.types"

export function OrdersListPage() {
  const {
    pagination,
    setPagination,
    sorting,
    setSorting,
    searchTerm,
    setSearchTerm,
    queryParams,
  } = useResourceListState({ initialPageSize: 10, enableSorting: true })

  const { data, isLoading } = useOrdersQuery(queryParams)
  const deleteOrder = useDeleteOrder()

  const [detailOrder, setDetailOrder] = useState<OrderResultDto | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<OrderResultDto | null>(null)

  const columns: ColumnDef<OrderResultDto>[] = [
    {
      accessorKey: "orderDate",
      header: "Tarih",
      cell: ({ row }) =>
        new Date(row.original.orderDate).toLocaleString("tr-TR"),
    },
    {
      id: "itemCount",
      header: "Ürün Sayısı",
      cell: ({ row }) => row.original.items.length,
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
            <DropdownMenuItem onClick={() => setDetailOrder(row.original)}>
              <EyeIcon /> Görüntüle / Düzenle
            </DropdownMenuItem>
            <PermissionGuard permission="Orders.Delete">
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
      <h1 className="text-2xl font-semibold tracking-tight">Siparişler</h1>

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
        searchPlaceholder="Ürün adına göre ara..."
        isLoading={isLoading}
        emptyMessage="Sipariş bulunamadı."
      />

      <OrderDetailDialog
        open={!!detailOrder}
        onOpenChange={(open) => !open && setDetailOrder(null)}
        order={detailOrder}
      />

      <ConfirmDeleteDialog
        open={!!deleteTarget}
        onOpenChange={(open) => !open && setDeleteTarget(null)}
        title="Siparişi sil"
        description="Bu siparişi silmek istediğinize emin misiniz?"
        isPending={deleteOrder.isPending}
        onConfirm={() => {
          if (!deleteTarget) return
          deleteOrder.mutate(deleteTarget.id, {
            onSuccess: () => setDeleteTarget(null),
          })
        }}
      />
    </div>
  )
}
