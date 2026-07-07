import type { BaseEntity } from "@/types/api.types"

export interface ProductResultDto extends BaseEntity {
  name: string
  description: string | null
  imageUrl: string | null
  price: number
  categoryId: string
  categoryName: string
  rowVersion: string | null
}

export interface ProductCreateDto {
  name: string
  description?: string
  imageUrl?: string
  price: number
  categoryId: string
}

export interface ProductUpdateDto extends ProductCreateDto {
  id: string
  rowVersion?: string
}
