import { AxiosError } from "axios"
import { toast } from "sonner"
import { extractErrorMessages } from "@/lib/api-error"

export function handleApiError(error: unknown) {
  extractErrorMessages(error).forEach((message) => toast.error(message))
  if (error instanceof AxiosError && error.response?.status === 409) {
    toast.error(
      "Bu kayıt başka bir işlem tarafından güncellenmiş olabilir. Sayfayı yenileyip tekrar deneyin."
    )
  }
}
