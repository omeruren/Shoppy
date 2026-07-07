import { zodResolver } from "@hookform/resolvers/zod"
import { useEffect } from "react"
import { Controller, useForm } from "react-hook-form"
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
import { Textarea } from "@/components/ui/textarea"
import { useCategoryOptions } from "@/hooks/useCategories"
import { useCreateProduct, useUpdateProduct } from "@/hooks/useProducts"
import type { ProductResultDto } from "@/types/product.types"

const productSchema = z.object({
  name: z.string().min(1, "İsim zorunludur."),
  description: z.string().optional(),
  imageUrl: z
    .string()
    .max(2048, "Görsel URL'si 2048 karakterden uzun olamaz.")
    .optional(),
  price: z
    .string()
    .min(1, "Fiyat zorunludur.")
    .refine((value) => Number(value) > 0, "Fiyat 0'dan büyük olmalıdır."),
  categoryId: z.string().min(1, "Kategori seçin."),
})

type ProductFormValues = z.infer<typeof productSchema>

const emptyValues: ProductFormValues = {
  name: "",
  description: "",
  imageUrl: "",
  price: "",
  categoryId: "",
}

interface ProductFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  product?: ProductResultDto
}

export function ProductFormDialog({
  open,
  onOpenChange,
  product,
}: ProductFormDialogProps) {
  const isEditing = !!product
  const { categories } = useCategoryOptions()
  const createProduct = useCreateProduct()
  const updateProduct = useUpdateProduct()

  const {
    register,
    handleSubmit,
    reset,
    control,
    formState: { errors },
  } = useForm<ProductFormValues>({
    resolver: zodResolver(productSchema),
    defaultValues: emptyValues,
  })

  useEffect(() => {
    if (!open) return
    reset(
      product
        ? {
            name: product.name,
            description: product.description ?? "",
            imageUrl: product.imageUrl ?? "",
            price: product.price.toString(),
            categoryId: product.categoryId,
          }
        : emptyValues
    )
  }, [open, product, reset])

  const isPending = createProduct.isPending || updateProduct.isPending

  const onSubmit = (values: ProductFormValues) => {
    const payload = { ...values, price: Number(values.price) }

    if (isEditing) {
      updateProduct.mutate(
        { id: product.id, ...payload, rowVersion: product.rowVersion ?? undefined },
        { onSuccess: () => onOpenChange(false) }
      )
    } else {
      createProduct.mutate(payload, { onSuccess: () => onOpenChange(false) })
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{isEditing ? "Ürünü Düzenle" : "Yeni Ürün"}</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4" noValidate>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="name">İsim</Label>
            <Input id="name" aria-invalid={!!errors.name} {...register("name")} />
            {errors.name && (
              <p className="text-sm text-danger">{errors.name.message}</p>
            )}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="description">Açıklama</Label>
            <Textarea id="description" {...register("description")} />
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="imageUrl">Görsel URL'si</Label>
            <Input
              id="imageUrl"
              type="url"
              aria-invalid={!!errors.imageUrl}
              {...register("imageUrl")}
            />
            {errors.imageUrl && (
              <p className="text-sm text-danger">{errors.imageUrl.message}</p>
            )}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="price">Fiyat</Label>
            <Input
              id="price"
              type="number"
              step="0.01"
              aria-invalid={!!errors.price}
              {...register("price")}
            />
            {errors.price && (
              <p className="text-sm text-danger">{errors.price.message}</p>
            )}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="categoryId">Kategori</Label>
            <Controller
              control={control}
              name="categoryId"
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger
                    id="categoryId"
                    className="w-full"
                    aria-invalid={!!errors.categoryId}
                  >
                    <SelectValue placeholder="Kategori seçin" />
                  </SelectTrigger>
                  <SelectContent>
                    {categories.map((category) => (
                      <SelectItem key={category.id} value={category.id}>
                        {category.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
            {errors.categoryId && (
              <p className="text-sm text-danger">{errors.categoryId.message}</p>
            )}
          </div>

          <DialogFooter>
            <Button type="submit" disabled={isPending}>
              {isPending ? "Kaydediliyor..." : "Kaydet"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
