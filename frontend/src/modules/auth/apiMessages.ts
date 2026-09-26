import { ApiError } from '../../api/apiClient.ts'

export function messageOf(error: unknown): string {
  if (error instanceof ApiError) {
    return error.message
  }
  if (error instanceof TypeError) {
    return 'Could not reach the server.'
  }
  return 'Something went wrong.'
}

export function fieldMessage(error: unknown, field: string): string | undefined {
  if (!(error instanceof ApiError) || !error.errors) {
    return undefined
  }
  return error.errors[field]?.[0]
}

export function passwordsMatch(password: string, confirmation: string): string | null {
  if (password.length < 8) {
    return 'Password must be at least 8 characters.'
  }
  if (password !== confirmation) {
    return 'Password confirmation does not match.'
  }
  return null
}
