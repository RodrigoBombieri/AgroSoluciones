using ControlBalanza.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ControlBalanza.Data
{
    public class AgroDbContext : IdentityDbContext<Usuario>
    {
        public AgroDbContext(DbContextOptions<AgroDbContext> options) : base(options) { }

        public DbSet<Pesaje> Pesajes { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Producto> Productos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Pesaje>(entity =>
            {
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
