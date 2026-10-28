/** Camión tal como lo devuelve GET /api/playon/camiones (ver Controllers/Api/PlayonController.cs). */
export interface Camion {
  id: number;
  numeroCPE: string;
  patenteCamion: string;
  patenteAcoplado: string | null;
  nombreChofer: string | null;
  proveedor: string | null;
  producto: string | null;
  kilosDeclarados: number;
  pesoBruto: number | null;
  estado: EstadoCamion;
  fechaEntrada: string;
  minutosEnPlanta: number;
  siloDestino: string | null;
  observaciones: string | null;
  usuarioPlayon: string | null;
}

export type EstadoCamion = 'EnEspera' | 'EnDescarga';
