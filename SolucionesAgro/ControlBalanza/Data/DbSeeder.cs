using ControlBalanza.Models;
using Microsoft.EntityFrameworkCore;

namespace ControlBalanza.Data
{
    public class DbSeeder
    {
        private readonly AgroDbContext _context;

        public DbSeeder(AgroDbContext context)
        {
            _context = context;
        }

        public void Seed()
        {
            // Aplicar migraciones pendientes
            _context.Database.Migrate();

            // Sembrar Productos si no existen
            if (!_context.Productos.Any())
            {
                var productos = new List<Producto>
                {
                    new Producto { Nombre = "Maíz", Variedad = "Común" },
                    new Producto { Nombre = "Soja", Variedad = "RR" },
                    new Producto { Nombre = "Trigo", Variedad = "Panadero" }
                };

                _context.Productos.AddRange(productos);
                _context.SaveChanges();
            }

            // Sembrar Proveedores si no existen
            if (!_context.Proveedores.Any())
            {
                var proveedores = new List<Proveedor>
                {
                    new Proveedor { RazonSocial = "Productora Uno", CUIT = "20-12345678-9", Localidad = "Rosario", Telefono = "341-1234567" },
                    new Proveedor { RazonSocial = "Distribuidora Dos", CUIT = "27-87654321-0", Localidad = "Venado Tuerto", Telefono = "3462-765432" }
                };

                _context.Proveedores.AddRange(proveedores);
                _context.SaveChanges();
            }
        }
    }
}
