using Microsoft.Data.Sqlite;

namespace BarberiaTurnos.Tests;

/// <summary>Base de datos SQLite en memoria (una por prueba) con el mismo modelo de EF Core que la aplicación.</summary>
public sealed class TestDb : IDisposable
{
    public const string UsuarioId = "user-test-1";
    private readonly SqliteConnection _conexion;

    public ApplicationDbContext Db { get; }

    public TestDb()
    {
        _conexion = new SqliteConnection("DataSource=:memory:");
        _conexion.Open();
        var opciones = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_conexion).Options;
        Db = new ApplicationDbContext(opciones);
        Db.Database.EnsureCreated();

        Db.Users.Add(new ApplicationUser
        {
            Id = UsuarioId,
            UserName = "prueba@test.local",
            NormalizedUserName = "PRUEBA@TEST.LOCAL",
            Email = "prueba@test.local",
            NormalizedEmail = "PRUEBA@TEST.LOCAL",
            NombreCompleto = "Usuario de Prueba",
            SecurityStamp = Guid.NewGuid().ToString()
        });
        Db.Configuraciones.Add(new ConfiguracionSistema { PorcentajeImpuesto = 18m });
        Db.SaveChanges();
    }

    public Cliente NuevoCliente(string nombre = "Cliente Prueba")
    {
        var c = new Cliente { Nombre = nombre, Telefono = "809-555-0000" };
        Db.Clientes.Add(c);
        Db.SaveChanges();
        return c;
    }

    public Barbero NuevoBarbero(string nombre = "Barbero Prueba", bool activo = true)
    {
        var b = new Barbero { Nombre = nombre, Telefono = "809-555-1111", Activo = activo };
        Db.Barberos.Add(b);
        Db.SaveChanges();
        return b;
    }

    public Servicio NuevoServicio(string nombre = "Corte", decimal precio = 350m, int duracion = 30)
    {
        var s = new Servicio { Nombre = nombre, Precio = precio, DuracionMinutos = duracion };
        Db.Servicios.Add(s);
        Db.SaveChanges();
        return s;
    }

    public Producto NuevoProducto(string nombre = "Pomada", decimal precio = 100m, int cantidad = 10, int minimo = 2)
    {
        var p = new Producto { Nombre = nombre, Precio = precio, Cantidad = cantidad, StockMinimo = minimo };
        Db.Productos.Add(p);
        Db.SaveChanges();
        return p;
    }

    /// <summary>Mañana a la hora indicada (siempre en el futuro).</summary>
    public static DateTime Manana(int hora, int minuto = 0) => DateTime.Today.AddDays(1).AddHours(hora).AddMinutes(minuto);

    public void Dispose()
    {
        Db.Dispose();
        _conexion.Dispose();
    }
}
