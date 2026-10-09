namespace BarberiaTurnos.Services;

public interface IDashboardService
{
    Task<DashboardViewModel> ObtenerAsync();
}

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _db;

    public DashboardService(ApplicationDbContext db) => _db = db;

    public async Task<DashboardViewModel> ObtenerAsync()
    {
        var hoy = DateTime.Today;
        var manana = hoy.AddDays(1);

        var turnosHoy = await _db.Turnos.AsNoTracking()
            .Include(t => t.Cliente).Include(t => t.Barbero).Include(t => t.Servicio)
            .Where(t => t.FechaHoraInicio >= hoy && t.FechaHoraInicio < manana && t.Estado != EstadoTurno.Cancelado)
            .ToListAsync();

        var totalesHoy = await _db.Ventas.AsNoTracking()
            .Where(v => v.Fecha >= hoy && v.Fecha < manana)
            .Select(v => v.Total)
            .ToListAsync();

        return new DashboardViewModel
        {
            TotalClientes = await _db.Clientes.CountAsync(),
            BarberosActivos = await _db.Barberos.CountAsync(b => b.Activo),
            TurnosPendientesHoy = turnosHoy.Count(t => t.Estado == EstadoTurno.Pendiente),
            CantidadVentasHoy = totalesHoy.Count,
            VentasHoy = totalesHoy.Sum(),
            ProductosStockBajo = await _db.Productos.AsNoTracking()
                .Where(p => p.Cantidad <= p.StockMinimo).OrderBy(p => p.Cantidad).ToListAsync(),
            TurnosDeHoy = turnosHoy.OrderBy(t => t.FechaHoraInicio).ToList()
        };
    }
}
