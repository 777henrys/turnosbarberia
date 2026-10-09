namespace BarberiaTurnos.Controllers;

[Authorize(Roles = Roles.Administrador)]
public class ConfiguracionController : Controller
{
    private readonly ApplicationDbContext _db;

    public ConfiguracionController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cfg = await _db.Configuraciones.AsNoTracking().FirstOrDefaultAsync() ?? new ConfiguracionSistema();
        return View(cfg);
    }

    [HttpPost]
    public async Task<IActionResult> Index([Bind("PorcentajeImpuesto")] ConfiguracionSistema modelo)
    {
        if (!ModelState.IsValid) return View(modelo);

        var cfg = await _db.Configuraciones.FirstOrDefaultAsync();
        if (cfg is null)
        {
            cfg = new ConfiguracionSistema();
            _db.Configuraciones.Add(cfg);
        }
        cfg.PorcentajeImpuesto = modelo.PorcentajeImpuesto;
        await _db.SaveChangesAsync();
        TempData["Exito"] = "Configuración guardada. Se aplicará a las nuevas ventas.";
        return RedirectToAction(nameof(Index));
    }
}
