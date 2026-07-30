import { from, map, Observable } from 'rxjs';
import type { ApiResult } from 'typedapi-client-helpers';

export interface TypedApiRequestError {
  status: number;
  error: unknown;
  message: string;
}

export function fromTypedApi<TResponse, TError>(
  request: Promise<ApiResult<TResponse, TError>>,
): Observable<TResponse> {
  return from(request).pipe(map(unwrapTypedApiResult));
}

export function unwrapTypedApiResult<TResponse, TError>(
  result: ApiResult<TResponse, TError>,
): TResponse {
  if (result.ok) {
    return result.response;
  }

  throw createTypedApiError(result.status, result.error);
}

export function createTypedApiError(
  status: number,
  error: unknown,
): TypedApiRequestError {
  return {
    status,
    error,
    message: getTypedApiErrorMessage(error),
  };
}

export function getTypedApiErrorMessage(error: unknown): string {
  if (typeof error === 'string') return error;
  if (error instanceof Error) return error.message;

  if (error && typeof error === 'object') {
    if ('message' in error && typeof error.message === 'string') {
      return error.message;
    }

    if ('detail' in error && typeof error.detail === 'string') {
      return error.detail;
    }

    if ('title' in error && typeof error.title === 'string') {
      return error.title;
    }

    if ('body' in error) {
      return getTypedApiErrorMessage(error.body);
    }
  }

  return 'Er is een onbekende fout opgetreden.';
}
