using System.Globalization;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BarberiaTurnos.Services;

public class ReporteDatos
{
    public string Titulo { get; set; } = string.Empty;
    public string Subtitulo { get; set; } = string.Empty;
    public string[] Cabeceras { get; set; } = Array.Empty<string>();
    public List<object?[]> Filas { get; set; } = new();
    public List<string> Resumen { get; set; } = new();
}

public interface IReporteService
{
    Task<ReporteDatos> TurnosPorFechaAsync(DateTime fecha);
    Task<ReporteDatos> VentasPorPeriodoAsync(DateTime desde, DateTime hasta);
    Task<ReporteDatos> ProductosAsync();
    byte[] GenerarPdf(ReporteDatos datos);
    byte[] GenerarExcel(ReporteDatos datos);
}

public class ReporteService : IReporteService
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("en-US");
    private readonly ApplicationDbContext _db;

    public ReporteService(ApplicationDbContext db) => _db = db;

    public async Task<ReporteDatos> TurnosPorFechaAsync(DateTime fecha)
    {
        var inicio = fecha.Date;
        var fin = inicio.AddDays(1);

        var turnos = await _db.Turnos.AsNoTracking()
            .Include(t => t.Cliente).Include(t => t.Barbero).Include(t => t.Servicio)
            .Where(t => t.FechaHoraInicio >= inicio && t.FechaHoraInicio < fin)
            .ToListAsync();
        turnos = turnos.OrderBy(t => t.FechaHoraInicio).ToList();

        var datos = new ReporteDatos
        {
            Titulo = "Reporte de turnos",
            Subtitulo = $"Fecha: {inicio:dd/MM/yyyy}",
            Cabeceras = new[] { "Hora", "Cliente", "Barbero", "Servicio", "Duración (min)", "Estado" }
        };
        foreach (var t in turnos)
            datos.Filas.Add(new object?[] { t.FechaHoraInicio.ToString("HH:mm") + " - " + t.FechaHoraFin.ToString("HH:mm"),
                t.Cliente?.Nombre, t.Barbero?.Nombre, t.Servicio?.Nombre, t.DuracionMinutos, t.Estado.ToString() });

        datos.Resumen.Add($"Total de turnos: {turnos.Count}");
        foreach (var g in turnos.GroupBy(t => t.Estado).OrderBy(g => g.Key))
            datos.Resumen.Add($"{g.Key}: {g.Count()}");
        return datos;
    }

    public async Task<ReporteDatos> VentasPorPeriodoAsync(DateTime desde, DateTime hasta)
    {
        var inicio = desde.Date;
        var fin = hasta.Date.AddDays(1);

        var ventas = await _db.Ventas.AsNoTracking()
            .Include(v => v.Cliente).Include(v => v.Usuario)
            .Where(v => v.Fecha >= inicio && v.Fecha < fin)
            .ToListAsync();
        ventas = ventas.OrderBy(v => v.Fecha).ToList();

        var datos = new ReporteDatos
        {
            Titulo = "Reporte de ventas",
            Subtitulo = $"Período: {inicio:dd/MM/yyyy} al {hasta.Date:dd/MM/yyyy}",
            Cabeceras = new[] { "Factura", "Fecha", "Cliente", "Usuario", "Subtotal", "Impuesto", "Total" }
        };
        foreach (var v in ventas)
            datos.Filas.Add(new object?[] { v.Numero, v.Fecha, v.Cliente?.Nombre ?? "Consumidor final",
                NombreUsuario(v.Usuario), v.Subtotal, v.Impuesto, v.Total });

        datos.Resumen.Add($"Cantidad de ventas: {ventas.Count}");
        datos.Resumen.Add($"Subtotal: {ventas.Sum(v => v.Subtotal).ToString("N2", Cultura)}");
        datos.Resumen.Add($"Impuesto: {ventas.Sum(v => v.Impuesto).ToString("N2", Cultura)}");
        datos.Resumen.Add($"Total vendido: {ventas.Sum(v => v.Total).ToString("N2", Cultura)}");
        return datos;
    }

    public async Task<ReporteDatos> ProductosAsync()
    {
        var productos = await _db.Productos.AsNoTracking().OrderBy(p => p.Nombre).ToListAsync();

        var datos = new ReporteDatos
        {
            Titulo = "Reporte de productos y existencias",
            Subtitulo = $"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}",
            Cabeceras = new[] { "Producto", "Descripción", "Precio", "Cantidad", "Stock mínimo", "Situación" }
        };
        foreach (var p in productos)
            datos.Filas.Add(new object?[] { p.Nombre, p.Descripcion, p.Precio, p.Cantidad, p.StockMinimo, p.StockBajo ? "Stock bajo" : "Normal" });

        datos.Resumen.Add($"Total de productos: {productos.Count}");
        datos.Resumen.Add($"Productos con stock bajo: {productos.Count(p => p.StockBajo)}");
        return datos;
    }

    private static string? NombreUsuario(ApplicationUser? u) =>
        u is null ? null : (string.IsNullOrEmpty(u.NombreCompleto) ? u.UserName : u.NombreCompleto);

    private static string Formatear(object? valor) => valor switch
    {
        null => string.Empty,
        decimal d => d.ToString("N2", Cultura),
        DateTime dt => dt.ToString("dd/MM/yyyy HH:mm", Cultura),
        _ => Convert.ToString(valor, Cultura) ?? string.Empty
    };

    public byte[] GenerarPdf(ReporteDatos datos)
    {
        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("Barbería - Sistema de Turnos").FontSize(9).FontColor(Colors.Grey.Darken1);
                    col.Item().Text(datos.Titulo).FontSize(18).SemiBold();
                    col.Item().Text(datos.Subtitulo).FontSize(11).FontColor(Colors.Grey.Darken2);
                });

                page.Content().Column(col =>
                {
                    col.Item().Table(tabla =>
                    {
                        tabla.ColumnsDefinition(c =>
                        {
                            foreach (var _ in datos.Cabeceras) c.RelativeColumn();
                        });

                        tabla.Header(h =>
                        {
                            foreach (var cab in datos.Cabeceras)
                                h.Cell().Background(Colors.Black).Padding(4).Text(cab).FontColor(Colors.White).SemiBold();
                        });

                        foreach (var fila in datos.Filas)
                            foreach (var celda in fila)
                                tabla.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(4).Text(Formatear(celda));
                    });

                    if (datos.Filas.Count == 0)
                        col.Item().PaddingTop(10).Text("No hay registros para los criterios seleccionados.").Italic();

                    col.Item().PaddingTop(12).Column(res =>
                    {
                        foreach (var linea in datos.Resumen)
                            res.Item().Text(linea).SemiBold();
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Página ");
                    t.CurrentPageNumber();
                    t.Span(" de ");
                    t.TotalPages();
                });
            });
        });

        return documento.GeneratePdf();
    }

    public byte[] GenerarExcel(ReporteDatos datos)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Reporte");

        hoja.Cell(1, 1).Value = datos.Titulo;
        hoja.Cell(1, 1).Style.Font.Bold = true;
        hoja.Cell(1, 1).Style.Font.FontSize = 14;
        hoja.Cell(2, 1).Value = datos.Subtitulo;

        const int filaCabecera = 4;
        for (var c = 0; c < datos.Cabeceras.Length; c++)
        {
            var celda = hoja.Cell(filaCabecera, c + 1);
            celda.Value = datos.Cabeceras[c];
            celda.Style.Font.Bold = true;
            celda.Style.Font.FontColor = XLColor.White;
            celda.Style.Fill.BackgroundColor = XLColor.Black;
        }

        var fila = filaCabecera + 1;
        foreach (var datosFila in datos.Filas)
        {
            for (var c = 0; c < datosFila.Length; c++)
            {
                var celda = hoja.Cell(fila, c + 1);
                switch (datosFila[c])
                {
                    case null: break;
                    case decimal d: celda.Value = d; celda.Style.NumberFormat.Format = "#,##0.00"; break;
                    case DateTime dt: celda.Value = dt; celda.Style.DateFormat.Format = "dd/MM/yyyy HH:mm"; break;
                    case int i: celda.Value = i; break;
                    default: celda.Value = Convert.ToString(datosFila[c], Cultura) ?? string.Empty; break;
                }
            }
            fila++;
        }

        fila++;
        foreach (var linea in datos.Resumen)
        {
            hoja.Cell(fila, 1).Value = linea;
            hoja.Cell(fila, 1).Style.Font.Bold = true;
            fila++;
        }

        hoja.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        libro.SaveAs(ms);
        return ms.ToArray();
    }
}
