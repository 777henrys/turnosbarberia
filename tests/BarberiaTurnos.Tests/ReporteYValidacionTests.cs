using System.ComponentModel.DataAnnotations;
using QuestPDF.Infrastructure;

namespace BarberiaTurnos.Tests;

public class ReporteServiceTests
{
    public ReporteServiceTests() => QuestPDF.Settings.License = LicenseType.Community;

    [Fact]
    public async Task Reportes_UsanDatosRealesYGeneranPdfYExcel()
    {
        using var t = new TestDb();
        var c = t.NuevoCliente(); var b = t.NuevoBarbero(); var s = t.NuevoServicio(); var p = t.NuevoProducto(cantidad: 1, minimo: 2);
        await new TurnoService(t.Db).CrearAsync(c.Id, b.Id, s.Id, TestDb.Manana(10), null);
        await new VentaService(t.Db).RegistrarAsync(new VentaInput(Guid.NewGuid(), c.Id, new[] { new LineaVentaInput(TipoItem.Servicio, s.Id, 1) }), TestDb.UsuarioId);
        var svc = new ReporteService(t.Db);

        var turnos = await svc.TurnosPorFechaAsync(TestDb.Manana(0));
        var ventas = await svc.VentasPorPeriodoAsync(DateTime.Today, DateTime.Today);
        var productos = await svc.ProductosAsync();

        Assert.Single(turnos.Filas);
        Assert.Single(ventas.Filas);
        Assert.Single(productos.Filas);
        Assert.Contains(productos.Resumen, l => l.Contains("stock bajo") && l.EndsWith("1"));

        foreach (var datos in new[] { turnos, ventas, productos })
        {
            var pdf = svc.GenerarPdf(datos);
            var xlsx = svc.GenerarExcel(datos);
            Assert.True(pdf.Length > 500);
            Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
            Assert.True(xlsx.Length > 500);
            Assert.Equal((byte)'P', xlsx[0]);   // los .xlsx son ZIP ("PK")
            Assert.Equal((byte)'K', xlsx[1]);
        }
    }
}

public class ValidacionModeloTests
{
    private static bool EsValido(object modelo) => Validator.TryValidateObject(modelo, new ValidationContext(modelo), new List<ValidationResult>(), true);

    [Theory]
    [InlineData(0, 30, false)]
    [InlineData(-5, 30, false)]
    [InlineData(100, 0, false)]
    [InlineData(100, -10, false)]
    [InlineData(100, 30, true)]
    public void Servicio_ExigePrecioYDuracionMayoresQueCero(double precio, int duracion, bool esperado)
    {
        var s = new Servicio { Nombre = "Corte", Precio = (decimal)precio, DuracionMinutos = duracion };
        Assert.Equal(esperado, EsValido(s));
    }

    [Theory]
    [InlineData(100, -1, 0, false)]
    [InlineData(100, 5, -1, false)]
    [InlineData(0, 5, 1, false)]
    [InlineData(100, 5, 1, true)]
    public void Producto_NoPermitePreciosNiCantidadesInvalidas(double precio, int cantidad, int minimo, bool esperado)
    {
        var p = new Producto { Nombre = "Pomada", Precio = (decimal)precio, Cantidad = cantidad, StockMinimo = minimo };
        Assert.Equal(esperado, EsValido(p));
    }

    [Fact]
    public void Cliente_RequiereNombreYTelefonoValido()
    {
        Assert.False(EsValido(new Cliente { Nombre = "", Telefono = "809-555-0101" }));
        Assert.False(EsValido(new Cliente { Nombre = "Ana", Telefono = "abc" }));
        Assert.True(EsValido(new Cliente { Nombre = "Ana", Telefono = "809-555-0101" }));
    }
}
