namespace BarberiaTurnos.Controllers;

public class ClientesController : Controller
{
    private readonly ApplicationDbContext _db;

    public ClientesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q)
    {
        var consulta = _db.Clientes.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            consulta = consulta.Where(c => c.Nombre.Contains(q) || c.Telefono.Contains(q));
        }
        ViewData["q"] = q;
        return View(await consulta.OrderBy(c => c.Nombre).ToListAsync());
    }

    [HttpGet]
    public IActionResult Create() => View("Formulario", new Cliente());

    [HttpPost]
    public async Task<IActionResult> Create([Bind("Nombre,Telefono")] Cliente cliente)
    {
        if (!ModelState.IsValid) return View("Formulario", cliente);
        cliente.Nombre = cliente.Nombre.Trim();
        cliente.FechaRegistro = DateTime.Now;
        _db.Clientes.Add(cliente);
        await _db.SaveChangesAsync();
        TempData["Exito"] = "Cliente registrado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var cliente = await _db.Clientes.FindAsync(id);
        return cliente is null ? NotFound() : View("Formulario", cliente);
    }

    [HttpPost]
    public async Task<IActionResult> Edit([Bind("Id,Nombre,Telefono")] Cliente cliente)
    {
        if (!ModelState.IsValid) return View("Formulario", cliente);
        var existente = await _db.Clientes.FindAsync(cliente.Id);
        if (existente is null) return NotFound();
        existente.Nombre = cliente.Nombre.Trim();
        existente.Telefono = cliente.Telefono;
        await _db.SaveChangesAsync();
        TempData["Exito"] = "Cliente actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var cliente = await _db.Clientes.FindAsync(id);
        if (cliente is null) return NotFound();

        if (await _db.Turnos.AnyAsync(t => t.ClienteId == id) || await _db.Ventas.AnyAsync(v => v.ClienteId == id))
        {
            TempData["Error"] = "No se puede eliminar el cliente porque tiene turnos o ventas asociados.";
            return RedirectToAction(nameof(Index));
        }

        _db.Clientes.Remove(cliente);
        await _db.SaveChangesAsync();
        TempData["Exito"] = "Cliente eliminado.";
        return RedirectToAction(nameof(Index));
    }
}
