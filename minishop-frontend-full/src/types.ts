
export type Role = 'Client' | 'Admin';

export interface User {
  id: number;
  username: string;
  email?: string;
  role: Role;
  isAdmin?: boolean;
  token?: string; // not used, kept for future JWT
}

export interface Category {
  id: number;
  name: string;
  products?: Product[];
}

export interface Product {
  id: number;
  name: string;
  description?: string;
  imageUrl?: string;
  stock: number;
  price: number;
  categoryId: number;
  category?: Category;
}

export interface CartItemDto {
  productId: number;
  quantity: number;
}

export interface OrderItem {
  productId: number;
  quantity: number;
  price: number;
}

export interface Order {
  id: number;
  userId?: number;
  createdAt: string;
  items?: OrderItem[];
}

export interface PlaceOrderResponse {
  orderId: number;
  createdAt: string;
  itemsCount: number;
  total: number;
}

export interface SalesByCategory {
  categoryName: string;
  totalSales: number;
}
