import type { BaseEntity } from "@/types/api.types"

export interface CategoryResultDto extends BaseEntity {
  name: string
  rowVersion: string | null
}

export interface CategoryCreateDto {
  name: string
}

export interface CategoryUpdateDto extends CategoryCreateDto {
  id: string
  rowVersion?: string
}
