import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import {
  createCategory,
  deleteCategory,
  getCategories,
  updateCategory,
} from "@/api/categories.api"
import type { ResourceListQueryParams } from "@/hooks/useResourceListState"
import { handleApiError } from "@/lib/handle-api-error"

const categoryKeys = {
  all: ["categories"] as const,
  list: (params: ResourceListQueryParams) =>
    [...categoryKeys.all, "list", params] as const,
}

export function useCategoriesQuery(params: ResourceListQueryParams) {
  return useQuery({
    queryKey: categoryKeys.list(params),
    queryFn: () => getCategories(params),
    placeholderData: (previousData) => previousData,
  })
}

/** Fetches a large, unfiltered page of categories for use in <Select> dropdowns
 * elsewhere (e.g. the Product form) rather than the paginated admin list. */
export function useCategoryOptions() {
  const { data, isLoading } = useCategoriesQuery({
    pageNumber: 1,
    pageSize: 100,
  })

  return { categories: data?.data?.data ?? [], isLoading }
}

export function useCreateCategory() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: createCategory,
    onSuccess: () => {
      toast.success("Kategori oluşturuldu.")
      queryClient.invalidateQueries({ queryKey: categoryKeys.all })
    },
    onError: handleApiError,
  })
}

export function useUpdateCategory() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: updateCategory,
    onSuccess: () => {
      toast.success("Kategori güncellendi.")
      queryClient.invalidateQueries({ queryKey: categoryKeys.all })
    },
    onError: handleApiError,
  })
}

export function useDeleteCategory() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: deleteCategory,
    onSuccess: () => {
      toast.success("Kategori silindi.")
      queryClient.invalidateQueries({ queryKey: categoryKeys.all })
    },
    onError: handleApiError,
  })
}
