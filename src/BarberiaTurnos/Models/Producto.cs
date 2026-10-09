namespace BarberiaTurnos.Models;

public class Producto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre admite máximo 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "La descripción admite máximo 300 caracteres.")]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Range(typeof(decimal), "0.01", "999999.99", ErrorMessage = "El precio debe ser mayor que cero.")]
    public decimal Precio { get; set; }

    [Display(Name = "Cantidad disponible")]
    [Range(0, 100000, ErrorMessage = "La cantidad no puede ser negativa.")]
    public int Cantidad { get; set; }

    [Display(Name = "Stock mínimo")]
    [Range(0, 100000, ErrorMessage = "El stock mínimo no puede ser negativo.")]
    public int StockMinimo { get; set; }

    [NotMapped]
    public bool StockBajo => Cantidad <= StockMinimo;
}
