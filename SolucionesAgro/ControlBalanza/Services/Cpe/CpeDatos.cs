namespace ControlBalanza.Services.Cpe
{
    /// <summary>
    /// Datos de una Carta de Porte Electrónica leídos del QR o extraídos por IA.
    /// Todos opcionales: se usan para precompletar el formulario, el operario confirma.
    /// </summary>
    public class CpeDatos
    {
        public string? NumeroCPE { get; set; }
        public string? Ctg { get; set; }
        public string? PatenteCamion { get; set; }
        public string? PatenteAcoplado { get; set; }
        public string? NombreChofer { get; set; }
        public string? CuitProveedor { get; set; }
        public string? RazonSocialProveedor { get; set; }
        public string? Producto { get; set; }
        public double? KilosDeclarados { get; set; }

        // Completados por CpeIngresoService al buscar en la base
        public int? ProveedorId { get; set; }
        public int? ProductoId { get; set; }

        /// <summary>"QR" o "IA".</summary>
        public string Origen { get; set; } = string.Empty;

        /// <summary>Avisos para el operario (campos no encontrados, proveedor inexistente, etc.).</summary>
        public List<string> Advertencias { get; set; } = new();

        public bool TieneDatos =>
            !string.IsNullOrWhiteSpace(NumeroCPE) || !string.IsNullOrWhiteSpace(Ctg) ||
            !string.IsNullOrWhiteSpace(PatenteCamion) || !string.IsNullOrWhiteSpace(CuitProveedor) ||
            KilosDeclarados.HasValue;
    }
}
