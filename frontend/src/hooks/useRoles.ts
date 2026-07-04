import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { createRole, deleteRole, getRoles, updateRole } from "@/api/roles.api"
import { handleApiError } from "@/lib/handle-api-error"

const roleKeys = {
  all: ["roles"] as const,
}

export function useRolesQuery() {
  return useQuery({
    queryKey: roleKeys.all,
    queryFn: getRoles,
  })
}

export function useCreateRole() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: createRole,
    onSuccess: () => {
      toast.success("Rol oluşturuldu.")
      queryClient.invalidateQueries({ queryKey: roleKeys.all })
    },
    onError: handleApiError,
  })
}

export function useUpdateRole() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: updateRole,
    onSuccess: () => {
      toast.success("Rol güncellendi.")
      queryClient.invalidateQueries({ queryKey: roleKeys.all })
    },
    onError: handleApiError,
  })
}

export function useDeleteRole() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: deleteRole,
    onSuccess: () => {
      toast.success("Rol silindi.")
      queryClient.invalidateQueries({ queryKey: roleKeys.all })
    },
    onError: handleApiError,
  })
}
