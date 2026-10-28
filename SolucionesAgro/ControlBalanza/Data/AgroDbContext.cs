using ControlBalanza.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ControlBalanza.Data
{
    public class AgroDbContext : IdentityDbContext<Usuario>
    {
        public AgroDbContext(DbContextOptions<AgroDbContext> options) : base(options) { }

        public DbSet<Pesaje> Pesajes => Set<Pesaje>();
        public DbSet<Proveedor> Proveedores => Set<Proveedor>();
        public DbSet<Producto> Productos => Set<Producto>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Proveedor>(entity =>
            {
                // No puede haber dos proveedores con el mismo CUIT
                entity.HasIndex(p => p.CUIT).IsUnique();
            });

            modelBuilder.Entity<Pesaje>(entity =>
            {
                // Una CPE se ingresa una sola vez
                entity.HasIndex(p => p.NumeroCPE).IsUnique();

                // Búsquedas frecuentes del playón y los reportes
                entity.HasIndex(p => p.Estado);
                entity.HasIndex(p => p.FechaEntrada);

                // Borrar un proveedor o producto NO debe borrar el historial de pesajes
                entity.HasOne(p => p.Proveedor)
                      .WithMany(p => p.Pesajes)
                      .HasForeignKey(p => p.ProveedorId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Producto)
                      .WithMany()
                      .HasForeignKey(p => p.ProductoId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Columna computada PesoNeto = PesoBruto - PesoTara
                entity.Property(p => p.PesoNeto)
                      .HasComputedColumnSql("[PesoBruto] - [PesoTara]", stored: true);

                // Usuarios por etapa: Restrict para no borrar pesajes al eliminar un usuario
                // (y porque SQL Server no permite múltiples rutas de cascada a la misma tabla).
                entity.HasOne(p => p.UsuarioBalanzaEntrada)
                      .WithMany()
                      .HasForeignKey(p => p.UsuarioBalanzaEntradaId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .IsRequired();

                entity.HasOne(p => p.UsuarioPlayon)
                      .WithMany()
                      .HasForeignKey(p => p.UsuarioPlayonId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.UsuarioBalanzaSalida)
                      .WithMany()
                      .HasForeignKey(p => p.UsuarioBalanzaSalidaId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
