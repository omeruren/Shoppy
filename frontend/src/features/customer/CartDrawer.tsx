import {
  MinusIcon,
  PlusIcon,
  ShoppingCartIcon,
  Trash2Icon,
} from "lucide-react"
import { useState } from "react"
import { useNavigate } from "react-router-dom"
import { toast } from "sonner"
import { ProductImage } from "@/components/ProductImage"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Sheet,
  SheetContent,
  SheetFooter,
  SheetHeader,
  SheetTitle,
  SheetTrigger,
} from "@/components/ui/sheet"
import { priceFormatter } from "@/features/customer/ProductCard"
import { MAX_ITEM_QUANTITY, useCartStore } from "@/stores/cart.store"

export function CartDrawer() {
  const [open, setOpen] = useState(false)
  const navigate = useNavigate()
  const items = useCartStore((state) => state.items)
  const updateQuantity = useCartStore((state) => state.updateQuantity)
  const removeItem = useCartStore((state) => state.removeItem)

  const total = items.reduce((sum, item) => sum + item.price * item.quantity, 0)

  return (
    <Sheet open={open} onOpenChange={setOpen}>
      <SheetTrigger asChild>
        <Button type="button" variant="outline" size="icon" className="relative">
          <ShoppingCartIcon />
          {items.length > 0 && (
            <Badge className="absolute -top-1.5 -right-1.5 h-4 min-w-4 justify-center px-1">
              {items.length}
            </Badge>
          )}
        </Button>
      </SheetTrigger>
      <SheetContent className="flex flex-col">
        <SheetHeader>
          <SheetTitle>Sepetim</SheetTitle>
        </SheetHeader>

        <div className="flex-1 overflow-y-auto px-4">
          {items.length === 0 ? (
            <p className="text-sm text-muted-foreground">Sepetiniz boş.</p>
          ) : (
            <ul className="flex flex-col gap-4">
              {items.map((item) => (
                <li key={item.productId} className="flex items-center gap-2">
                  <ProductImage
                    src={item.imageUrl}
                    alt={item.name}
                    className="h-10 w-10 shrink-0 rounded-md"
                    iconClassName="size-4"
                  />
                  <div className="flex-1">
                    <p className="text-sm font-medium">{item.name}</p>
                    <p className="text-xs text-muted-foreground">
                      {priceFormatter.format(item.price)}
                    </p>
                  </div>
                  <div className="flex items-center gap-1">
                    <Button
                      type="button"
                      variant="outline"
                      size="icon-xs"
                      onClick={() => {
                        if (item.quantity <= 1) {
                          removeItem(item.productId)
                          toast.success(`${item.name} sepetten çıkarıldı.`)
                        } else {
                          updateQuantity(item.productId, item.quantity - 1)
                        }
                      }}
                    >
                      <MinusIcon />
                    </Button>
                    <span className="w-5 text-center text-sm">
                      {item.quantity}
                    </span>
                    <Button
                      type="button"
                      variant="outline"
                      size="icon-xs"
                      onClick={() => {
                        if (item.quantity >= MAX_ITEM_QUANTITY) {
                          toast.warning(
                            `Bir üründen en fazla ${MAX_ITEM_QUANTITY} adet ekleyebilirsiniz.`
                          )
                          return
                        }
                        updateQuantity(item.productId, item.quantity + 1)
                      }}
                    >
                      <PlusIcon />
                    </Button>
                  </div>
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon-sm"
                    onClick={() => {
                      removeItem(item.productId)
                      toast.success(`${item.name} sepetten çıkarıldı.`)
                    }}
                  >
                    <Trash2Icon />
                  </Button>
                </li>
              ))}
            </ul>
          )}
        </div>

        <SheetFooter>
          <div className="flex items-center justify-between text-sm font-semibold">
            <span>Toplam</span>
            <span>{priceFormatter.format(total)}</span>
          </div>
          <Button
            type="button"
            disabled={items.length === 0}
            onClick={() => {
              setOpen(false)
              navigate("/checkout")
            }}
          >
            Siparişi Tamamla
          </Button>
        </SheetFooter>
      </SheetContent>
    </Sheet>
  )
}
