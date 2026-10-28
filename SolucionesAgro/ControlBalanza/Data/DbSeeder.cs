using ControlBalanza.Models;
using ControlBalanza.Seguridad;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ControlBalanza.Data
{
    public static class DbSeeder
    {
        /// <summary>Contraseña de los usuarios de prueba (solo se crean en Development).</summary>
        public const string PasswordUsuariosPrueba = "Agro1234";

        public static async Task Seed(AgroDbContext context, UserManager<Usuario> userManager,
            RoleManager<IdentityRole> roleManager, bool esDesarrollo)
        {
            // Aplica las migraciones pendientes (EnsureCreated ignoraría el historial de migraciones)
            await context.Database.MigrateAsync();

            // Roles
            foreach (var rol in Roles.Todos)
            {
                if (!await roleManager.RoleExistsAsync(rol))
                    await roleManager.CreateAsync(new IdentityRole(rol));
            }

            // Usuario administrador
            await CrearUsuarioSiNoExiste(userManager, "admin@admin.com", "Admin123", "Admin", "Sistema", Roles.Admin);

            // Un usuario por rol para probar el circuito completo (solo en desarrollo)
            if (esDesarrollo)
            {
                await CrearUsuarioSiNoExiste(userManager, "entrada@agro.local", PasswordUsuariosPrueba, "Operario", "Entrada", Roles.BalanzaEntrada);
                await CrearUsuarioSiNoExiste(userManager, "playon@agro.local", PasswordUsuariosPrueba, "Operario", "Playón", Roles.Playon);
                await CrearUsuarioSiNoExiste(userManager, "salida@agro.local", PasswordUsuariosPrueba, "Operario", "Salida", Roles.BalanzaSalida);
            }

            // Productos
            if (!await context.Productos.AnyAsync())
            {
                context.Productos.AddRange(
                    new Producto { Nombre = "Maíz", Variedad = "Común" },
                    new Producto { Nombre = "Soja", Variedad = "RR" },
                    new Producto { Nombre = "Trigo", Variedad = "Panadero" });
                await context.SaveChangesAsync();
            }

            // Proveedores (CUIT de ejemplo con dígito verificador válido)
            if (!await context.Proveedores.AnyAsync())
            {
                context.Proveedores.AddRange(
                    new Proveedor { RazonSocial = "Productora Uno", CUIT = "20-12345678-6", Localidad = "Rosario", Telefono = "341-1234567" },
                    new Proveedor { RazonSocial = "Distribuidora Dos", CUIT = "30-71234567-1", Localidad = "Venado Tuerto", Telefono = "3462-765432" });
                await context.SaveChangesAsync();
            }
        }

        private static async Task CrearUsuarioSiNoExiste(UserManager<Usuario> userManager, string email, string password,
            string nombre, string apellido, string rol)
        {
            var usuario = await userManager.FindByEmailAsync(email);
            if (usuario == null)
            {
                usuario = new Usuario
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    Nombre = nombre,
                    Apellido = apellido
                };
                var resultado = await userManager.CreateAsync(usuario, password);
                if (!resultado.Succeeded)
                    throw new InvalidOperationException($"No se pudo crear {email}: {string.Join(", ", resultado.Errors.Select(e => e.Description))}");
            }

            if (!await userManager.IsInRoleAsync(usuario, rol))
                await userManager.AddToRoleAsync(usuario, rol);
        }
    }
}
