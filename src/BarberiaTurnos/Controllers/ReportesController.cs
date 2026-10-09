namespace BarberiaTurnos.Controllers;

public class ReportesController : Controller
{
    private const string TipoPdf = "application/pdf";
    private const string TipoExcel = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IReporteService _reportes;

    public ReportesController(IReporteService reportes) => _reportes = reportes;

    public IActionResult Index() => View();

    /// <param name="reporte">turnos | ventas | productos</param>
    /// <param name="formato">pdf | excel</param>
    [HttpGet]
    public async Task<IActionResult> Generar(string reporte, string formato, DateTime? fecha, DateTime? desde, DateTime? hasta)
    {
        ReporteDatos datos;
        switch (reporte?.ToLowerInvariant())
        {
            case "turnos":
                datos = await _reportes.TurnosPorFechaAsync(fecha ?? DateTime.Today);
                break;
            case "ventas":
                var d = (desde ?? DateTime.Today.AddDays(-30)).Date;
                var h = (hasta ?? DateTime.Today).Date;
                if (h < d)
                {
                    TempData["Error"] = "La fecha final no puede ser anterior a la inicial.";
                    return RedirectToAction(nameof(Index));
                }
                datos = await _reportes.VentasPorPeriodoAsync(d, h);
                break;
            case "productos":
                datos = await _reportes.ProductosAsync();
                break;
            default:
                return BadRequest("Reporte no válido.");
        }

        var nombre = $"{reporte}_{DateTime.Now:yyyyMMdd_HHmm}";
        return formato?.ToLowerInvariant() switch
        {
            "pdf" => File(_reportes.GenerarPdf(datos), TipoPdf, nombre + ".pdf"),
            "excel" => File(_reportes.GenerarExcel(datos), TipoExcel, nombre + ".xlsx"),
            _ => BadRequest("Formato no válido.")
        };
    }
}
