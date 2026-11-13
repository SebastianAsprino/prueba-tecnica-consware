import { HttpErrorResponse } from '@angular/common/http';
import { API_BASE_URL } from './api-config';

export const API_UNREACHABLE_MESSAGE = `No se pudo conectar con la API. Verifica que esté corriendo en ${new URL(API_BASE_URL).origin}.`;

/** Traduce un error HTTP del API a un mensaje legible para el usuario. */
export function describeHttpError(err: HttpErrorResponse): string {
  if (err.status === 0) {
    return API_UNREACHABLE_MESSAGE;
  }
  const apiErrors = err.error?.errors as string[] | undefined;
  if (apiErrors?.length) {
    return apiErrors.join(' ');
  }
  return `Ocurrió un error inesperado (código ${err.status}).`;
}
