import { apiRequest } from '../../api/apiClient.ts'

export type OrderType = {
  id: string
  name: string
  description: string | null
  calculatorTemplateId: string | null
  calculatorTemplateName: string | null
  isActive: boolean
  createdAt: string
  updatedAt: string | null
}

export function listOrderTypes(selector = false): Promise<OrderType[]> {
  const query = selector ? '?view=selector' : ''
  return apiRequest<OrderType[]>(`/api/order-types${query}`)
}

export function createOrderType(input: {
  name: string
  description?: string
  calculatorTemplateId?: string | null
}): Promise<OrderType> {
  return apiRequest<OrderType>('/api/order-types', { method: 'POST', body: JSON.stringify(input) })
}

export function updateOrderType(
  orderTypeId: string,
  input: { name: string; description?: string; calculatorTemplateId?: string | null },
): Promise<OrderType> {
  return apiRequest<OrderType>(`/api/order-types/${orderTypeId}`, {
    method: 'PATCH',
    body: JSON.stringify(input),
  })
}

export function activateOrderType(orderTypeId: string): Promise<OrderType> {
  return apiRequest<OrderType>(`/api/order-types/${orderTypeId}/activate`, { method: 'POST' })
}

export function deactivateOrderType(orderTypeId: string): Promise<OrderType> {
  return apiRequest<OrderType>(`/api/order-types/${orderTypeId}/deactivate`, { method: 'POST' })
}
