import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { changePassword, getMe, updateMe } from "@/api/me.api"
import { handleApiError } from "@/lib/handle-api-error"

const meKeys = {
  all: ["me"] as const,
}

export function useMeQuery() {
  return useQuery({
    queryKey: meKeys.all,
    queryFn: getMe,
  })
}

export function useUpdateMe() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: updateMe,
    onSuccess: () => {
      toast.success("Profil güncellendi.")
      queryClient.invalidateQueries({ queryKey: meKeys.all })
    },
    onError: handleApiError,
  })
}

export function useChangePassword() {
  return useMutation({
    mutationFn: changePassword,
    onSuccess: () => {
      toast.success("Şifre değiştirildi.")
    },
    onError: handleApiError,
  })
}
