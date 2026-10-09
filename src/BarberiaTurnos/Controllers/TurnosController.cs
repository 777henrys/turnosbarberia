namespace BarberiaTurnos.Controllers;

public class TurnosController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ITurnoService _turnos;

    public TurnosController(ApplicationDbContext db, ITurnoService turnos)
    {
        _db = db;
        _turnos = turnos;
    }

    /// <summary>Lista los turnos de un día. Si se escribe una búsqueda, filtra por cliente o barbero en todas las fechas.</summary>
    public async Task<IActionResult> Index(DateTime? fecha, string? q)
    {
        var consulta = _db.Turnos.AsNoTracking()
            .Include(t => t.Cliente).Include(t => t.Barbero).Include(t => t.Servicio)
            .AsQueryable();

        var dia = (fecha ?? DateTime.Today).Date;
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            consulta = consulta.Where(t => t.Cliente!.Nombre.Contains(q) || t.Barbero!.Nombre.Contains(q));
        }
        else
        {
            var siguiente = dia.AddDays(1);
            consulta = consulta.Where(t => t.FechaHoraInicio >= dia && t.FechaHoraInicio < siguiente);
        }

        ViewData["Fecha"] = dia;
        ViewData["q"] = q;
        var lista = await consulta.ToListAsync();
        return View(lista.OrderBy(t => t.FechaHoraInicio).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var siguienteHora = DateTime.Now.AddHours(1);
        var vm = new TurnoFormViewModel
        {
            FechaHora = new DateTime(siguienteHora.Year, siguienteHora.Month, siguienteHora.Day, siguienteHora.Hour, 0, 0)
        };
        await CargarListasAsync(vm);
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Create(TurnoFormViewModel vm)
    {
        if (ModelState.IsValid)
        {
            var resultado = await _turnos.CrearAsync(vm.ClienteId!.Value, vm.BarberoId!.Value, vm.ServicioId!.Value, vm.FechaHora!.Value, vm.Notas);
            if (resultado.Exito)
            {
                TempData["Exito"] = resultado.Mensaje;
                return RedirectToAction(nameof(Index), new { fecha = vm.FechaHora.Value.Date.ToString("yyyy-MM-dd") });
            }
            ModelState.AddModelError(string.Empty, resultado.Mensaje);
        }
        await CargarListasAsync(vm);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Reprogramar(int id)
    {
        var turno = await _db.Turnos.AsNoTracking().Include(t => t.Cliente).Include(t => t.Servicio).FirstOrDefaultAsync(t => t.Id == id);
        if (turno is null) return NotFound();
        if (turno.Estado is EstadoTurno.Completado or EstadoTurno.Cancelado)
        {
            TempData["Error"] = "Solo se pueden reprogramar turnos pendientes o confirmados.";
            return RedirectToAction(nameof(Index));
        }

        var vm = new ReprogramarViewModel
        {
            Id = turno.Id,
            BarberoId = turno.BarberoId,
            FechaHora = turno.FechaHoraInicio,
            Resumen = $"{turno.Cliente?.Nombre} - {turno.Servicio?.Nombre} ({turno.DuracionMinutos} min)"
        };
        await CargarBarberosAsync(vm);
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Reprogramar(ReprogramarViewModel vm)
    {
        if (ModelState.IsValid)
        {
            var resultado = await _turnos.ReprogramarAsync(vm.Id, vm.BarberoId!.Value, vm.FechaHora!.Value);
            if (resultado.Exito)
            {
                TempData["Exito"] = resultado.Mensaje;
                return RedirectToAction(nameof(Index), new { fecha = vm.FechaHora.Value.Date.ToString("yyyy-MM-dd") });
            }
            ModelState.AddModelError(string.Empty, resultado.Mensaje);
        }
        var turno = await _db.Turnos.AsNoTracking().Include(t => t.Cliente).Include(t => t.Servicio).FirstOrDefaultAsync(t => t.Id == vm.Id);
        vm.Resumen = turno is null ? string.Empty : $"{turno.Cliente?.Nombre} - {turno.Servicio?.Nombre} ({turno.DuracionMinutos} min)";
        await CargarBarberosAsync(vm);
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Cancelar(int id, DateTime? fecha)
    {
        var resultado = await _turnos.CancelarAsync(id);
        TempData[resultado.Exito ? "Exito" : "Error"] = resultado.Mensaje;
        return RedirectToAction(nameof(Index), new { fecha = fecha?.ToString("yyyy-MM-dd") });
    }

    [HttpPost]
    public async Task<IActionResult> CambiarEstado(int id, EstadoTurno estado, DateTime? fecha)
    {
        var resultado = await _turnos.CambiarEstadoAsync(id, estado);
        TempData[resultado.Exito ? "Exito" : "Error"] = resultado.Mensaje;
        return RedirectToAction(nameof(Index), new { fecha = fecha?.ToString("yyyy-MM-dd") });
    }

    private async Task CargarListasAsync(TurnoFormViewModel vm)
    {
        vm.Clientes = await _db.Clientes.AsNoTracking().OrderBy(c => c.Nombre)
            .Select(c => new SelectListItem(c.Nombre + " - " + c.Telefono, c.Id.ToString())).ToListAsync();
        vm.Barberos = await _db.Barberos.AsNoTracking().Where(b => b.Activo).OrderBy(b => b.Nombre)
            .Select(b => new SelectListItem(b.Nombre, b.Id.ToString())).ToListAsync();
        vm.Servicios = (await _db.Servicios.AsNoTracking().Where(s => s.Activo).OrderBy(s => s.Nombre).ToListAsync())
            .Select(s => new SelectListItem($"{s.Nombre} ({s.DuracionMinutos} min)", s.Id.ToString())).ToList();
    }

    private async Task CargarBarberosAsync(ReprogramarViewModel vm)
    {
        vm.Barberos = await _db.Barberos.AsNoTracking().Where(b => b.Activo || b.Id == vm.BarberoId).OrderBy(b => b.Nombre)
            .Select(b => new SelectListItem(b.Nombre, b.Id.ToString())).ToListAsync();
    }
}
