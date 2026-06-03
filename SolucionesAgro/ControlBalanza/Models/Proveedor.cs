namespace ControlBalanza.Models
{
    public class Proveedor
    {
        public int Id { get; set; }
        public string RazonSocial { get; set; }
        public string CUIT { get; set; }
        public string Localidad { get; set; }
        public string Telefono { get; set; }
        public ICollection<Pesaje> Pesajes { get; set; }
    }
}
