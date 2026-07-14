import { createContext, ReactNode, useContext, useEffect, useMemo, useState } from "react";
import type { MenuItem } from "../models";

const cartStorageKey = "quickbite.cart";

export interface CartLine {
  menuItemId: string;
  restaurantId: string;
  name: string;
  unitPrice: number;
  quantity: number;
}

interface CartContextValue {
  items: CartLine[];
  itemCount: number;
  totalAmount: number;
  addItem: (item: MenuItem) => void;
  setQuantity: (menuItemId: string, quantity: number) => void;
  removeItem: (menuItemId: string) => void;
  clearCart: () => void;
}

const CartContext = createContext<CartContextValue | undefined>(undefined);

export function CartProvider({ children }: { children: ReactNode }) {
  const [items, setItems] = useState<CartLine[]>(() => loadStoredCart());

  useEffect(() => {
    localStorage.setItem(cartStorageKey, JSON.stringify(items));
  }, [items]);

  const value = useMemo<CartContextValue>(() => {
    const itemCount = items.reduce((sum, item) => sum + item.quantity, 0);
    const totalAmount = items.reduce((sum, item) => sum + item.quantity * item.unitPrice, 0);

    return {
      items,
      itemCount,
      totalAmount,
      addItem: (menuItem) => {
        setItems((currentItems) => {
          const existingItem = currentItems.find((item) => item.menuItemId === menuItem.id);

          if (existingItem) {
            return currentItems.map((item) =>
              item.menuItemId === menuItem.id ? { ...item, quantity: item.quantity + 1 } : item);
          }

          return [
            ...currentItems,
            {
              menuItemId: menuItem.id,
              restaurantId: menuItem.restaurantId,
              name: menuItem.name,
              unitPrice: menuItem.price,
              quantity: 1
            }
          ];
        });
      },
      setQuantity: (menuItemId, quantity) => {
        setItems((currentItems) =>
          currentItems
            .map((item) => item.menuItemId === menuItemId ? { ...item, quantity: Math.max(0, quantity) } : item)
            .filter((item) => item.quantity > 0));
      },
      removeItem: (menuItemId) => {
        setItems((currentItems) => currentItems.filter((item) => item.menuItemId !== menuItemId));
      },
      clearCart: () => setItems([])
    };
  }, [items]);

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>;
}

export function useCart() {
  const value = useContext(CartContext);

  if (!value) {
    throw new Error("useCart must be used inside CartProvider.");
  }

  return value;
}

function loadStoredCart() {
  const rawCart = localStorage.getItem(cartStorageKey);

  if (!rawCart) {
    return [];
  }

  try {
    const parsedCart = JSON.parse(rawCart);
    return Array.isArray(parsedCart) ? parsedCart.filter(isCartLine) : [];
  } catch {
    return [];
  }
}

function isCartLine(value: unknown): value is CartLine {
  return Boolean(
    typeof value === "object"
      && value !== null
      && "menuItemId" in value
      && "restaurantId" in value
      && "name" in value
      && "unitPrice" in value
      && "quantity" in value
      && typeof value.menuItemId === "string"
      && typeof value.restaurantId === "string"
      && typeof value.name === "string"
      && typeof value.unitPrice === "number"
      && Number.isFinite(value.unitPrice)
      && typeof value.quantity === "number"
      && Number.isInteger(value.quantity)
      && value.quantity > 0);
}
