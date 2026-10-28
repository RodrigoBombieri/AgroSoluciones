using ControlBalanza.Data;
using ControlBalanza.Validacion;
using Microsoft.EntityFrameworkCore;

namespace ControlBalanza.Services.Cpe
{
    /// <summary>
    /// Une los datos leídos de la CPE (QR o IA) con la base: busca el proveedor por CUIT y el producto por nombre.
    /// </summary>
    public class CpeIngresoService
    {
        private readonly AgroDbContext _context;

        public CpeIngresoService(AgroDbContext context) => _context = context;

        public async Task<CpeDatos> CompletarAsync(CpeDatos datos)
        {
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

            // Si la CPE no trae número pero sí CTG, se usa el CTG como identificador
            if (string.IsNullOrWhiteSpace(datos.NumeroCPE) && !string.IsNullOrWhiteSpace(datos.Ctg))
            {
                datos.NumeroCPE = datos.Ctg;
                datos.Advertencias.Add("Se usó el CTG como número de CPE.");
            }

            return datos;
        }

        private static string SinAcentos(string texto) =>
            new string(texto.Normalize(System.Text.NormalizationForm.FormD)
                .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                .ToArray());
    }
}
