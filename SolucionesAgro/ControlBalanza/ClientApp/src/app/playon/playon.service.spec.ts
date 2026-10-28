import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { PlayonService, mensajeDeError } from './playon.service';
import { xsrfInterceptor } from '../core/xsrf.interceptor';

describe('PlayonService', () => {
  let servicio: PlayonService;
  let http: HttpTestingController;
  let meta: HTMLMetaElement;

  beforeEach(() => {
    meta = document.createElement('meta');
    meta.name = 'xsrf-token';
    meta.content = 'token-de-prueba';
    document.head.appendChild(meta);

    TestBed.configureTestingModule({
      providers: [provideHttpClient(withInterceptors([xsrfInterceptor])), provideHttpClientTesting()],
    });
    servicio = TestBed.inject(PlayonService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    meta.remove();
  });

  it('lista los camiones sin token antiforgery (GET)', () => {
    servicio.listar().subscribe();
    const req = http.expectOne('/api/playon/camiones');
    expect(req.request.method).toBe('GET');
    expect(req.request.headers.has('X-XSRF-TOKEN')).toBe(false);
    req.flush([]);
  });

  it('inicia la descarga mandando silo y token', () => {
    servicio.iniciarDescarga(7, 'Silo 3').subscribe();
    const req = http.expectOne('/api/playon/camiones/7/iniciar-descarga');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ siloDestino: 'Silo 3' });
    expect(req.request.headers.get('X-XSRF-TOKEN')).toBe('token-de-prueba');
    req.flush({});
  });

  it('finaliza la descarga mandando observaciones', () => {
    servicio.finalizarDescarga(7, null).subscribe();
    const req = http.expectOne('/api/playon/camiones/7/finalizar-descarga');
    expect(req.request.body).toEqual({ observaciones: null });
    req.flush({});
  });

  it('traduce errores HTTP a mensajes para el operario', () => {
    expect(mensajeDeError(new HttpErrorResponse({ status: 401 }))).toContain('sesión expiró');
    expect(mensajeDeError(new HttpErrorResponse({ status: 409, error: { detail: 'Ya lo movió otro' } }))).toBe('Ya lo movió otro');
    expect(mensajeDeError(new HttpErrorResponse({ status: 0 }))).toContain('Sin conexión');
  });
});
