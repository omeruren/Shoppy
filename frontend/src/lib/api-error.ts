import { AxiosError } from "axios"
import type { ApiResult, ValidationProblemDetails } from "@/types/api.types"

type KnownErrorBody =
  | ApiResult<unknown>
  | ValidationProblemDetails
  | { title?: string }

/** Backend returns three different error shapes (see CLAUDE.md/spec BÖLÜM G) —
 * normalize all of them into a flat list of user-facing messages. */
export function extractErrorMessages(error: unknown): string[] {
  if (!(error instanceof AxiosError)) {
    return ["Beklenmeyen bir hata oluştu."]
  }

  const data = error.response?.data as KnownErrorBody | undefined

  if (!data) {
    return ["Sunucuya ulaşılamadı. Lütfen bağlantınızı kontrol edin."]
  }

  if (
    "errorMessages" in data &&
    Array.isArray(data.errorMessages) &&
    data.errorMessages.length > 0
  ) {
    return data.errorMessages
  }

  if ("errors" in data && data.errors) {
    return Object.values(data.errors).flat()
  }

  if ("title" in data && data.title) {
    return [data.title]
  }

  return ["Beklenmeyen bir hata oluştu."]
}
