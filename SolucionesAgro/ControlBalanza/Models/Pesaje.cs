namespace ControlBalanza.Models
{
    public enum EstadoPesaje { EnEspera, EnDescarga, ListoSalida, Completado }
    public class Pesaje
    {
        public int Id { get; set; }
        public string NumeroCPE { get; set; }
        public string PatenteCamion { get; set; }
        public string PatenteAcoplado { get; set; }
        public string NombreChofer { get; set; }

        public double KilosDeclarados { get; set; } // Obtenidos de la CPE
        public double? PesoBruto { get; set; } // Se registra en la entrada
        public double? PesoTara { get; set; } // Se registra en la salida
        public double? PesoNeto { get; private set; } // Recordar en la DBContext mediante FluentAPI ->
                                                      // OnModelCreating ->
                                                      // .Property(p => p.PesoNeto)
                                                      // .HasComputedColumnSql("[PesoBruto] - [PesoTara], stored: true")
                                                      // 'stored: true' obliga a SQL Server a guardar el dato físicamente en el disco
        public EstadoPesaje Estado { get; set; } = EstadoPesaje.EnEspera;
        // Sale de Balanza: "EnEspera" -> Pasa a playón: "EnDescarga" ->
        // -> Sale de playón: "ListoSalida" -> Sale de Balanza: "Completado"
        
        public DateTime FechaEntrada { get; set; }
        public DateTime? FechaSalida { get; set; }

        public int UsuarioBalanzaEntradaId { get; set; } // FK al usuario que registró la entrada
        public int? UsuarioPlayonId { get; set; } // FK al usuario que registró la descarga
        public int? UsuarioBalanzaSalidaId { get; set; } // FK al usuario que registró la salida 

        public int ProveedorId { get; set; }
        public Proveedor Proveedor { get; set; }

        public int ProductoId { get; set; }
        public Producto Producto { get; set; }
    }
}
