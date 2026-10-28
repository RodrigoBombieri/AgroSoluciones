import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { Observable } from 'rxjs';
import { Camion } from './camion.model';
import { CamionCard } from './camion-card';
import { PlayonService, mensajeDeError } from './playon.service';

/** Cada cuánto se refresca la lista (otros operarios ingresan y mueven camiones). */
export const INTERVALO_REFRESCO_MS = 15_000;

@Component({
  selector: 'app-playon-panel',
  imports: [CamionCard],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './playon-panel.html',
  styleUrl: './playon-panel.css',
})
export class PlayonPanel implements OnInit {
  private readonly servicio = inject(PlayonService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly camiones = signal<Camion[]>([]);
  protected readonly cargando = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly aviso = signal<string | null>(null);
  protected readonly ocupados = signal<ReadonlySet<number>>(new Set());
  protected readonly ultimaActualizacion = signal<Date | null>(null);

  protected readonly enEspera = computed(() => this.camiones().filter((c) => c.estado === 'EnEspera'));
  protected readonly enDescarga = computed(() => this.camiones().filter((c) => c.estado === 'EnDescarga'));

  ngOnInit(): void {
    this.cargar();
    const timer = setInterval(() => this.cargar(), INTERVALO_REFRESCO_MS);
    this.destroyRef.onDestroy(() => clearInterval(timer));
  }

  /** @param limpiarError false para conservar el mensaje de una operación fallida. */
  cargar(limpiarError = true): void {
    this.servicio.listar().subscribe({
      next: (lista) => {
        this.camiones.set(lista);
        if (limpiarError) this.error.set(null);
        this.cargando.set(false);
        this.ultimaActualizacion.set(new Date());
      },
      error: (e) => {
        this.error.set(mensajeDeError(e));
        this.cargando.set(false);
      },
    });
  }

  protected iniciar(evento: { id: number; silo: string | null }): void {
    this.operar(evento.id, this.servicio.iniciarDescarga(evento.id, evento.silo), 'Descarga iniciada');
  }

  protected finalizar(evento: { id: number; observaciones: string | null }): void {
    this.operar(evento.id, this.servicio.finalizarDescarga(evento.id, evento.observaciones),
      'Descarga finalizada: el camión pasó a balanza de salida');
  }

  private operar(id: number, accion: Observable<Camion>, mensajeOk: string): void {
    this.marcarOcupado(id, true);
    this.error.set(null);
    accion.subscribe({
      next: (actualizado) => {
        // Si terminó la descarga ya no pertenece al playón: se quita de la lista
        this.camiones.update((lista) =>
          actualizado.estado === 'EnEspera' || actualizado.estado === 'EnDescarga'
            ? lista.map((c) => (c.id === id ? actualizado : c))
            : lista.filter((c) => c.id !== id),
        );
        this.aviso.set(`${mensajeOk} (${actualizado.patenteCamion}).`);
        this.marcarOcupado(id, false);
      },
      error: (e) => {
        this.error.set(mensajeDeError(e));
        this.marcarOcupado(id, false);
        this.cargar(false); // El estado real pudo cambiar: se recarga sin ocultar el error
      },
    });
  }

  private marcarOcupado(id: number, ocupado: boolean): void {
    this.ocupados.update((actual) => {
      const nuevo = new Set(actual);
      if (ocupado) nuevo.add(id);
      else nuevo.delete(id);
      return nuevo;
    });
  }
}
