import { apiRequest } from './apiClient.ts'

export type HealthResponse = {
  application: string
  database: string
}

export function getHealth(signal?: AbortSignal): Promise<HealthResponse> {
  return apiRequest<HealthResponse>('/health', { signal })
}
