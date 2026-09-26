export type ApiErrorBody = {
  code: string
  message: string
  errors: Record<string, string[]> | null
}

export class ApiError extends Error {
  readonly status: number
  readonly code: string
  readonly errors: Record<string, string[]> | null

  constructor(status: number, body: ApiErrorBody) {
    super(body.message)
    this.name = 'ApiError'
    this.status = status
    this.code = body.code
    this.errors = body.errors
  }
}

function getApiBaseUrl(): string {
  const baseUrl = import.meta.env.VITE_API_BASE_URL
  if (!baseUrl) {
    throw new Error(
      'API base URL configuration missing. Set VITE_API_BASE_URL (see frontend/.env.example).',
    )
  }
  return baseUrl.replace(/\/+$/, '')
}

function isApiErrorBody(value: unknown): value is ApiErrorBody {
  return (
    typeof value === 'object' &&
    value !== null &&
    typeof (value as ApiErrorBody).code === 'string' &&
    typeof (value as ApiErrorBody).message === 'string'
  )
}

async function readJson(response: Response): Promise<unknown> {
  const text = await response.text()
  return text ? (JSON.parse(text) as unknown) : null
}

let unauthorizedHandler: (() => void) | null = null

export function setUnauthorizedHandler(handler: (() => void) | null): void {
  unauthorizedHandler = handler
}

export async function apiRequest<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  headers.set('Accept', 'application/json')
  if (init.body !== undefined && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  const response = await fetch(`${getApiBaseUrl()}${path}`, {
    ...init,
    headers,
    credentials: 'include',
  })
  const body = await readJson(response)

  if (!response.ok) {
    const error = new ApiError(
      response.status,
      isApiErrorBody(body)
        ? body
        : {
            code: 'HTTP_ERROR',
            message: `Request failed with status ${response.status}.`,
            errors: null,
          },
    )
    if (
      response.status === 401 &&
      path !== '/api/auth/login' &&
      path !== '/api/auth/setup' &&
      path !== '/api/auth/setup-status'
    ) {
      unauthorizedHandler?.()
    }
    throw error
  }

  return body as T
}
