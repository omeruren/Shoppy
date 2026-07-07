import { create } from "zustand"

const CART_STORAGE_KEY = "shoppy.cart"
export const MAX_ITEM_QUANTITY = 20

export interface CartItem {
  productId: string
  name: string
  price: number
  quantity: number
  imageUrl: string | null
}

interface CartState {
  items: CartItem[]
  addItem: (
    product: { id: string; name: string; price: number; imageUrl: string | null },
    quantity: number
  ) => void
  updateQuantity: (productId: string, quantity: number) => void
  removeItem: (productId: string) => void
  clear: () => void
}

function readStoredItems(): CartItem[] {
  try {
    const raw = localStorage.getItem(CART_STORAGE_KEY)
    return raw ? (JSON.parse(raw) as CartItem[]) : []
  } catch {
    return []
  }
}

function persist(items: CartItem[]) {
  localStorage.setItem(CART_STORAGE_KEY, JSON.stringify(items))
}

export const useCartStore = create<CartState>((set, get) => ({
  items: readStoredItems(),

  addItem: (product, quantity) => {
    const items = get().items
    const existing = items.find((item) => item.productId === product.id)

    const nextItems = existing
      ? items.map((item) =>
          item.productId === product.id
            ? {
                ...item,
                quantity: Math.min(
                  item.quantity + quantity,
                  MAX_ITEM_QUANTITY
                ),
              }
            : item
        )
      : [
          ...items,
          {
            productId: product.id,
            name: product.name,
            price: product.price,
            quantity: Math.min(quantity, MAX_ITEM_QUANTITY),
            imageUrl: product.imageUrl,
          },
        ]

    persist(nextItems)
    set({ items: nextItems })
  },

  updateQuantity: (productId, quantity) => {
    const nextItems = get().items.map((item) =>
      item.productId === productId
        ? { ...item, quantity: Math.min(Math.max(quantity, 1), MAX_ITEM_QUANTITY) }
        : item
    )
    persist(nextItems)
    set({ items: nextItems })
  },

  removeItem: (productId) => {
    const nextItems = get().items.filter((item) => item.productId !== productId)
    persist(nextItems)
    set({ items: nextItems })
  },

  clear: () => {
    persist([])
    set({ items: [] })
  },
}))
