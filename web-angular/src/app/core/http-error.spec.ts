import { HttpErrorResponse } from '@angular/common/http';
import { API_UNREACHABLE_MESSAGE, describeHttpError } from './http-error';

describe('describeHttpError', () => {
  it('reports an unreachable API on network errors (status 0)', () => {
    expect(describeHttpError(new HttpErrorResponse({ status: 0 }))).toBe(API_UNREACHABLE_MESSAGE);
  });

  it('joins the validation errors returned by the API', () => {
    const err = new HttpErrorResponse({
      status: 400,
      error: { errors: ['Monto inválido.', 'Falta categoría.'] },
    });
    expect(describeHttpError(err)).toBe('Monto inválido. Falta categoría.');
  });

  it('falls back to a generic message with the status code', () => {
    expect(describeHttpError(new HttpErrorResponse({ status: 500 }))).toBe(
      'Ocurrió un error inesperado (código 500).',
    );
  });
});
