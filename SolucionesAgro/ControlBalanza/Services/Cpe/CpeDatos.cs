namespace ControlBalanza.Services.Cpe
{
    /// <summary>
    /// Datos de una Carta de Porte Electrónica leídos del QR, del texto del PDF o por OCR de una foto.
    /// Todos opcionales: se usan para precompletar el formulario, el operario confirma.
    /// </summary>
    public class CpeDatos
    {
        /// <summary>Formato impreso: sucursal (5) - número de orden (8), ej: 00001-00001665.</summary>
        public string? NumeroCPE { get; set; }
        /// <summary>Código de Trazabilidad de Granos, 11 dígitos (es lo único que trae el QR).</summary>
        public string? Ctg { get; set; }
        public string? PatenteCamion { get; set; }
        public string? PatenteAcoplado { get; set; }
        public string? NombreChofer { get; set; }
        public string? CuitProveedor { get; set; }
        public string? RazonSocialProveedor { get; set; }
        public string? Producto { get; set; }
        /// <summary>Peso neto de carga declarado en la CPE (sección B).</summary>
        public double? KilosDeclarados { get; set; }
        /// <summary>N° de planta (RUCA) del destino, para verificar que el camión viene a esta planta.</summary>
        public int? PlantaDestino { get; set; }
        public DateTime? Vencimiento { get; set; }

        // Completados por CpeIngresoService al buscar en la base
        public int? ProveedorId { get; set; }
        public int? ProductoId { get; set; }

        /// <summary>"QR", "PDF" (texto del PDF) u "OCR" (foto o PDF escaneado).</summary>
        public string Origen { get; set; } = string.Empty;

        /// <summary>Avisos para el operario (campos no encontrados, proveedor inexistente, etc.).</summary>
        public List<string> Advertencias { get; set; } = new();

        public bool TieneDatos =>
            !string.IsNullOrWhiteSpace(NumeroCPE) || !string.IsNullOrWhiteSpace(Ctg) ||
            !string.IsNullOrWhiteSpace(PatenteCamion) || !string.IsNullOrWhiteSpace(CuitProveedor) ||
            KilosDeclarados.HasValue;
    }
}
