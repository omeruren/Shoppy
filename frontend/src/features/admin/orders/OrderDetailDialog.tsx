import { zodResolver } from "@hookform/resolvers/zod"
import { useEffect } from "react"
import { Controller, useFieldArray, useForm } from "react-hook-form"
import { PlusIcon, TrashIcon } from "lucide-react"
import { z } from "zod"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { useProductOptions } from "@/hooks/useProducts"
import { useUpdateOrder } from "@/hooks/useOrders"
import type { OrderResultDto } from "@/types/order.types"

const orderItemSchema = z.object({
  id: z.string().optional(),
  productId: z.string().min(1, "Ürün seçin."),
  quantity: z.number().int().positive("Miktar 0'dan büyük olmalıdır."),
  rowVersion: z.string().optional(),
})

const orderSchema = z.object({
  items: z.array(orderItemSchema).min(1, "Sipariş en az bir ürün içermelidir."),
})

type OrderFormValues = z.infer<typeof orderSchema>

interface OrderDetailDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  order: OrderResultDto | null
}

export function OrderDetailDialog({
  open,
  onOpenChange,
  order,
}: OrderDetailDialogProps) {
  const { products } = useProductOptions()
  const updateOrder = useUpdateOrder()

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<OrderFormValues>({
    resolver: zodResolver(orderSchema),
    defaultValues: { items: [] },
  })

  const { fields, append, remove } = useFieldArray({ control, name: "items" })

  useEffect(() => {
    if (!open || !order) return
    reset({
      items: order.items.map((item) => ({
        id: item.id,
        productId: item.productId,
        quantity: item.quantity,
        rowVersion: item.rowVersion ?? undefined,
      })),
    })
  }, [open, order, reset])

  if (!order) return null

  const onSubmit = (values: OrderFormValues) => {
    updateOrder.mutate(
      {
        id: order.id,
        orderDate: order.orderDate,
        rowVersion: order.rowVersion ?? undefined,
        items: values.items,
      },
      { onSuccess: () => onOpenChange(false) }
    )
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>
            Sipariş — {new Date(order.orderDate).toLocaleString("tr-TR")}
          </DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4" noValidate>
          <div className="flex flex-col gap-3">
            {fields.map((field, index) => (
              <div key={field.id} className="flex items-start gap-2">
                <div className="flex flex-1 flex-col gap-1.5">
                  <Label>Ürün</Label>
                  <Controller
                    control={control}
                    name={`items.${index}.productId`}
                    render={({ field: selectField }) => (
                      <Select
                        value={selectField.value}
                        onValueChange={selectField.onChange}
                      >
                        <SelectTrigger className="w-full">
                          <SelectValue placeholder="Ürün seçin" />
                        </SelectTrigger>
                        <SelectContent>
                          {products.map((product) => (
                            <SelectItem key={product.id} value={product.id}>
                              {product.name}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    )}
                  />
                  {errors.items?.[index]?.productId && (
                    <p className="text-sm text-danger">
                      {errors.items[index]?.productId?.message}
                    </p>
                  )}
                </div>

                <div className="flex w-24 flex-col gap-1.5">
                  <Label>Miktar</Label>
                  <Controller
                    control={control}
                    name={`items.${index}.quantity`}
                    render={({ field: quantityField }) => (
                      <Input
                        type="number"
                        min={1}
                        value={quantityField.value}
                        onChange={(event) =>
                          quantityField.onChange(event.target.valueAsNumber)
                        }
                      />
                    )}
                  />
                </div>

                <Button
                  type="button"
                  variant="ghost"
                  size="icon-sm"
                  className="mt-6"
                  onClick={() => remove(index)}
                >
                  <TrashIcon />
                </Button>
              </div>
            ))}

            {errors.items?.root && (
              <p className="text-sm text-danger">{errors.items.root.message}</p>
            )}
          </div>

          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => append({ productId: "", quantity: 1 })}
          >
            <PlusIcon /> Ürün ekle
          </Button>

          <DialogFooter>
            <Button type="submit" disabled={updateOrder.isPending}>
              {updateOrder.isPending ? "Kaydediliyor..." : "Kaydet"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
