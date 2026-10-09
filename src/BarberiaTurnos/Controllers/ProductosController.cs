namespace BarberiaTurnos.Controllers;

public class ProductosController : Controller
{
    private readonly ApplicationDbContext _db;

    public ProductosController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q)
    {
        var consulta = _db.Productos.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            consulta = consulta.Where(p => p.Nombre.Contains(q) || (p.Descripcion != null && p.Descripcion.Contains(q)));
        }
        ViewData["q"] = q;
        return View(await consulta.OrderBy(p => p.Nombre).ToListAsync());
    }

    [Authorize(Roles = Roles.Administrador)]
    [HttpGet]
    public IActionResult Create() => View("Formulario", new Producto());

    [Authorize(Roles = Roles.Administrador)]
    [HttpPost]
    public async Task<IActionResult> Create([Bind("Nombre,Descripcion,Precio,Cantidad,StockMinimo")] Producto producto)
    {
        producto.Nombre = (producto.Nombre ?? string.Empty).Trim();
        if (await _db.Productos.AnyAsync(p => p.Nombre == producto.Nombre))
            ModelState.AddModelError(nameof(Producto.Nombre), "Ya existe un producto con ese nombre.");
        if (!ModelState.IsValid) return View("Formulario", producto);

        _db.Productos.Add(producto);
        await _db.SaveChangesAsync();
        TempData["Exito"] = "Producto registrado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roles.Administrador)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var producto = await _db.Productos.FindAsync(id);
        return producto is null ? NotFound() : View("Formulario", producto);
    }

    [Authorize(Roles = Roles.Administrador)]
    [HttpPost]
    public async Task<IActionResult> Edit([Bind("Id,Nombre,Descripcion,Precio,Cantidad,StockMinimo")] Producto producto)
    {
        producto.Nombre = (producto.Nombre ?? string.Empty).Trim();
        if (await _db.Productos.AnyAsync(p => p.Nombre == producto.Nombre && p.Id != producto.Id))
            ModelState.AddModelError(nameof(Producto.Nombre), "Ya existe otro producto con ese nombre.");
        if (!ModelState.IsValid) return View("Formulario", producto);

        var existente = await _db.Productos.FindAsync(producto.Id);
        if (existente is null) return NotFound();
        existente.Nombre = producto.Nombre;
        existente.Descripcion = producto.Descripcion;
        existente.Precio = producto.Precio;
        existente.Cantidad = producto.Cantidad;
        existente.StockMinimo = producto.StockMinimo;
        await _db.SaveChangesAsync();
        TempData["Exito"] = "Producto actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roles.Administrador)]
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var producto = await _db.Productos.FindAsync(id);
        if (producto is null) return NotFound();

        if (await _db.DetallesVenta.AnyAsync(d => d.ProductoId == id))
        {
            TempData["Error"] = "No se puede eliminar el producto porque tiene ventas asociadas.";
            return RedirectToAction(nameof(Index));
        }

        _db.Productos.Remove(producto);
        await _db.SaveChangesAsync();
        TempData["Exito"] = "Producto eliminado.";
        return RedirectToAction(nameof(Index));
    }
}
