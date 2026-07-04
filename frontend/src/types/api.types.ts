export interface ApiResult<T> {
  data: T | null
  errorMessages: string[]
  isSuccessful: boolean
  statusCode: number
}

export interface PaginationResultDto<T> {
  data: T[]
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPageCount: number
}

export interface PaginationRequestDto {
  pageNumber?: number
  pageSize?: number
  searchTerm?: string
  sortBy?: string
  sortDirection?: "asc" | "desc"
}

export interface BaseEntity {
  id: string
  createdAt: string
  createdBy: string
  updatedAt: string | null
  updatedBy: string | null
  isDeleted: boolean
  deletedAt: string | null
  deletedBy: string | null
}

export interface ValidationProblemDetails {
  title: string
  status: number
  errors: Record<string, string[]>
}

export interface ProblemDetails {
  title: string
  status: number
  instance?: string
  traceId?: string
}
