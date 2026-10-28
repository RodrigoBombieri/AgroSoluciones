using System.ComponentModel.DataAnnotations;
using ControlBalanza.Models;

namespace ControlBalanza.ViewModels
{
    /// <summary>Filtros del historial de pesajes (vienen por querystring).</summary>
    public class FiltroPesajes
    {
        [Display(Name = "Desde")]
        [DataType(DataType.Date)]
        public DateTime? Desde { get; set; }

        [Display(Name = "Hasta")]
        [DataType(DataType.Date)]
        public DateTime? Hasta { get; set; }

        [Display(Name = "Proveedor")]
        public int? ProveedorId { get; set; }

        [Display(Name = "Producto")]
        public int? ProductoId { get; set; }

        [Display(Name = "Patente")]
        public string? Patente { get; set; }

        [Display(Name = "Nº CPE")]
        public string? NumeroCPE { get; set; }

        [Display(Name = "Estado")]
        public EstadoPesaje? Estado { get; set; }

        [Display(Name = "Solo con diferencia fuera de tolerancia")]
        public bool SoloConAlerta { get; set; }

        public int Pagina { get; set; } = 1;
    }

    public class ReporteViewModel
    {
        public FiltroPesajes Filtro { get; set; } = new();
        public IReadOnlyList<Pesaje> Pesajes { get; set; } = Array.Empty<Pesaje>();
        public int TotalRegistros { get; set; }
        public int Pagina { get; set; }
        public int TotalPaginas { get; set; }
        public double TotalNetoKg { get; set; }
        public double TotalDeclaradoKg { get; set; }
        public double Tolerancia { get; set; }
    }

    public class TotalPorProducto
    {
        public string Producto { get; set; } = string.Empty;
        public int Camiones { get; set; }
        public double NetoKg { get; set; }
    }

    public class DashboardViewModel
    {
        public int EnEspera { get; set; }
        public int EnDescarga { get; set; }
        public int ListosSalida { get; set; }
        public int IngresosHoy { get; set; }
        public int CompletadosHoy { get; set; }
        public double NetoHoyKg { get; set; }
        public double? DemoraPromedioMinutos { get; set; }
        public double Tolerancia { get; set; }
        public IReadOnlyList<TotalPorProducto> PorProductoHoy { get; set; } = Array.Empty<TotalPorProducto>();
        public IReadOnlyList<Pesaje> UltimosMovimientos { get; set; } = Array.Empty<Pesaje>();
        public IReadOnlyList<Pesaje> Alertas { get; set; } = Array.Empty<Pesaje>();
    }
}
