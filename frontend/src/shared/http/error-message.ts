import { HttpErrorResponse } from '@angular/common/http';
export function errorMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 401) return 'Autentificarea este necesară. Verifică adresa și parola.';
    if (error.status === 0) return 'Nu putem contacta serverul. Verifică dacă API-ul este pornit.';
    return error.error?.detail || 'Operațiunea nu a putut fi finalizată. Încearcă din nou.';
  }
  return 'A apărut o eroare. Încearcă din nou.';
}
