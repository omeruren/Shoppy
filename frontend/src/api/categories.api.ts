import { client } from "@/api/client"
import type { ResourceListQueryParams } from "@/hooks/useResourceListState"
import type { ApiResult, PaginationResultDto } from "@/types/api.types"
import type {
  CategoryCreateDto,
  CategoryResultDto,
  CategoryUpdateDto,
} from "@/types/category.types"

export async function getCategories(params: ResourceListQueryParams) {
  const { data } = await client.get<
    ApiResult<PaginationResultDto<CategoryResultDto>>
  >("/categories", { params })
  return data
}

export async function createCategory(request: CategoryCreateDto) {
  const { data } = await client.post<ApiResult<string>>(
    "/categories",
    request
  )
  return data
}

export async function updateCategory(request: CategoryUpdateDto) {
  const { data } = await client.put<ApiResult<string>>(
    `/categories/${request.id}`,
    request
  )
  return data
}

export async function deleteCategory(id: string) {
  const { data } = await client.delete<ApiResult<string>>(
    `/categories/${id}`
  )
  return data
}
