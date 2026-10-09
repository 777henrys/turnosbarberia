using System.Data;

namespace BarberiaTurnos.Services;

public record LineaVentaInput(TipoItem Tipo, int ItemId, int Cantidad);

public record VentaInput(Guid Token, int? ClienteId, IReadOnlyList<LineaVentaInput> Lineas);

public static class CalculoFactura
{
    /// <summary>Calcula impuesto y total a partir del subtotal y un porcentaje configurable.</summary>
    public static (decimal Impuesto, decimal Total) Calcular(decimal subtotal, decimal porcentajeImpuesto)
    {
        var impuesto = Math.Round(subtotal * porcentajeImpuesto / 100m, 2, MidpointRounding.AwayFromZero);
        return (impuesto, subtotal + impuesto);
    }
}

public interface IVentaService
{
    Task<Resultado> RegistrarAsync(VentaInput input, string usuarioId);
}

public class VentaService : IVentaService
{
    private readonly ApplicationDbContext _db;

    public VentaService(ApplicationDbContext db) => _db = db;

    public async Task<Resultado> RegistrarAsync(VentaInput input, string usuarioId)
    {
        if (input.Token == Guid.Empty) return Resultado.Error("Operación no válida. Recargue la página e intente de nuevo.");
        if (string.IsNullOrWhiteSpace(usuarioId)) return Resultado.Error("No se pudo identificar al usuario responsable.");
        if (input.Lineas is null || input.Lineas.Count == 0) return Resultado.Error("Agregue al menos un artículo a la venta.");
        if (input.Lineas.Any(l => l.Cantidad < 1 || l.Cantidad > 1000)) return Resultado.Error("Las cantidades deben estar entre 1 y 1000.");

        // Evita duplicados: si el token ya se registró, devolvemos esa venta.
        var existente = await _db.Ventas.AsNoTracking().FirstOrDefaultAsync(v => v.TokenOperacion == input.Token);
        if (existente is not null) return Resultado.Ok("Esta venta ya había sido registrada.", existente.Id);

        if (input.ClienteId is not null && !await _db.Clientes.AnyAsync(c => c.Id == input.ClienteId))
            return Resultado.Error("El cliente seleccionado no existe.");

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var servicioIds = input.Lineas.Where(l => l.Tipo == TipoItem.Servicio).Select(l => l.ItemId).Distinct().ToList();
        var productoIds = input.Lineas.Where(l => l.Tipo == TipoItem.Producto).Select(l => l.ItemId).Distinct().ToList();

        var servicios = await _db.Servicios.Where(s => servicioIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id);
        var productos = await _db.Productos.Where(p => productoIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

        // Validar stock acumulado por producto (una misma línea puede repetirse).
        var pedidoPorProducto = input.Lineas.Where(l => l.Tipo == TipoItem.Producto)
            .GroupBy(l => l.ItemId).ToDictionary(g => g.Key, g => g.Sum(l => l.Cantidad));

        foreach (var (id, cantidad) in pedidoPorProducto)
        {
            if (!productos.TryGetValue(id, out var p)) return Resultado.Error("Uno de los productos seleccionados no existe.");
            if (cantidad > p.Cantidad)
                return Resultado.Error($"Stock insuficiente para \"{p.Nombre}\": disponible {p.Cantidad}, solicitado {cantidad}.");
        }

        var venta = new Venta
        {
            Fecha = DateTime.Now,
            UsuarioId = usuarioId,
            ClienteId = input.ClienteId,
            TokenOperacion = input.Token
        };

        foreach (var l in input.Lineas)
        {
            if (l.Tipo == TipoItem.Servicio)
            {
                if (!servicios.TryGetValue(l.ItemId, out var s)) return Resultado.Error("Uno de los servicios seleccionados no existe.");
                if (!s.Activo) return Resultado.Error($"El servicio \"{s.Nombre}\" está inactivo.");
                venta.Detalles.Add(new DetalleVenta
                {
                    Tipo = TipoItem.Servicio,
                    ServicioId = s.Id,
                    Descripcion = s.Nombre,
                    Cantidad = l.Cantidad,
                    PrecioUnitario = s.Precio,
                    Subtotal = s.Precio * l.Cantidad
                });
            }
            else
            {
                var p = productos[l.ItemId];
                venta.Detalles.Add(new DetalleVenta
                {
                    Tipo = TipoItem.Producto,
                    ProductoId = p.Id,
                    Descripcion = p.Nombre,
                    Cantidad = l.Cantidad,
                    PrecioUnitario = p.Precio,
                    Subtotal = p.Precio * l.Cantidad
                });
            }
        }

        foreach (var (id, cantidad) in pedidoPorProducto)
            productos[id].Cantidad -= cantidad;

        var porcentaje = (await _db.Configuraciones.AsNoTracking().FirstOrDefaultAsync())?.PorcentajeImpuesto ?? 0m;
        venta.Subtotal = venta.Detalles.Sum(d => d.Subtotal);
        venta.PorcentajeImpuesto = porcentaje;
        (venta.Impuesto, venta.Total) = CalculoFactura.Calcular(venta.Subtotal, porcentaje);

        _db.Ventas.Add(venta);

        try
        {
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (DbUpdateException)
        {
            // Posible doble envío simultáneo con el mismo token (índice único).
            await tx.RollbackAsync();
            _db.ChangeTracker.Clear();
            var duplicada = await _db.Ventas.AsNoTracking().FirstOrDefaultAsync(v => v.TokenOperacion == input.Token);
            if (duplicada is not null) return Resultado.Ok("Esta venta ya había sido registrada.", duplicada.Id);
            throw;
        }

        return Resultado.Ok("Venta registrada correctamente.", venta.Id);
    }
}
