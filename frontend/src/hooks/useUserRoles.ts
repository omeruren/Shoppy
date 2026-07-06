import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import {
  assignUserRole,
  getUserRoles,
  removeUserRole,
} from "@/api/userRoles.api"
import { handleApiError } from "@/lib/handle-api-error"

const userRoleKeys = {
  all: ["userRoles"] as const,
}

export function useUserRolesQuery() {
  return useQuery({
    queryKey: userRoleKeys.all,
    queryFn: getUserRoles,
  })
}

export function useAssignUserRole() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: assignUserRole,
    onSuccess: () => {
      toast.success("Rol atandı.")
      queryClient.invalidateQueries({ queryKey: userRoleKeys.all })
    },
    onError: handleApiError,
  })
}

export function useRemoveUserRole() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ userId, roleId }: { userId: string; roleId: string }) =>
      removeUserRole(userId, roleId),
    onSuccess: () => {
      toast.success("Rol kaldırıldı.")
      queryClient.invalidateQueries({ queryKey: userRoleKeys.all })
    },
    onError: handleApiError,
  })
}
