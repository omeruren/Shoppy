import type { BaseEntity } from "@/types/api.types"

export interface OrderItemResultDto extends BaseEntity {
  productId: string
  productName: string
  quantity: number
  unitPrice: number
  rowVersion: string | null
}

export interface OrderResultDto extends BaseEntity {
  orderDate: string
  items: OrderItemResultDto[]
  rowVersion: string | null
}

export interface OrderItemCreateDto {
  productId: string
  quantity: number
}

export interface OrderCreateDto {
  items: OrderItemCreateDto[]
}

export interface OrderItemUpdateDto {
  id?: string
  productId: string
  quantity: number
  rowVersion?: string
}

export interface OrderUpdateDto {
  id: string
  orderDate: string
  items: OrderItemUpdateDto[]
  rowVersion?: string
}
