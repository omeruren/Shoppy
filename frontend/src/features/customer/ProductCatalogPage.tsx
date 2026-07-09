import { ChevronLeftIcon, ChevronRightIcon } from "lucide-react"
import { useEffect, useState } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Skeleton } from "@/components/ui/skeleton"
import { ProductCard } from "@/features/customer/ProductCard"
import { useCategoryOptions } from "@/hooks/useCategories"
import { useProductsQuery } from "@/hooks/useProducts"
import { useResourceListState } from "@/hooks/useResourceListState"

const PAGE_SIZE = 12

export function ProductCatalogPage() {
  const [categoryId, setCategoryId] = useState("all")

  const { pagination, setPagination, searchTerm, setSearchTerm, queryParams } =
    useResourceListState({ initialPageSize: PAGE_SIZE })

  const { categories } = useCategoryOptions()
  const { data, isLoading } = useProductsQuery({
    ...queryParams,
    categoryId: categoryId === "all" ? undefined : categoryId,
  })

  useEffect(() => {
    setPagination((current) =>
      current.pageIndex === 0 ? current : { ...current, pageIndex: 0 }
    )
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [categoryId])

  const pageItems = data?.data?.data ?? []
  const pageCount = Math.max(1, data?.data?.totalPageCount ?? 1)
  const pageIndex = pagination.pageIndex

  return (
    <div className="flex flex-col gap-4">
      <h1 className="text-2xl font-semibold tracking-tight">Ürünler</h1>

      <div className="flex flex-wrap items-center gap-2">
        <Input
          value={searchTerm}
          onChange={(event) => setSearchTerm(event.target.value)}
          placeholder="Ürün ara..."
          className="max-w-xs"
        />
        <Select value={categoryId} onValueChange={setCategoryId}>
          <SelectTrigger>
            <SelectValue placeholder="Kategori" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Tüm Kategoriler</SelectItem>
            {categories.map((category) => (
              <SelectItem key={category.id} value={category.id}>
                {category.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
        {isLoading
          ? Array.from({ length: 8 }).map((_, index) => (
              <Skeleton key={index} className="h-64 rounded-xl" />
            ))
          : pageItems.map((product) => (
              <ProductCard key={product.id} product={product} />
            ))}
      </div>

      {!isLoading && pageItems.length === 0 && (
        <p className="py-8 text-center text-muted-foreground">
          Ürün bulunamadı.
        </p>
      )}

      <div className="flex items-center justify-between text-sm text-text-secondary">
        <span>
          Sayfa {pageIndex + 1} / {pageCount}
        </span>
        <div className="flex gap-1">
          <Button
            variant="outline"
            size="icon-sm"
            onClick={() =>
              setPagination((current) => ({
                ...current,
                pageIndex: Math.max(0, current.pageIndex - 1),
              }))
            }
            disabled={pageIndex === 0}
          >
            <ChevronLeftIcon />
          </Button>
          <Button
            variant="outline"
            size="icon-sm"
            onClick={() =>
              setPagination((current) => ({
                ...current,
                pageIndex: Math.min(pageCount - 1, current.pageIndex + 1),
              }))
            }
            disabled={pageIndex + 1 >= pageCount}
          >
            <ChevronRightIcon />
          </Button>
        </div>
      </div>
    </div>
  )
}
