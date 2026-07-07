import { Link, useNavigate } from "react-router-dom"
import { ProductImage } from "@/components/ProductImage"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { priceFormatter } from "@/features/customer/ProductCard"
import { useCreateOrder } from "@/hooks/useOrders"
import { useCartStore } from "@/stores/cart.store"

export function CheckoutPage() {
  const navigate = useNavigate()
  const items = useCartStore((state) => state.items)
  const clear = useCartStore((state) => state.clear)
  const createOrder = useCreateOrder()

  const total = items.reduce((sum, item) => sum + item.price * item.quantity, 0)

  if (items.length === 0) {
    return (
      <div className="flex flex-col items-center gap-3 py-12 text-center">
        <p className="text-muted-foreground">Sepetiniz boş.</p>
        <Link to="/products" className="text-accent hover:underline">
          Alışverişe devam et
        </Link>
      </div>
    )
  }

  const handleConfirm = () => {
    createOrder.mutate(
      { items: items.map((item) => ({ productId: item.productId, quantity: item.quantity })) },
      {
        onSuccess: () => {
          clear()
          navigate("/orders")
        },
      }
    )
  }

  return (
    <div className="flex max-w-xl flex-col gap-4">
      <h1 className="text-2xl font-semibold tracking-tight">
        Siparişi Onayla
      </h1>

      <Card>
        <CardContent className="flex flex-col gap-3">
          {items.map((item) => (
            <div key={item.productId} className="flex items-center gap-2 text-sm">
              <ProductImage
                src={item.imageUrl}
                alt={item.name}
                className="h-10 w-10 shrink-0 rounded-md"
                iconClassName="size-4"
              />
              <span className="flex-1">
                {item.name} × {item.quantity}
              </span>
              <span className="font-medium">
                {priceFormatter.format(item.price * item.quantity)}
              </span>
            </div>
          ))}
          <div className="flex items-center justify-between border-t border-border pt-3 text-base font-semibold">
            <span>Toplam</span>
            <span>{priceFormatter.format(total)}</span>
          </div>
        </CardContent>
      </Card>

      <Button
        type="button"
        onClick={handleConfirm}
        disabled={createOrder.isPending}
      >
        {createOrder.isPending ? "Sipariş oluşturuluyor..." : "Siparişi Onayla"}
      </Button>
    </div>
  )
}
