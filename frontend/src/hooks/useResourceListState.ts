import { useEffect, useState } from "react"
import type { PaginationState, SortingState } from "@tanstack/react-table"

interface UseResourceListStateOptions {
  initialPageSize?: number
  enableSorting?: boolean
  searchDelayMs?: number
}

export interface ResourceListQueryParams {
  pageNumber: number
  pageSize: number
  searchTerm?: string
  sortBy?: string
  sortDirection?: "asc" | "desc"
}

/** Combines pagination/search/sort UI state into the query params the backend
 * list endpoints accept, and resets to page 1 whenever the search term changes. */
export function useResourceListState(options: UseResourceListStateOptions = {}) {
  const { initialPageSize = 10, enableSorting = false, searchDelayMs = 400 } =
    options

  const [pagination, setPagination] = useState<PaginationState>({
    pageIndex: 0,
    pageSize: initialPageSize,
  })
  const [sorting, setSorting] = useState<SortingState>([])
  const [searchTerm, setSearchTerm] = useState("")
  const [debouncedSearchTerm, setDebouncedSearchTerm] = useState("")

  useEffect(() => {
    const handle = setTimeout(
      () => setDebouncedSearchTerm(searchTerm),
      searchDelayMs
    )
    return () => clearTimeout(handle)
  }, [searchTerm, searchDelayMs])

  useEffect(() => {
    setPagination((current) =>
      current.pageIndex === 0 ? current : { ...current, pageIndex: 0 }
    )
  }, [debouncedSearchTerm])

  const activeSort = sorting[0]

  const queryParams: ResourceListQueryParams = {
    pageNumber: pagination.pageIndex + 1,
    pageSize: pagination.pageSize,
    searchTerm: debouncedSearchTerm || undefined,
    sortBy: activeSort?.id,
    sortDirection: activeSort ? (activeSort.desc ? "desc" : "asc") : undefined,
  }

  return {
    pagination,
    setPagination,
    sorting: enableSorting ? sorting : undefined,
    setSorting: enableSorting ? setSorting : undefined,
    searchTerm,
    setSearchTerm,
    queryParams,
  }
}
