import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { PlayonPanel } from './playon-panel';
import { Camion } from './camion.model';

function camion(parcial: Partial<Camion>): Camion {
  return {
    id: 1,
    numeroCPE: '10001-00000001',
    patenteCamion: 'AB123CD',
    patenteAcoplado: null,
    nombreChofer: null,
    proveedor: 'Productora Uno',
    producto: 'Soja RR',
    kilosDeclarados: 30000,
    pesoBruto: 45000,
    estado: 'EnEspera',
    fechaEntrada: '2026-10-28T10:00:00',
    minutosEnPlanta: 35,
    siloDestino: null,
    observaciones: null,
    usuarioPlayon: null,
    ...parcial,
  };
}

describe('PlayonPanel', () => {
  let fixture: ComponentFixture<PlayonPanel>;
  let http: HttpTestingController;
  let html: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PlayonPanel],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(PlayonPanel);
    http = TestBed.inject(HttpTestingController);
    html = fixture.nativeElement;
    fixture.detectChanges();
    http.expectOne('/api/playon/camiones').flush([
      camion({ id: 1, patenteCamion: 'AB123CD', estado: 'EnEspera' }),
      camion({ id: 2, patenteCamion: 'AAA111', estado: 'EnDescarga', siloDestino: 'Silo 2' }),
    ]);
    await fixture.whenStable();
  });

  afterEach(() => fixture.destroy());

  it('separa los camiones en espera y descargando', () => {
    const columnas = html.querySelectorAll('.columna');
    expect(columnas[0].textContent).toContain('AB123CD');
    expect(columnas[1].textContent).toContain('AAA111');
    expect(columnas[1].textContent).toContain('Silo 2');
  });

  it('al iniciar la descarga manda el silo y mueve el camión de columna', async () => {
    const input = html.querySelector<HTMLInputElement>('#silo-1')!;
    input.value = 'Silo 5';
    html.querySelector<HTMLButtonElement>('.columna .btn-accion')!.click();

    const req = http.expectOne('/api/playon/camiones/1/iniciar-descarga');
    expect(req.request.body).toEqual({ siloDestino: 'Silo 5' });
    req.flush(camion({ id: 1, patenteCamion: 'AB123CD', estado: 'EnDescarga', siloDestino: 'Silo 5' }));
    await fixture.whenStable();

    const columnas = html.querySelectorAll('.columna');
    expect(columnas[0].textContent).not.toContain('AB123CD');
    expect(columnas[1].textContent).toContain('AB123CD');
    expect(html.textContent).toContain('Descarga iniciada');
  });

  it('al finalizar la descarga el camión sale del playón', async () => {
    html.querySelectorAll<HTMLButtonElement>('.btn-accion.ok')[0].click();
    http.expectOne('/api/playon/camiones/2/finalizar-descarga')
      .flush(camion({ id: 2, patenteCamion: 'AAA111', estado: 'ListoSalida' as never }));
    await fixture.whenStable();

    expect(html.textContent).not.toContain('Silo 2');
    expect(html.textContent).toContain('balanza de salida');
  });

  it('si otro operario ya movió el camión muestra el error y recarga', async () => {
    html.querySelector<HTMLButtonElement>('.columna .btn-accion')!.click();
    http.expectOne('/api/playon/camiones/1/iniciar-descarga')
      .flush({ detail: 'Solo se puede iniciar la descarga de un camión en espera' }, { status: 409, statusText: 'Conflict' });
    http.expectOne('/api/playon/camiones').flush([]);
    await fixture.whenStable();

    expect(html.querySelector('[role="alert"]')?.textContent).toContain('camión en espera');
  });
});
