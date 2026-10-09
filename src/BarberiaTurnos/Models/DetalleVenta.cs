namespace BarberiaTurnos.Models;

public class DetalleVenta
{
    public int Id { get; set; }

    public int VentaId { get; set; }
    public Venta? Venta { get; set; }

    public TipoItem Tipo { get; set; }

    public int? ServicioId { get; set; }
    public Servicio? Servicio { get; set; }

    public int? ProductoId { get; set; }
    public Producto? Producto { get; set; }

    [StringLength(150)]
    public string Descripcion { get; set; } = string.Empty;

    public int Cantidad { get; set; }

    /// <summary>Precio aplicado en el momento de la venta.</summary>
    public decimal PrecioUnitario { get; set; }

    public decimal Subtotal { get; set; }
}
