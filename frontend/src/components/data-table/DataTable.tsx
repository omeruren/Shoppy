import {
  type ColumnDef,
  type OnChangeFn,
  type PaginationState,
  type SortingState,
  flexRender,
  getCoreRowModel,
  useReactTable,
} from "@tanstack/react-table"
import type { ReactNode } from "react"
import { ChevronLeftIcon, ChevronRightIcon, ChevronsUpDownIcon } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Skeleton } from "@/components/ui/skeleton"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { cn } from "@/lib/utils"

interface DataTableProps<TData> {
  columns: ColumnDef<TData, unknown>[]
  data: TData[]
  pageCount: number
  pagination: PaginationState
  onPaginationChange: OnChangeFn<PaginationState>
  sorting?: SortingState
  onSortingChange?: OnChangeFn<SortingState>
  searchValue?: string
  onSearchChange?: (value: string) => void
  searchPlaceholder?: string
  isLoading?: boolean
  emptyMessage?: string
  toolbarActions?: ReactNode
}

export function DataTable<TData>({
  columns,
  data,
  pageCount,
  pagination,
  onPaginationChange,
  sorting,
  onSortingChange,
  searchValue,
  onSearchChange,
  searchPlaceholder = "Ara...",
  isLoading = false,
  emptyMessage = "Kayıt bulunamadı.",
  toolbarActions,
}: DataTableProps<TData>) {
  const sortingEnabled = !!onSortingChange

  const table = useReactTable({
    data,
    columns,
    pageCount,
    state: {
      pagination,
      ...(sortingEnabled ? { sorting: sorting ?? [] } : {}),
    },
    onPaginationChange,
    onSortingChange,
    manualPagination: true,
    manualSorting: sortingEnabled,
    manualFiltering: true,
    enableSorting: sortingEnabled,
    getCoreRowModel: getCoreRowModel(),
  })

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap items-center justify-between gap-2">
        {onSearchChange ? (
          <Input
            value={searchValue ?? ""}
            onChange={(event) => onSearchChange(event.target.value)}
            placeholder={searchPlaceholder}
            className="max-w-xs"
          />
        ) : (
          <div />
        )}
        {toolbarActions}
      </div>

      <div className="rounded-xl border border-border">
        <Table>
          <TableHeader>
            {table.getHeaderGroups().map((headerGroup) => (
              <TableRow key={headerGroup.id}>
                {headerGroup.headers.map((header) => (
                  <TableHead key={header.id}>
                    {header.isPlaceholder ? null : header.column.getCanSort() ? (
                      <button
                        type="button"
                        className="flex items-center gap-1 select-none"
                        onClick={header.column.getToggleSortingHandler()}
                      >
                        {flexRender(
                          header.column.columnDef.header,
                          header.getContext()
                        )}
                        <ChevronsUpDownIcon
                          className={cn(
                            "size-3.5 text-muted-foreground",
                            header.column.getIsSorted() && "text-foreground"
                          )}
                        />
                      </button>
                    ) : (
                      flexRender(
                        header.column.columnDef.header,
                        header.getContext()
                      )
                    )}
                  </TableHead>
                ))}
              </TableRow>
            ))}
          </TableHeader>
          <TableBody>
            {isLoading ? (
              Array.from({ length: pagination.pageSize }).map((_, index) => (
                <TableRow key={index}>
                  {columns.map((_, columnIndex) => (
                    <TableCell key={columnIndex}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : table.getRowModel().rows.length ? (
              table.getRowModel().rows.map((row) => (
                <TableRow key={row.id}>
                  {row.getVisibleCells().map((cell) => (
                    <TableCell key={cell.id}>
                      {flexRender(cell.column.columnDef.cell, cell.getContext())}
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : (
              <TableRow>
                <TableCell
                  colSpan={columns.length}
                  className="h-24 text-center text-muted-foreground"
                >
                  {emptyMessage}
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>

      <div className="flex items-center justify-between text-sm text-text-secondary">
        <span>
          Sayfa {pagination.pageIndex + 1} / {Math.max(pageCount, 1)}
        </span>
        <div className="flex gap-1">
          <Button
            variant="outline"
            size="icon-sm"
            onClick={() => table.previousPage()}
            disabled={!table.getCanPreviousPage()}
          >
            <ChevronLeftIcon />
          </Button>
          <Button
            variant="outline"
            size="icon-sm"
            onClick={() => table.nextPage()}
            disabled={!table.getCanNextPage()}
          >
            <ChevronRightIcon />
          </Button>
        </div>
      </div>
    </div>
  )
}
