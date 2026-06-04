using ControlBalanza.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ControlBalanza.Data
{
    public class DbSeeder
    {
        
        public static async Task Seed(AgroDbContext context, UserManager<Usuario> userManager, RoleManager<IdentityRole> roleManager)
        {
            context.Database.EnsureCreated();

            // Crear rol Admin si no existe
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            // Crear usuario admin si no existe
            var adminUser = await userManager.FindByEmailAsync("admin@admin.com");
            if (adminUser == null)
            {
                adminUser = new Usuario
                {
                    UserName = "admin@admin.com",
                    Email = "admin@admin.com",
                    Nombre = "Admin",
                    Apellido = "Sistema",
                    ImagenUrlPerfil = "/images/default-avatar.png"
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Sembrar Productos si no existen
            if (!context.Productos.Any())
            {
                var productos = new List<Producto>
                {
                    new Producto { Nombre = "Maíz", Variedad = "Común" },
                    new Producto { Nombre = "Soja", Variedad = "RR" },
                    new Producto { Nombre = "Trigo", Variedad = "Panadero" }
                };

                context.Productos.AddRange(productos);
                context.SaveChanges();
            }

            // Sembrar Proveedores si no existen
            if (!context.Proveedores.Any())
            {
                var proveedores = new List<Proveedor>
                {
                    new Proveedor { RazonSocial = "Productora Uno", CUIT = "20-12345678-9", Localidad = "Rosario", Telefono = "341-1234567" },
                    new Proveedor { RazonSocial = "Distribuidora Dos", CUIT = "27-87654321-0", Localidad = "Venado Tuerto", Telefono = "3462-765432" }
                };

                context.Proveedores.AddRange(proveedores);
                context.SaveChanges();
            }
        }
    }
}
