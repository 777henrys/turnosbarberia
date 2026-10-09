namespace BarberiaTurnos.Models;

public class Cliente
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre admite máximo 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [StringLength(20, ErrorMessage = "El teléfono admite máximo 20 caracteres.")]
    [RegularExpression(@"^[0-9+\-() ]{7,20}$", ErrorMessage = "Teléfono no válido (solo números, espacios, +, - y paréntesis).")]
    public string Telefono { get; set; } = string.Empty;

    [Display(Name = "Fecha de registro")]
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    public ICollection<Turno> Turnos { get; set; } = new List<Turno>();
    public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
}
