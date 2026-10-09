using Microsoft.AspNetCore.Identity;

namespace BarberiaTurnos.Data;

/// <summary>
/// Aplica migraciones y crea los datos mínimos: roles, configuración, cuenta de administrador
/// (solo si se configuran Seed:AdminEmail y Seed:AdminPassword mediante user-secrets o variables de entorno)
/// y, opcionalmente, datos de demostración.
/// </summary>
public static class DbInitializer
{
    public static async Task InicializarAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var config = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

        if (!db.Database.GetMigrations().Any())
        {
            throw new InvalidOperationException(
                "No existen migraciones. Ejecute primero: dotnet ef migrations add InitialCreate --project src/BarberiaTurnos " +
                "(consulte el README.md, sección 'Base de datos').");
        }

        await db.Database.MigrateAsync();

        // Roles
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var rol in Roles.Todos)
        {
            if (!await roleManager.RoleExistsAsync(rol))
                await roleManager.CreateAsync(new IdentityRole(rol));
        }

        // Configuración (impuesto inicial editable por el administrador desde el menú Configuración)
        if (!await db.Configuraciones.AnyAsync())
        {
            db.Configuraciones.Add(new ConfiguracionSistema { PorcentajeImpuesto = 18m });
            await db.SaveChangesAsync();
        }

        // Administrador inicial: la contraseña NUNCA está en el repositorio.
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var admins = await userManager.GetUsersInRoleAsync(Roles.Administrador);
        if (admins.Count == 0)
        {
            var email = config["Seed:AdminEmail"];
            var password = config["Seed:AdminPassword"];
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning("No hay administrador. Configure Seed:AdminEmail y Seed:AdminPassword con 'dotnet user-secrets' y reinicie la aplicación.");
            }
            else
            {
                var admin = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    NombreCompleto = config["Seed:AdminNombre"] ?? "Administrador"
                };
                var result = await userManager.CreateAsync(admin, password);
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, Roles.Administrador);
                else
                    logger.LogError("No se pudo crear el administrador: {Errores}", string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }

        // Datos de demostración (no sensibles), solo si se activa en la configuración.
        if (config.GetValue<bool>("Seed:CargarDatosDemo"))
            await CargarDemoAsync(db);
    }

    private static async Task CargarDemoAsync(ApplicationDbContext db)
    {
        if (!await db.Servicios.AnyAsync())
        {
            db.Servicios.AddRange(
                new Servicio { Nombre = "Corte de cabello", Precio = 350m, DuracionMinutos = 30 },
                new Servicio { Nombre = "Arreglo de barba", Precio = 200m, DuracionMinutos = 20 },
                new Servicio { Nombre = "Lavado", Precio = 150m, DuracionMinutos = 15 },
                new Servicio { Nombre = "Corte + barba", Precio = 500m, DuracionMinutos = 45 });
        }
        if (!await db.Barberos.AnyAsync())
        {
            db.Barberos.AddRange(
                new Barbero { Nombre = "Barbero Demo Uno", Telefono = "809-555-0101" },
                new Barbero { Nombre = "Barbero Demo Dos", Telefono = "809-555-0102" });
        }
        if (!await db.Clientes.AnyAsync())
        {
            db.Clientes.AddRange(
                new Cliente { Nombre = "Cliente Demo Uno", Telefono = "809-555-0201" },
                new Cliente { Nombre = "Cliente Demo Dos", Telefono = "829-555-0202" });
        }
        if (!await db.Productos.AnyAsync())
        {
            db.Productos.AddRange(
                new Producto { Nombre = "Pomada para cabello", Descripcion = "Pomada de fijación media", Precio = 450m, Cantidad = 20, StockMinimo = 5 },
                new Producto { Nombre = "Aceite para barba", Descripcion = "Aceite hidratante 30 ml", Precio = 600m, Cantidad = 12, StockMinimo = 4 },
                new Producto { Nombre = "Shampoo", Descripcion = "Shampoo de uso diario", Precio = 300m, Cantidad = 3, StockMinimo = 5 });
        }
        await db.SaveChangesAsync();
    }
}
