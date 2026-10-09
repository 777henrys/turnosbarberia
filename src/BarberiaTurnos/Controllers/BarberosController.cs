namespace BarberiaTurnos.Controllers;

public class BarberosController : Controller
{
    private readonly ApplicationDbContext _db;

    public BarberosController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q)
    {
        var consulta = _db.Barberos.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            consulta = consulta.Where(b => b.Nombre.Contains(q) || b.Telefono.Contains(q));
        }
        ViewData["q"] = q;
        return View(await consulta.OrderBy(b => b.Nombre).ToListAsync());
    }

    [Authorize(Roles = Roles.Administrador)]
    [HttpGet]
    public IActionResult Create() => View("Formulario", new Barbero());

    [Authorize(Roles = Roles.Administrador)]
    [HttpPost]
    public async Task<IActionResult> Create([Bind("Nombre,Telefono,Activo")] Barbero barbero)
    {
        if (!ModelState.IsValid) return View("Formulario", barbero);
        barbero.Nombre = barbero.Nombre.Trim();
        _db.Barberos.Add(barbero);
        await _db.SaveChangesAsync();
        TempData["Exito"] = "Barbero registrado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roles.Administrador)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var barbero = await _db.Barberos.FindAsync(id);
        return barbero is null ? NotFound() : View("Formulario", barbero);
    }

    [Authorize(Roles = Roles.Administrador)]
    [HttpPost]
    public async Task<IActionResult> Edit([Bind("Id,Nombre,Telefono,Activo")] Barbero barbero)
    {
        if (!ModelState.IsValid) return View("Formulario", barbero);
        var existente = await _db.Barberos.FindAsync(barbero.Id);
        if (existente is null) return NotFound();
        existente.Nombre = barbero.Nombre.Trim();
        existente.Telefono = barbero.Telefono;
        existente.Activo = barbero.Activo;
        await _db.SaveChangesAsync();
        TempData["Exito"] = "Barbero actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roles.Administrador)]
    [HttpPost]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var barbero = await _db.Barberos.FindAsync(id);
        if (barbero is null) return NotFound();
        barbero.Activo = !barbero.Activo;
        await _db.SaveChangesAsync();
        TempData["Exito"] = barbero.Activo ? "Barbero activado." : "Barbero desactivado.";
        return RedirectToAction(nameof(Index));
    }
}
