namespace BarberiaTurnos.Models;

public class Venta
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;

    public string UsuarioId { get; set; } = string.Empty;
    public ApplicationUser? Usuario { get; set; }

    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public decimal Subtotal { get; set; }
    public decimal PorcentajeImpuesto { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Total { get; set; }

    /// <summary>Identificador de operación para evitar registrar la misma venta dos veces.</summary>
    public Guid TokenOperacion { get; set; }

    public ICollection<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();

    [NotMapped]
    public string Numero => $"F-{Id:D6}";
}
