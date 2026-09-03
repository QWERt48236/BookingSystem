import { HttpErrorResponse } from '@angular/common/http';

export function extractErrorMessage(err: HttpErrorResponse, fallback: string): string {
  const body = err.error;
  if (Array.isArray(body) && body.every((m) => typeof m === 'string') && body.length > 0) {
    return body.join(' ');
  }
  return fallback;
}
