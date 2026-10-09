namespace BarberiaTurnos.Tests;

public class VentaServiceTests
{
    private static VentaInput Entrada(Guid? token, int? clienteId, params LineaVentaInput[] lineas)
        => new(token ?? Guid.NewGuid(), clienteId, lineas);

    [Fact]
    public async Task Registrar_ConStockInsuficiente_SeRechazaYNoCambiaElInventario()
    {
        using var t = new TestDb();
        var p = t.NuevoProducto(cantidad: 2);
        var svc = new VentaService(t.Db);

        var r = await svc.RegistrarAsync(Entrada(null, null, new LineaVentaInput(TipoItem.Producto, p.Id, 3)), TestDb.UsuarioId);

        Assert.False(r.Exito);
        Assert.Contains("Stock insuficiente", r.Mensaje);
        Assert.Equal(2, (await t.Db.Productos.AsNoTracking().SingleAsync()).Cantidad);
        Assert.Empty(t.Db.Ventas);
    }

    [Fact]
    public async Task Registrar_LineasRepetidasQueSuperanElStock_SeRechaza()
    {
        using var t = new TestDb();
        var p = t.NuevoProducto(cantidad: 5);
        var svc = new VentaService(t.Db);

        var r = await svc.RegistrarAsync(Entrada(null, null,
            new LineaVentaInput(TipoItem.Producto, p.Id, 3),
            new LineaVentaInput(TipoItem.Producto, p.Id, 3)), TestDb.UsuarioId);

        Assert.False(r.Exito);
    }

    [Fact]
    public async Task Registrar_VentaValida_CalculaTotalesYDescuentaInventario()
    {
        using var t = new TestDb();
        var servicio = t.NuevoServicio(precio: 350m);
        var producto = t.NuevoProducto(precio: 100m, cantidad: 10);
        var svc = new VentaService(t.Db);

        var r = await svc.RegistrarAsync(Entrada(null, null,
            new LineaVentaInput(TipoItem.Servicio, servicio.Id, 1),
            new LineaVentaInput(TipoItem.Producto, producto.Id, 2)), TestDb.UsuarioId);

        Assert.True(r.Exito, r.Mensaje);
        var venta = await t.Db.Ventas.Include(v => v.Detalles).AsNoTracking().SingleAsync();
        Assert.Equal(550m, venta.Subtotal);          // 350 + 2 x 100
        Assert.Equal(18m, venta.PorcentajeImpuesto);
        Assert.Equal(99m, venta.Impuesto);           // 550 x 18 %
        Assert.Equal(649m, venta.Total);
        Assert.Equal(2, venta.Detalles.Count);
        Assert.Equal(TestDb.UsuarioId, venta.UsuarioId);
        Assert.Equal(8, (await t.Db.Productos.AsNoTracking().SingleAsync()).Cantidad);
    }

    [Fact]
    public async Task Registrar_UsaElPorcentajeConfigurado()
    {
        using var t = new TestDb();
        var cfg = await t.Db.Configuraciones.SingleAsync();
        cfg.PorcentajeImpuesto = 0m;
        await t.Db.SaveChangesAsync();
        var s = t.NuevoServicio(precio: 200m);

        var r = await new VentaService(t.Db).RegistrarAsync(Entrada(null, null, new LineaVentaInput(TipoItem.Servicio, s.Id, 1)), TestDb.UsuarioId);

        Assert.True(r.Exito, r.Mensaje);
        var venta = await t.Db.Ventas.AsNoTracking().SingleAsync();
        Assert.Equal(0m, venta.Impuesto);
        Assert.Equal(200m, venta.Total);
    }

    [Fact]
    public async Task Registrar_ConservaElPrecioAplicadoEnElMomentoDeLaVenta()
    {
        using var t = new TestDb();
        var p = t.NuevoProducto(precio: 100m, cantidad: 10);
        var svc = new VentaService(t.Db);
        await svc.RegistrarAsync(Entrada(null, null, new LineaVentaInput(TipoItem.Producto, p.Id, 1)), TestDb.UsuarioId);

        p.Precio = 999m;
        await t.Db.SaveChangesAsync();

        var detalle = await t.Db.DetallesVenta.AsNoTracking().SingleAsync();
        Assert.Equal(100m, detalle.PrecioUnitario);
    }

    [Fact]
    public async Task Registrar_ConElMismoToken_NoDuplicaLaVentaNiElDescuento()
    {
        using var t = new TestDb();
        var p = t.NuevoProducto(cantidad: 10);
        var svc = new VentaService(t.Db);
        var token = Guid.NewGuid();
        var linea = new LineaVentaInput(TipoItem.Producto, p.Id, 1);

        var r1 = await svc.RegistrarAsync(Entrada(token, null, linea), TestDb.UsuarioId);
        var r2 = await svc.RegistrarAsync(Entrada(token, null, linea), TestDb.UsuarioId);

        Assert.True(r1.Exito);
        Assert.True(r2.Exito);
        Assert.Equal(r1.Id, r2.Id);
        Assert.Equal(1, await t.Db.Ventas.CountAsync());
        Assert.Equal(9, (await t.Db.Productos.AsNoTracking().SingleAsync()).Cantidad);
    }

    [Fact]
    public async Task Registrar_SinLineasOConCantidadCero_SeRechaza()
    {
        using var t = new TestDb();
        var p = t.NuevoProducto();
        var svc = new VentaService(t.Db);

        Assert.False((await svc.RegistrarAsync(Entrada(null, null), TestDb.UsuarioId)).Exito);
        Assert.False((await svc.RegistrarAsync(Entrada(null, null, new LineaVentaInput(TipoItem.Producto, p.Id, 0)), TestDb.UsuarioId)).Exito);
    }

    [Fact]
    public async Task Registrar_ConServicioInactivo_SeRechazaYNoDescuentaStock()
    {
        using var t = new TestDb();
        var s = t.NuevoServicio();
        s.Activo = false;
        var p = t.NuevoProducto(cantidad: 5);
        await t.Db.SaveChangesAsync();

        var r = await new VentaService(t.Db).RegistrarAsync(Entrada(null, null,
            new LineaVentaInput(TipoItem.Producto, p.Id, 1),
            new LineaVentaInput(TipoItem.Servicio, s.Id, 1)), TestDb.UsuarioId);

        Assert.False(r.Exito);
        Assert.Empty(t.Db.Ventas);
        Assert.Equal(5, (await t.Db.Productos.AsNoTracking().SingleAsync()).Cantidad);
    }
}

public class CalculoFacturaTests
{
    [Theory]
    [InlineData(1000, 18, 180, 1180)]
    [InlineData(550, 18, 99, 649)]
    [InlineData(100, 0, 0, 100)]
    [InlineData(99.99, 18, 18.00, 117.99)]
    public void Calcular_DevuelveImpuestoYTotalCorrectos(double subtotal, double porcentaje, double impuestoEsperado, double totalEsperado)
    {
        var (impuesto, total) = CalculoFactura.Calcular((decimal)subtotal, (decimal)porcentaje);

        Assert.Equal((decimal)impuestoEsperado, impuesto);
        Assert.Equal((decimal)totalEsperado, total);
    }
}
