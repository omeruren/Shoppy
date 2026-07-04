import { toast } from "sonner"
import { extractErrorMessages } from "@/lib/api-error"

export function handleApiError(error: unknown) {
  extractErrorMessages(error).forEach((message) => toast.error(message))
}
