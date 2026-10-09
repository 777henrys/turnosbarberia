namespace BarberiaTurnos.Tests;

public class TurnoServiceTests
{
    [Fact]
    public async Task Crear_TurnoValido_SeRegistraComoPendiente()
    {
        using var t = new TestDb();
        var c = t.NuevoCliente(); var b = t.NuevoBarbero(); var s = t.NuevoServicio();
        var svc = new TurnoService(t.Db);

        var r = await svc.CrearAsync(c.Id, b.Id, s.Id, TestDb.Manana(10), null);

        Assert.True(r.Exito, r.Mensaje);
        var turno = await t.Db.Turnos.SingleAsync();
        Assert.Equal(EstadoTurno.Pendiente, turno.Estado);
        Assert.Equal(30, turno.DuracionMinutos);
    }

    [Fact]
    public async Task Crear_EnFechaPasada_SeRechaza()
    {
        using var t = new TestDb();
        var c = t.NuevoCliente(); var b = t.NuevoBarbero(); var s = t.NuevoServicio();
        var svc = new TurnoService(t.Db);

        var r = await svc.CrearAsync(c.Id, b.Id, s.Id, DateTime.Now.AddHours(-2), null);

        Assert.False(r.Exito);
        Assert.Empty(t.Db.Turnos);
    }

    [Theory]
    [InlineData(10, 0)]   // misma hora exacta
    [InlineData(10, 15)]  // empieza dentro del turno existente (10:00-10:30)
    [InlineData(9, 45)]   // termina dentro del turno existente (9:45-10:15)
    public async Task Crear_ConSolapamientoMismoBarbero_SeRechaza(int hora, int minuto)
    {
        using var t = new TestDb();
        var c = t.NuevoCliente(); var b = t.NuevoBarbero(); var s = t.NuevoServicio(duracion: 30);
        var svc = new TurnoService(t.Db);
        Assert.True((await svc.CrearAsync(c.Id, b.Id, s.Id, TestDb.Manana(10), null)).Exito);

        var r = await svc.CrearAsync(c.Id, b.Id, s.Id, TestDb.Manana(hora, minuto), null);

        Assert.False(r.Exito);
        Assert.Contains("no disponible", r.Mensaje);
        Assert.Equal(1, await t.Db.Turnos.CountAsync());
    }

    [Fact]
    public async Task Crear_HorarioContiguo_SePermite()
    {
        using var t = new TestDb();
        var c = t.NuevoCliente(); var b = t.NuevoBarbero(); var s = t.NuevoServicio(duracion: 30);
        var svc = new TurnoService(t.Db);
        await svc.CrearAsync(c.Id, b.Id, s.Id, TestDb.Manana(10), null);

        var r = await svc.CrearAsync(c.Id, b.Id, s.Id, TestDb.Manana(10, 30), null);

        Assert.True(r.Exito, r.Mensaje);
    }

    [Fact]
    public async Task Crear_MismoHorarioConOtroBarbero_SePermite()
    {
        using var t = new TestDb();
        var c = t.NuevoCliente(); var b1 = t.NuevoBarbero("B1"); var b2 = t.NuevoBarbero("B2"); var s = t.NuevoServicio();
        var svc = new TurnoService(t.Db);
        await svc.CrearAsync(c.Id, b1.Id, s.Id, TestDb.Manana(10), null);

        var r = await svc.CrearAsync(c.Id, b2.Id, s.Id, TestDb.Manana(10), null);

        Assert.True(r.Exito, r.Mensaje);
    }

    [Fact]
    public async Task Crear_ConBarberoInactivo_SeRechaza()
    {
        using var t = new TestDb();
        var c = t.NuevoCliente(); var b = t.NuevoBarbero(activo: false); var s = t.NuevoServicio();

        var r = await new TurnoService(t.Db).CrearAsync(c.Id, b.Id, s.Id, TestDb.Manana(10), null);

        Assert.False(r.Exito);
    }

    [Fact]
    public async Task Cancelar_LiberaElHorario()
    {
        using var t = new TestDb();
        var c = t.NuevoCliente(); var b = t.NuevoBarbero(); var s = t.NuevoServicio();
        var svc = new TurnoService(t.Db);
        var creado = await svc.CrearAsync(c.Id, b.Id, s.Id, TestDb.Manana(10), null);

        var cancelado = await svc.CancelarAsync(creado.Id!.Value);
        var nuevo = await svc.CrearAsync(c.Id, b.Id, s.Id, TestDb.Manana(10), null);

        Assert.True(cancelado.Exito);
        Assert.True(nuevo.Exito, nuevo.Mensaje);
        Assert.Equal(EstadoTurno.Cancelado, (await t.Db.Turnos.FindAsync(creado.Id))!.Estado);
    }

    [Fact]
    public async Task Cancelar_TurnoCompletado_SeRechaza()
    {
        using var t = new TestDb();
        var c = t.NuevoCliente(); var b = t.NuevoBarbero(); var s = t.NuevoServicio();
        var svc = new TurnoService(t.Db);
        var creado = await svc.CrearAsync(c.Id, b.Id, s.Id, TestDb.Manana(10), null);
        await svc.CambiarEstadoAsync(creado.Id!.Value, EstadoTurno.Completado);

        var r = await svc.CancelarAsync(creado.Id!.Value);

        Assert.False(r.Exito);
    }

    [Fact]
    public async Task Reprogramar_AHorarioLibre_ActualizaElTurno()
    {
        using var t = new TestDb();
        var c = t.NuevoCliente(); var b = t.NuevoBarbero(); var s = t.NuevoServicio();
        var svc = new TurnoService(t.Db);
        var creado = await svc.CrearAsync(c.Id, b.Id, s.Id, TestDb.Manana(10), null);

        var r = await svc.ReprogramarAsync(creado.Id!.Value, b.Id, TestDb.Manana(15));

        Assert.True(r.Exito, r.Mensaje);
        Assert.Equal(TestDb.Manana(15), (await t.Db.Turnos.FindAsync(creado.Id))!.FechaHoraInicio);
    }

    [Fact]
    public async Task Reprogramar_AHorarioOcupado_SeRechaza()
    {
        using var t = new TestDb();
        var c = t.NuevoCliente(); var b = t.NuevoBarbero(); var s = t.NuevoServicio();
        var svc = new TurnoService(t.Db);
        var a = await svc.CrearAsync(c.Id, b.Id, s.Id, TestDb.Manana(10), null);
        await svc.CrearAsync(c.Id, b.Id, s.Id, TestDb.Manana(11), null);

        var r = await svc.ReprogramarAsync(a.Id!.Value, b.Id, TestDb.Manana(11, 15));

        Assert.False(r.Exito);
    }

    [Fact]
    public async Task Reprogramar_DesplazandoElPropioTurno_NoCuentaComoConflicto()
    {
        using var t = new TestDb();
        var c = t.NuevoCliente(); var b = t.NuevoBarbero(); var s = t.NuevoServicio();
        var svc = new TurnoService(t.Db);
        var creado = await svc.CrearAsync(c.Id, b.Id, s.Id, TestDb.Manana(10), null);

        var r = await svc.ReprogramarAsync(creado.Id!.Value, b.Id, TestDb.Manana(10, 15));

        Assert.True(r.Exito, r.Mensaje);
    }
}
