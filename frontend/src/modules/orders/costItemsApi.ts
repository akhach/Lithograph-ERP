import { apiRequest } from '../../api/apiClient.ts'

export type CostItem = {
  id: string
  category: string
  supplier: string | null
  expenseDate: string | null
  description: string | null
  amount: number
  sortOrder: number
  createdAt: string
  updatedAt: string | null
}

export type CostItemList = {
  items: CostItem[]
  totalCost: number
}

export type CostItemInput = {
  category: string
  supplier?: string
  expenseDate?: string
  description?: string
  amount: string
}

export function listCostItems(orderId: string): Promise<CostItemList> {
  return apiRequest<CostItemList>(`/api/orders/${orderId}/cost-items`)
}

export function createCostItem(orderId: string, input: CostItemInput): Promise<CostItem> {
  return apiRequest<CostItem>(`/api/orders/${orderId}/cost-items`, {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export function updateCostItem(
  orderId: string,
  costItemId: string,
  input: CostItemInput,
): Promise<CostItem> {
  return apiRequest<CostItem>(`/api/orders/${orderId}/cost-items/${costItemId}`, {
    method: 'PATCH',
    body: JSON.stringify(input),
  })
}

export function deleteCostItem(orderId: string, costItemId: string): Promise<void> {
  return apiRequest<void>(`/api/orders/${orderId}/cost-items/${costItemId}`, { method: 'DELETE' })
}

export function reorderCostItems(orderId: string, ids: string[]): Promise<CostItemList> {
  return apiRequest<CostItemList>(`/api/orders/${orderId}/cost-items/reorder`, {
    method: 'POST',
    body: JSON.stringify({ ids }),
  })
}
