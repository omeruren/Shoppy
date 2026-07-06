import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import {
  createOrder,
  deleteOrder,
  getOrders,
  updateOrder,
} from "@/api/orders.api"
import type { ResourceListQueryParams } from "@/hooks/useResourceListState"
import { handleApiError } from "@/lib/handle-api-error"

const orderKeys = {
  all: ["orders"] as const,
  list: (params: ResourceListQueryParams) =>
    [...orderKeys.all, "list", params] as const,
}

export function useOrdersQuery(params: ResourceListQueryParams) {
  return useQuery({
    queryKey: orderKeys.list(params),
    queryFn: () => getOrders(params),
    placeholderData: (previousData) => previousData,
  })
}

export function useCreateOrder() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: createOrder,
    onSuccess: () => {
      toast.success("Siparişiniz alındı.")
      queryClient.invalidateQueries({ queryKey: orderKeys.all })
    },
    onError: handleApiError,
  })
}

export function useUpdateOrder() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: updateOrder,
    onSuccess: () => {
      toast.success("Sipariş güncellendi.")
      queryClient.invalidateQueries({ queryKey: orderKeys.all })
    },
    onError: handleApiError,
  })
}

export function useDeleteOrder() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: deleteOrder,
    onSuccess: () => {
      toast.success("Sipariş silindi.")
      queryClient.invalidateQueries({ queryKey: orderKeys.all })
    },
    onError: handleApiError,
  })
}
