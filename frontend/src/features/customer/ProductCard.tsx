import { MinusIcon, PlusIcon, ShoppingCartIcon } from "lucide-react"
import { useState } from "react"
import { Link } from "react-router-dom"
import { toast } from "sonner"
import { ProductImage } from "@/components/ProductImage"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { MAX_ITEM_QUANTITY, useCartStore } from "@/stores/cart.store"
import type { ProductResultDto } from "@/types/product.types"

export const priceFormatter = new Intl.NumberFormat("tr-TR", {
  style: "currency",
  currency: "TRY",
})

export function ProductCard({ product }: { product: ProductResultDto }) {
  const [quantity, setQuantity] = useState(1)
  const addItem = useCartStore((state) => state.addItem)

  return (
    <Card>
      <ProductImage
        src={product.imageUrl}
        alt={product.name}
        className="h-40 w-full rounded-t-xl"
      />
      <CardHeader>
        <Link to={`/products/${product.id}`}>
          <CardTitle className="hover:text-accent">{product.name}</CardTitle>
        </Link>
        <Badge variant="outline" className="w-fit">
          {product.categoryName}
        </Badge>
      </CardHeader>
      <CardContent className="flex flex-col gap-2">
        {product.description && (
          <p className="line-clamp-2 text-sm text-muted-foreground">
            {product.description}
          </p>
        )}
        <span className="text-lg font-semibold">
          {priceFormatter.format(product.price)}
        </span>
      </CardContent>
      <CardFooter className="flex items-center gap-2">
        <div className="flex items-center gap-1">
          <Button
            type="button"
            variant="outline"
            size="icon-sm"
            onClick={() => setQuantity((q) => Math.max(1, q - 1))}
          >
            <MinusIcon />
          </Button>
          <span className="w-6 text-center text-sm">{quantity}</span>
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
          className="flex-1"
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
      </CardFooter>
    </Card>
  )
}
