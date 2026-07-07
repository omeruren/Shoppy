import { ImageIcon } from "lucide-react"
import { useState } from "react"
import { cn } from "@/lib/utils"

interface ProductImageProps {
  src: string | null
  alt: string
  className?: string
  iconClassName?: string
}

export function ProductImage({
  src,
  alt,
  className,
  iconClassName,
}: ProductImageProps) {
  const [failed, setFailed] = useState(false)

  if (!src || failed) {
    return (
      <div
        className={cn(
          "flex items-center justify-center bg-muted text-muted-foreground",
          className
        )}
      >
        <ImageIcon className={cn("size-8", iconClassName)} />
      </div>
    )
  }

  return (
    <img
      src={src}
      alt={alt}
      className={cn("object-cover", className)}
      onError={() => setFailed(true)}
    />
  )
}
