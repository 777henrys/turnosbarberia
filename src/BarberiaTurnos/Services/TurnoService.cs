using System.Data;

namespace BarberiaTurnos.Services;

public interface ITurnoService
{
    Task<Resultado> CrearAsync(int clienteId, int barberoId, int servicioId, DateTime inicio, string? notas);
    Task<Resultado> ReprogramarAsync(int turnoId, int barberoId, DateTime nuevoInicio);
    Task<Resultado> CancelarAsync(int turnoId);
    Task<Resultado> CambiarEstadoAsync(int turnoId, EstadoTurno nuevoEstado);
}

public class TurnoService : ITurnoService
{
    private readonly ApplicationDbContext _db;

    public TurnoService(ApplicationDbContext db) => _db = db;

    private static DateTime Normalizar(DateTime d) => new(d.Year, d.Month, d.Day, d.Hour, d.Minute, 0);

    public async Task<Resultado> CrearAsync(int clienteId, int barberoId, int servicioId, DateTime inicio, string? notas)
    {
        inicio = Normalizar(inicio);
        if (inicio < DateTime.Now)
            return Resultado.Error("No se pueden registrar turnos en fechas u horas pasadas.");

        var cliente = await _db.Clientes.FindAsync(clienteId);
        if (cliente is null) return Resultado.Error("El cliente seleccionado no existe.");

        var barbero = await _db.Barberos.FindAsync(barberoId);
        if (barbero is null) return Resultado.Error("El barbero seleccionado no existe.");
        if (!barbero.Activo) return Resultado.Error("El barbero seleccionado está inactivo.");

        var servicio = await _db.Servicios.FindAsync(servicioId);
        if (servicio is null) return Resultado.Error("El servicio seleccionado no existe.");
        if (!servicio.Activo) return Resultado.Error("El servicio seleccionado está inactivo.");

        var fin = inicio.AddMinutes(servicio.DuracionMinutos);

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var conflicto = await BuscarConflictoAsync(barberoId, inicio, fin, null);
        if (conflicto is not null)
            return Resultado.Error(MensajeConflicto(barbero.Nombre, conflicto));

        var turno = new Turno
        {
            ClienteId = clienteId,
            BarberoId = barberoId,
            ServicioId = servicioId,
            FechaHoraInicio = inicio,
            DuracionMinutos = servicio.DuracionMinutos,
            Estado = EstadoTurno.Pendiente,
            Notas = string.IsNullOrWhiteSpace(notas) ? null : notas.Trim(),
            FechaCreacion = DateTime.Now
        };
        _db.Turnos.Add(turno);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return Resultado.Ok("Turno registrado correctamente.", turno.Id);
    }

    public async Task<Resultado> ReprogramarAsync(int turnoId, int barberoId, DateTime nuevoInicio)
    {
        nuevoInicio = Normalizar(nuevoInicio);
        if (nuevoInicio < DateTime.Now)
            return Resultado.Error("No se pueden programar turnos en fechas u horas pasadas.");

        var turno = await _db.Turnos.FindAsync(turnoId);
        if (turno is null) return Resultado.Error("El turno no existe.");
        if (turno.Estado is EstadoTurno.Completado or EstadoTurno.Cancelado)
            return Resultado.Error($"No se puede reprogramar un turno {turno.Estado.ToString().ToLower()}.");

        var barbero = await _db.Barberos.FindAsync(barberoId);
        if (barbero is null) return Resultado.Error("El barbero seleccionado no existe.");
        if (!barbero.Activo) return Resultado.Error("El barbero seleccionado está inactivo.");

        var fin = nuevoInicio.AddMinutes(turno.DuracionMinutos);

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var conflicto = await BuscarConflictoAsync(barberoId, nuevoInicio, fin, turno.Id);
        if (conflicto is not null)
            return Resultado.Error(MensajeConflicto(barbero.Nombre, conflicto));

        turno.BarberoId = barberoId;
        turno.FechaHoraInicio = nuevoInicio;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return Resultado.Ok("Turno reprogramado correctamente.", turno.Id);
    }

    public async Task<Resultado> CancelarAsync(int turnoId)
    {
        var turno = await _db.Turnos.FindAsync(turnoId);
        if (turno is null) return Resultado.Error("El turno no existe.");
        if (turno.Estado == EstadoTurno.Cancelado) return Resultado.Error("El turno ya estaba cancelado.");
        if (turno.Estado == EstadoTurno.Completado) return Resultado.Error("No se puede cancelar un turno completado.");

        turno.Estado = EstadoTurno.Cancelado;
        await _db.SaveChangesAsync();
        return Resultado.Ok("Turno cancelado.", turno.Id);
    }

    public async Task<Resultado> CambiarEstadoAsync(int turnoId, EstadoTurno nuevoEstado)
    {
        if (nuevoEstado == EstadoTurno.Cancelado) return await CancelarAsync(turnoId);

        var turno = await _db.Turnos.FindAsync(turnoId);
        if (turno is null) return Resultado.Error("El turno no existe.");

        var permitido = (turno.Estado, nuevoEstado) switch
        {
            (EstadoTurno.Pendiente, EstadoTurno.Confirmado) => true,
            (EstadoTurno.Pendiente, EstadoTurno.Completado) => true,
            (EstadoTurno.Confirmado, EstadoTurno.Completado) => true,
            _ => false
        };
        if (!permitido)
            return Resultado.Error($"No se puede cambiar el turno de {turno.Estado} a {nuevoEstado}.");

        turno.Estado = nuevoEstado;
        await _db.SaveChangesAsync();
        return Resultado.Ok($"Turno marcado como {nuevoEstado}.", turno.Id);
    }

    /// <summary>
    /// Busca un turno no cancelado del mismo barbero que se solape con [inicio, fin).
    /// La comparación final se hace en memoria para no depender de funciones de fecha del proveedor.
    /// </summary>
    private async Task<Turno?> BuscarConflictoAsync(int barberoId, DateTime inicio, DateTime fin, int? excluirTurnoId)
    {
        var desde = inicio.AddDays(-1);
        var excluir = excluirTurnoId ?? 0;

        var candidatos = await _db.Turnos.AsNoTracking()
            .Where(t => t.BarberoId == barberoId
                        && t.Estado != EstadoTurno.Cancelado
                        && t.Id != excluir
                        && t.FechaHoraInicio >= desde
                        && t.FechaHoraInicio < fin)
            .ToListAsync();

        return candidatos.FirstOrDefault(t => t.FechaHoraInicio < fin && t.FechaHoraFin > inicio);
    }

    private static string MensajeConflicto(string barbero, Turno conflicto) =>
        $"Horario no disponible: {barbero} ya tiene un turno el {conflicto.FechaHoraInicio:dd/MM/yyyy} " +
        $"de {conflicto.FechaHoraInicio:HH:mm} a {conflicto.FechaHoraFin:HH:mm}. Elija otra hora u otro barbero.";
}
