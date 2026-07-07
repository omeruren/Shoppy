import { MinusIcon, PlusIcon, ShoppingCartIcon } from "lucide-react"
import { useState } from "react"
import { Link, useParams } from "react-router-dom"
import { toast } from "sonner"
import { ProductImage } from "@/components/ProductImage"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import { priceFormatter } from "@/features/customer/ProductCard"
import { useProductQuery } from "@/hooks/useProducts"
import { MAX_ITEM_QUANTITY, useCartStore } from "@/stores/cart.store"

export function ProductDetailPage() {
  const { id } = useParams<{ id: string }>()
  const [quantity, setQuantity] = useState(1)
  const addItem = useCartStore((state) => state.addItem)

  const { data, isLoading } = useProductQuery(id ?? "")
  const product = data?.data

  if (isLoading) {
    return (
      <div className="flex flex-col gap-4">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-40 w-full max-w-xl" />
      </div>
    )
  }

  if (!product) {
    return (
      <div className="flex flex-col items-center gap-3 py-12 text-center">
        <p className="text-muted-foreground">Ürün bulunamadı.</p>
        <Link to="/products" className="text-accent hover:underline">
          Ürünlere dön
        </Link>
      </div>
    )
  }

  return (
    <div className="flex max-w-xl flex-col gap-4">
      <Link to="/products" className="text-sm text-text-secondary hover:text-accent">
        ← Ürünlere dön
      </Link>

      <ProductImage
        src={product.imageUrl}
        alt={product.name}
        className="h-64 w-full rounded-xl"
        iconClassName="size-10"
      />

      <div className="flex flex-col gap-2">
        <Badge variant="outline" className="w-fit">
          {product.categoryName}
        </Badge>
        <h1 className="text-2xl font-semibold tracking-tight">
          {product.name}
        </h1>
        {product.description && (
          <p className="text-text-secondary">{product.description}</p>
        )}
        <span className="text-xl font-semibold">
          {priceFormatter.format(product.price)}
        </span>
      </div>

      <div className="flex items-center gap-3">
        <div className="flex items-center gap-1">
          <Button
            type="button"
            variant="outline"
            size="icon-sm"
            onClick={() => setQuantity((q) => Math.max(1, q - 1))}
          >
            <MinusIcon />
          </Button>
          <span className="w-8 text-center text-sm">{quantity}</span>
          <Button
            type="button"
            variant="outline"
            size="icon-sm"
            onClick={() =>
              setQuantity((q) => {
                if (q >= MAX_ITEM_QUANTITY) {
                  toast.warning(
                    `Bir üründen en fazla ${MAX_ITEM_QUANTITY} adet ekleyebilirsiniz.`
                  )
                  return q
                }
                return q + 1
              })
            }
          >
            <PlusIcon />
          </Button>
        </div>
        <Button
          type="button"
          onClick={() => {
            addItem(
              {
                id: product.id,
                name: product.name,
                price: product.price,
                imageUrl: product.imageUrl,
              },
              quantity
            )
            toast.success(`${product.name} sepete eklendi.`)
            setQuantity(1)
          }}
        >
          <ShoppingCartIcon /> Sepete Ekle
        </Button>
      </div>
    </div>
  )
}
