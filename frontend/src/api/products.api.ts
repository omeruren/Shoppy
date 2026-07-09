import { client } from "@/api/client"
import type { ResourceListQueryParams } from "@/hooks/useResourceListState"
import type { ApiResult, PaginationResultDto } from "@/types/api.types"
import type {
  ProductCreateDto,
  ProductResultDto,
  ProductUpdateDto,
} from "@/types/product.types"

export type ProductListQueryParams = ResourceListQueryParams & {
  categoryId?: string
}

export async function getProducts(params: ProductListQueryParams) {
  const { data } = await client.get<
    ApiResult<PaginationResultDto<ProductResultDto>>
  >("/products", { params })
  return data
}

export async function getProductById(id: string) {
  const { data } = await client.get<ApiResult<ProductResultDto>>(
    `/products/${id}`
  )
  return data
}

export async function createProduct(request: ProductCreateDto) {
  const { data } = await client.post<ApiResult<string>>("/products", request)
  return data
}

export async function updateProduct(request: ProductUpdateDto) {
  const { data } = await client.put<ApiResult<string>>(
    `/products/${request.id}`,
    request
  )
  return data
}

export async function deleteProduct(id: string) {
  const { data } = await client.delete<ApiResult<string>>(`/products/${id}`)
  return data
}
