import { apiRequest } from '../../api/apiClient.ts'
import type { OrderDetail } from './ordersApi.ts'

export type CalculatorFieldValue = number | string | boolean | null

export type CalculatorOption = {
  value: string
  label: string
  numericValue?: number | null
}

export type CalculatorColumn = {
  key: string
  label: string
  type: string
  visibility: string
  options?: CalculatorOption[] | null
}

export type CalculatorElement = {
  id: string
  type: string
  key?: string | null
  label?: string | null
  visibility?: string | null
  defaultValue?: number | null
  min?: number | null
  max?: number | null
  decimalPlaces?: number | null
  defaultText?: string | null
  defaultChecked?: boolean | null
  options?: CalculatorOption[] | null
  columns?: CalculatorColumn[] | null
}

export type CalculatorFieldError = {
  fieldKey: string
  code: string
  message: string
}

export type OrderCalculator = {
  orderId: string
  calculatorId: string
  template: { id: string; name: string }
  templateVersion: { id: string; versionNumber: number }
  sellingPriceFieldKey?: string | null
  elements: CalculatorElement[]
  values: Record<string, CalculatorFieldValue>
  calculatedValues: Record<string, CalculatorFieldValue>
  sellingPrice?: number | null
  calculationComplete: boolean
  fieldErrors: CalculatorFieldError[]
  updatedAt: string
  lastCalculatedAt?: string | null
}

export function getOrderCalculator(orderId: string): Promise<OrderCalculator> {
  return apiRequest<OrderCalculator>(`/api/orders/${orderId}/calculator`)
}

export function saveOrderCalculator(
  orderId: string,
  fieldValues: Record<string, CalculatorFieldValue>,
  updatedAt: string,
): Promise<OrderCalculator> {
  return apiRequest<OrderCalculator>(`/api/orders/${orderId}/calculator`, {
    method: 'PATCH',
    body: JSON.stringify({ fieldValues, updatedAt }),
  })
}

export function resetOrderCalculator(orderId: string, updatedAt: string): Promise<OrderCalculator> {
  return apiRequest<OrderCalculator>(`/api/orders/${orderId}/calculator/reset`, {
    method: 'POST',
    body: JSON.stringify({ updatedAt }),
  })
}

export function changeOrderType(
  orderId: string,
  input: { orderTypeId: string; resetCalculator: true; updatedAt?: string },
): Promise<OrderDetail> {
  return apiRequest<OrderDetail>(`/api/orders/${orderId}/change-order-type`, {
    method: 'POST',
    body: JSON.stringify(input),
  })
}
