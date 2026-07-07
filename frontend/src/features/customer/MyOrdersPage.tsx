import { ChevronLeftIcon, ChevronRightIcon } from "lucide-react"
import { ProductImage } from "@/components/ProductImage"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Skeleton } from "@/components/ui/skeleton"
import { priceFormatter } from "@/features/customer/ProductCard"
import { useOrdersQuery } from "@/hooks/useOrders"
import { useResourceListState } from "@/hooks/useResourceListState"

export function MyOrdersPage() {
  const { pagination, setPagination, queryParams } = useResourceListState({
    initialPageSize: 5,
  })
  const { data, isLoading } = useOrdersQuery(queryParams)

  const orders = data?.data?.data ?? []
  const pageCount = data?.data?.totalPageCount ?? 0

  return (
    <div className="flex flex-col gap-4">
      <h1 className="text-2xl font-semibold tracking-tight">Siparişlerim</h1>

      {isLoading ? (
        <div className="flex flex-col gap-3">
          {Array.from({ length: 3 }).map((_, index) => (
            <Skeleton key={index} className="h-32 rounded-xl" />
          ))}
        </div>
      ) : orders.length === 0 ? (
        <p className="py-8 text-center text-muted-foreground">
          Henüz siparişiniz yok.
        </p>
      ) : (
        <div className="flex flex-col gap-3">
          {orders.map((order) => {
            const total = order.items.reduce(
              (sum, item) => sum + item.unitPrice * item.quantity,
              0
            )
            return (
              <Card key={order.id}>
                <CardHeader>
                  <CardTitle className="text-sm font-medium text-muted-foreground">
                    {new Date(order.orderDate).toLocaleString("tr-TR")}
                  </CardTitle>
                </CardHeader>
                <CardContent className="flex flex-col gap-2">
                  {order.items.map((item) => (
                    <div
                      key={item.id}
                      className="flex items-center gap-2 text-sm"
                    >
                      <ProductImage
                        src={item.imageUrl}
                        alt={item.productName}
                        className="h-10 w-10 shrink-0 rounded-md"
                        iconClassName="size-4"
                      />
                      <span className="flex-1">
                        {item.productName} × {item.quantity}
                      </span>
                      <span className="font-medium">
                        {priceFormatter.format(item.unitPrice * item.quantity)}
                      </span>
                    </div>
                  ))}
                  <div className="flex items-center justify-between border-t border-border pt-2 text-sm font-semibold">
                    <span>Toplam</span>
                    <span>{priceFormatter.format(total)}</span>
                  </div>
                </CardContent>
              </Card>
            )
          })}
        </div>
      )}

      <div className="flex items-center justify-between text-sm text-text-secondary">
        <span>
          Sayfa {pagination.pageIndex + 1} / {Math.max(pageCount, 1)}
        </span>
        <div className="flex gap-1">
          <Button
            variant="outline"
            size="icon-sm"
            onClick={() =>
              setPagination((state) => ({
                ...state,
                pageIndex: Math.max(0, state.pageIndex - 1),
              }))
            }
            disabled={pagination.pageIndex === 0}
          >
            <ChevronLeftIcon />
          </Button>
          <Button
            variant="outline"
            size="icon-sm"
            onClick={() =>
              setPagination((state) => ({
                ...state,
                pageIndex: state.pageIndex + 1,
              }))
            }
            disabled={pagination.pageIndex + 1 >= pageCount}
          >
            <ChevronRightIcon />
          </Button>
        </div>
      </div>
    </div>
  )
}
