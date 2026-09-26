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

export async function apiRequest<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  headers.set('Accept', 'application/json')
  if (init.body !== undefined && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  const response = await fetch(`${getApiBaseUrl()}${path}`, { ...init, headers })
  const body = await readJson(response)

  if (!response.ok) {
    throw new ApiError(
      response.status,
      isApiErrorBody(body)
        ? body
        : {
            code: 'HTTP_ERROR',
            message: `Request failed with status ${response.status}.`,
            errors: null,
          },
    )
  }

  return body as T
}
