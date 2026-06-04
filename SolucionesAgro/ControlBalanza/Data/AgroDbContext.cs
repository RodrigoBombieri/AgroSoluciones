using ControlBalanza.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ControlBalanza.Data
{
    public class AgroDbContext : IdentityDbContext
    {
        public AgroDbContext(DbContextOptions options): base(options) { }

        public DbSet<Pesaje> Pesajes { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Producto> Productos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurar columna computada PesoNeto como la resta entre PesoBruto y PesoTara
            modelBuilder.Entity<Pesaje>()
                .Property(p => p.PesoNeto)
                .HasComputedColumnSql("[PesoBruto] - [PesoTara]", stored: true);
        }
    }
}
