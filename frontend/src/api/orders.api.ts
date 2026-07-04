import { client } from "@/api/client"
import type { ResourceListQueryParams } from "@/hooks/useResourceListState"
import type { ApiResult, PaginationResultDto } from "@/types/api.types"
import type { OrderResultDto, OrderUpdateDto } from "@/types/order.types"

export async function getOrders(params: ResourceListQueryParams) {
  const { data } = await client.get<
    ApiResult<PaginationResultDto<OrderResultDto>>
  >("/orders", { params })
  return data
}

export async function updateOrder(request: OrderUpdateDto) {
  const { data } = await client.put<ApiResult<string>>(
    `/orders/${request.id}`,
    request
  )
  return data
}

export async function deleteOrder(id: string) {
  const { data } = await client.delete<ApiResult<string>>(`/orders/${id}`)
  return data
}
