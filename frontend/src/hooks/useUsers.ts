import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import {
  createUser,
  deleteUser,
  getUsers,
  updateUser,
  type UsersListParams,
} from "@/api/users.api"
import { handleApiError } from "@/lib/handle-api-error"

const userKeys = {
  all: ["users"] as const,
  list: (params: UsersListParams) => [...userKeys.all, "list", params] as const,
}

export function useUsersQuery(params: UsersListParams) {
  return useQuery({
    queryKey: userKeys.list(params),
    queryFn: () => getUsers(params),
    placeholderData: (previousData) => previousData,
  })
}

export function useCreateUser() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: createUser,
    onSuccess: () => {
      toast.success("Kullanıcı oluşturuldu.")
      queryClient.invalidateQueries({ queryKey: userKeys.all })
    },
    onError: handleApiError,
  })
}

export function useUpdateUser() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: updateUser,
    onSuccess: () => {
      toast.success("Kullanıcı güncellendi.")
      queryClient.invalidateQueries({ queryKey: userKeys.all })
    },
    onError: handleApiError,
  })
}

export function useDeleteUser() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: deleteUser,
    onSuccess: () => {
      toast.success("Kullanıcı silindi.")
      queryClient.invalidateQueries({ queryKey: userKeys.all })
    },
    onError: handleApiError,
  })
}
