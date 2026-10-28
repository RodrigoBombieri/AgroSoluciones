import { HttpInterceptorFn } from '@angular/common/http';

/**
 * Agrega el token antiforgery de ASP.NET Core a los pedidos que modifican datos.
 * La vista Razor (Views/Home/Playon.cshtml) lo deja en <meta name="xsrf-token">.
 */
export const xsrfInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.method === 'GET' || req.method === 'HEAD') {
    return next(req);
  }
  const token = document.querySelector<HTMLMetaElement>('meta[name="xsrf-token"]')?.content;
  return next(token ? req.clone({ setHeaders: { 'X-XSRF-TOKEN': token } }) : req);
};
