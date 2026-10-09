using System.Security.Claims;

namespace BarberiaTurnos.Controllers;

public class VentasController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IVentaService _ventas;

    public VentasController(ApplicationDbContext db, IVentaService ventas)
    {
        _db = db;
        _ventas = ventas;
    }

    public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta)
    {
        var d = (desde ?? DateTime.Today.AddDays(-30)).Date;
        var h = (hasta ?? DateTime.Today).Date;
        var fin = h.AddDays(1);

        var ventas = await _db.Ventas.AsNoTracking()
            .Include(v => v.Cliente).Include(v => v.Usuario)
            .Where(v => v.Fecha >= d && v.Fecha < fin)
            .ToListAsync();

        ViewData["Desde"] = d;
        ViewData["Hasta"] = h;
        return View(ventas.OrderByDescending(v => v.Fecha).ToList());
    }

    public async Task<IActionResult> Details(int id)
    {
        var venta = await _db.Ventas.AsNoTracking()
            .Include(v => v.Cliente).Include(v => v.Usuario).Include(v => v.Detalles)
            .FirstOrDefaultAsync(v => v.Id == id);
        return venta is null ? NotFound() : View(venta);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = new VentaCreateViewModel { Token = Guid.NewGuid() };
        await CargarCatalogoAsync(vm);
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Create(VentaCreateViewModel vm)
    {
        if (ModelState.IsValid)
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var input = new VentaInput(vm.Token, vm.ClienteId, vm.Lineas.Select(l => new LineaVentaInput(l.Tipo, l.ItemId, l.Cantidad)).ToList());
            var resultado = await _ventas.RegistrarAsync(input, usuarioId);
            if (resultado.Exito)
            {
                TempData["Exito"] = resultado.Mensaje;
                return RedirectToAction(nameof(Details), new { id = resultado.Id });
            }
            ModelState.AddModelError(string.Empty, resultado.Mensaje);
        }

        await CargarCatalogoAsync(vm);
        return View(vm);
    }

    private async Task CargarCatalogoAsync(VentaCreateViewModel vm)
    {
        vm.Clientes = await _db.Clientes.AsNoTracking().OrderBy(c => c.Nombre)
            .Select(c => new SelectListItem(c.Nombre + " - " + c.Telefono, c.Id.ToString())).ToListAsync();

        var servicios = await _db.Servicios.AsNoTracking().Where(s => s.Activo).OrderBy(s => s.Nombre).ToListAsync();
        var productos = await _db.Productos.AsNoTracking().Where(p => p.Cantidad > 0).OrderBy(p => p.Nombre).ToListAsync();

        vm.Catalogo = servicios.Select(s => new ItemCatalogoViewModel { Tipo = nameof(TipoItem.Servicio), Id = s.Id, Nombre = s.Nombre, Precio = s.Precio })
            .Concat(productos.Select(p => new ItemCatalogoViewModel { Tipo = nameof(TipoItem.Producto), Id = p.Id, Nombre = p.Nombre, Precio = p.Precio, Stock = p.Cantidad }))
            .ToList();

        vm.PorcentajeImpuesto = (await _db.Configuraciones.AsNoTracking().FirstOrDefaultAsync())?.PorcentajeImpuesto ?? 0m;
    }
}
