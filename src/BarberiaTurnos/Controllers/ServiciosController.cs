namespace BarberiaTurnos.Controllers;

public class ServiciosController : Controller
{
    private readonly ApplicationDbContext _db;

    public ServiciosController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q)
    {
        var consulta = _db.Servicios.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            consulta = consulta.Where(s => s.Nombre.Contains(q));
        }
        ViewData["q"] = q;
        return View(await consulta.OrderBy(s => s.Nombre).ToListAsync());
    }

    [Authorize(Roles = Roles.Administrador)]
    [HttpGet]
    public IActionResult Create() => View("Formulario", new Servicio());

    [Authorize(Roles = Roles.Administrador)]
    [HttpPost]
    public async Task<IActionResult> Create([Bind("Nombre,Precio,DuracionMinutos,Activo")] Servicio servicio)
    {
        servicio.Nombre = (servicio.Nombre ?? string.Empty).Trim();
        if (await _db.Servicios.AnyAsync(s => s.Nombre == servicio.Nombre))
            ModelState.AddModelError(nameof(Servicio.Nombre), "Ya existe un servicio con ese nombre.");
        if (!ModelState.IsValid) return View("Formulario", servicio);

        _db.Servicios.Add(servicio);
        await _db.SaveChangesAsync();
        TempData["Exito"] = "Servicio registrado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roles.Administrador)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var servicio = await _db.Servicios.FindAsync(id);
        return servicio is null ? NotFound() : View("Formulario", servicio);
    }

    [Authorize(Roles = Roles.Administrador)]
    [HttpPost]
    public async Task<IActionResult> Edit([Bind("Id,Nombre,Precio,DuracionMinutos,Activo")] Servicio servicio)
    {
        servicio.Nombre = (servicio.Nombre ?? string.Empty).Trim();
        if (await _db.Servicios.AnyAsync(s => s.Nombre == servicio.Nombre && s.Id != servicio.Id))
            ModelState.AddModelError(nameof(Servicio.Nombre), "Ya existe otro servicio con ese nombre.");
        if (!ModelState.IsValid) return View("Formulario", servicio);

        var existente = await _db.Servicios.FindAsync(servicio.Id);
        if (existente is null) return NotFound();
        existente.Nombre = servicio.Nombre;
        existente.Precio = servicio.Precio;
        existente.DuracionMinutos = servicio.DuracionMinutos;
        existente.Activo = servicio.Activo;
        await _db.SaveChangesAsync();
        TempData["Exito"] = "Servicio actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roles.Administrador)]
    [HttpPost]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var servicio = await _db.Servicios.FindAsync(id);
        if (servicio is null) return NotFound();
        servicio.Activo = !servicio.Activo;
        await _db.SaveChangesAsync();
        TempData["Exito"] = servicio.Activo ? "Servicio activado." : "Servicio desactivado.";
        return RedirectToAction(nameof(Index));
    }
}
