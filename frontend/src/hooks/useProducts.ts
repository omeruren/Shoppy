import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import {
  createProduct,
  deleteProduct,
  getProducts,
  updateProduct,
} from "@/api/products.api"
import type { ResourceListQueryParams } from "@/hooks/useResourceListState"
import { handleApiError } from "@/lib/handle-api-error"

const productKeys = {
  all: ["products"] as const,
  list: (params: ResourceListQueryParams) =>
    [...productKeys.all, "list", params] as const,
}

export function useProductsQuery(params: ResourceListQueryParams) {
  return useQuery({
    queryKey: productKeys.list(params),
    queryFn: () => getProducts(params),
    placeholderData: (previousData) => previousData,
  })
}

/** Fetches a large, unfiltered page of products for use in <Select> dropdowns
 * (e.g. the Order item editor) rather than the paginated admin list. */
export function useProductOptions() {
  const { data, isLoading } = useProductsQuery({ pageNumber: 1, pageSize: 100 })

  return { products: data?.data?.data ?? [], isLoading }
}

export function useCreateProduct() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: createProduct,
    onSuccess: () => {
      toast.success("Ürün oluşturuldu.")
      queryClient.invalidateQueries({ queryKey: productKeys.all })
    },
    onError: handleApiError,
  })
}

export function useUpdateProduct() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: updateProduct,
    onSuccess: () => {
      toast.success("Ürün güncellendi.")
      queryClient.invalidateQueries({ queryKey: productKeys.all })
    },
    onError: handleApiError,
  })
}

export function useDeleteProduct() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: deleteProduct,
    onSuccess: () => {
      toast.success("Ürün silindi.")
      queryClient.invalidateQueries({ queryKey: productKeys.all })
    },
    onError: handleApiError,
  })
}
