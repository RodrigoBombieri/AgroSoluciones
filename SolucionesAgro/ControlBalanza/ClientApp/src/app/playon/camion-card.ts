import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { Camion } from './camion.model';

/** Tarjeta de un camión en el playón con la acción que corresponde a su estado. */
@Component({
  selector: 'app-camion-card',
  imports: [DecimalPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './camion-card.html',
  styleUrl: './camion-card.css',
})
export class CamionCard {
  readonly camion = input.required<Camion>();
  readonly ocupado = input(false);

  readonly iniciar = output<{ id: number; silo: string | null }>();
  readonly finalizar = output<{ id: number; observaciones: string | null }>();

  protected demora(minutos: number): string {
    return minutos >= 60 ? `${Math.floor(minutos / 60)} h ${minutos % 60} min` : `${minutos} min`;
  }

  protected onIniciar(silo: string): void {
    this.iniciar.emit({ id: this.camion().id, silo: silo.trim() || null });
  }

  protected onFinalizar(observaciones: string): void {
    this.finalizar.emit({ id: this.camion().id, observaciones: observaciones.trim() || null });
  }
}
