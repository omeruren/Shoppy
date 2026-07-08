import type { BaseEntity } from "@/types/api.types"

export interface RoleResultDto extends BaseEntity {
  name: string
  rowVersion: string | null
}

export interface RoleCreateDto {
  name: string
}

export interface RoleUpdateDto extends RoleCreateDto {
  id: string
  rowVersion?: string
}

export interface PermissionCatalogItem {
  name: string
  group: string
}
