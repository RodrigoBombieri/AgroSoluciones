using ControlBalanza.Data;
using ControlBalanza.Models;
using ControlBalanza.Validacion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ControlBalanza.Services.Cpe
{
    /// <summary>
    /// Une los datos leídos de la CPE (QR, PDF u OCR) con la base y la configuración:
    /// busca el proveedor por CUIT y el producto por nombre, cruza el CTG del QR con el impreso,
    /// y avisa si la CPE está vencida, es para otra planta o ya fue ingresada.
    /// </summary>
    public class CpeIngresoService
    {
        private readonly AgroDbContext _context;
        private readonly BalanzaOptions _opciones;
        private readonly TimeProvider _reloj;

        public CpeIngresoService(AgroDbContext context, IOptions<BalanzaOptions> opciones, TimeProvider reloj)
        {
            _context = context;
            _opciones = opciones.Value;
            _reloj = reloj;
        }

        /// <param name="ctgQr">CTG leído del QR de la misma CPE (si se pudo leer), para cruzarlo con el impreso.</param>
        public async Task<CpeDatos> CompletarAsync(CpeDatos datos, string? ctgQr = null)
        {
            CruzarCtgDelQr(datos, ctgQr);

            if (!string.IsNullOrWhiteSpace(datos.CuitProveedor))
            {
                var digitos = Cuit.SoloDigitos(datos.CuitProveedor);
                // Se compara por dígitos para no depender de cómo se cargaron los guiones
                var proveedores = await _context.Proveedores.AsNoTracking()
                    .Select(p => new { p.Id, p.CUIT })
                    .ToListAsync();
                var proveedor = proveedores.FirstOrDefault(p => Cuit.SoloDigitos(p.CUIT) == digitos);

                if (proveedor != null) datos.ProveedorId = proveedor.Id;
                else datos.Advertencias.Add(
                    $"No hay un proveedor con CUIT {datos.CuitProveedor}{(datos.RazonSocialProveedor != null ? $" ({datos.RazonSocialProveedor})" : "")}. Dé de alta el proveedor o elíjalo manualmente.");
            }

            if (!string.IsNullOrWhiteSpace(datos.Producto))
            {
                var buscado = SinAcentos(datos.Producto).ToLowerInvariant();
                var productos = await _context.Productos.AsNoTracking().ToListAsync();
                var producto = productos.FirstOrDefault(p => SinAcentos(p.Nombre).ToLowerInvariant() == buscado)
                            ?? productos.FirstOrDefault(p => buscado.Contains(SinAcentos(p.Nombre).ToLowerInvariant()));

                if (producto != null) datos.ProductoId = producto.Id;
                else datos.Advertencias.Add($"No se encontró el producto \"{datos.Producto}\". Elíjalo manualmente.");
            }

            // Controles de la CPE
            if (datos.Vencimiento.HasValue && datos.Vencimiento.Value < _reloj.GetLocalNow().DateTime)
                datos.Advertencias.Add($"⚠ La CPE está VENCIDA (venció el {datos.Vencimiento:dd/MM/yyyy HH:mm}).");

            if (_opciones.NumeroPlantaRuca.HasValue && datos.PlantaDestino.HasValue && datos.PlantaDestino != _opciones.NumeroPlantaRuca)
                datos.Advertencias.Add($"⚠ La CPE tiene como destino la planta {datos.PlantaDestino}, no esta planta ({_opciones.NumeroPlantaRuca}).");

            if (!string.IsNullOrWhiteSpace(datos.Ctg))
            {
                var previo = await _context.Pesajes.AsNoTracking()
                    .Where(p => p.Ctg == datos.Ctg)
                    .Select(p => new { p.NumeroCPE, p.FechaEntrada, p.Estado })
                    .FirstOrDefaultAsync();
                if (previo != null)
                    datos.Advertencias.Add($"⚠ Esta CPE (CTG {datos.Ctg}) ya fue ingresada el {previo.FechaEntrada:dd/MM/yyyy HH:mm}.");
            }

            return datos;
        }

        private static void CruzarCtgDelQr(CpeDatos datos, string? ctgQr)
        {
            if (string.IsNullOrWhiteSpace(ctgQr)) return;
            var qr = Ctg.Normalizar(ctgQr);
            if (!Ctg.EsValido(qr)) return;

            if (datos.Ctg == null)
            {
                datos.Ctg = qr;
            }
            else if (datos.Ctg != qr)
            {
                // El QR es una lectura exacta; el impreso puede venir del OCR. Igual puede tratarse de un documento adulterado.
                datos.Advertencias.Add($"⚠ El CTG del QR ({qr}) NO coincide con el impreso ({datos.Ctg}). Verifique la CPE antes de ingresarla; se usó el del QR.");
                datos.Ctg = qr;
            }
        }

        private static string SinAcentos(string texto) =>
            new string(texto.Normalize(System.Text.NormalizationForm.FormD)
                .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                .ToArray());
    }
}
