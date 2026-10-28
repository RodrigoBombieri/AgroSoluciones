import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Camion } from './camion.model';

@Injectable({ providedIn: 'root' })
export class PlayonService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/playon/camiones';

  listar(): Observable<Camion[]> {
    return this.http.get<Camion[]>(this.base);
  }

  iniciarDescarga(id: number, siloDestino: string | null): Observable<Camion> {
    return this.http.post<Camion>(`${this.base}/${id}/iniciar-descarga`, { siloDestino });
  }

  finalizarDescarga(id: number, observaciones: string | null): Observable<Camion> {
    return this.http.post<Camion>(`${this.base}/${id}/finalizar-descarga`, { observaciones });
  }
}

/** Traduce un error HTTP a un mensaje para el operario. */
export function mensajeDeError(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    switch (error.status) {
      case 0:
        return 'Sin conexión con el servidor. Se reintentará automáticamente.';
      case 401:
        return 'La sesión expiró. Recargá la página para volver a ingresar.';
      case 403:
        return 'No tenés permiso para operar el playón.';
      case 400:
        return 'La sesión de seguridad venció. Recargá la página.';
      case 409:
        return error.error?.detail ?? 'El camión cambió de estado. Se actualizó la lista.';
    }
    return error.error?.detail ?? `Error del servidor (${error.status}).`;
  }
  return 'Error inesperado.';
}
