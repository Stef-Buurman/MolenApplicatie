import type { ApiErrorResult, ApiSuccessResult } from 'typedapi-client-helpers';

export function handleGoodResult<T>(
  _response: ApiSuccessResult<T>,
): void | Promise<void> {
  // Add your default success handling here.
}

export function handleErrors<T>(
  _error: ApiErrorResult<T>,
): void | Promise<void> {
  // Add your default error handling here.
}

export const unknownErrorMessage = 'An unknown error occurred.';
