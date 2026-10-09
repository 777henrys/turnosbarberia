namespace BarberiaTurnos.Models;

public class Servicio
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre admite máximo 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "999999.99", ErrorMessage = "El precio debe ser mayor que cero.")]
    public decimal Precio { get; set; }

    [Display(Name = "Duración (minutos)")]
    [Range(1, 600, ErrorMessage = "La duración debe ser mayor que cero (máximo 600 minutos).")]
    public int DuracionMinutos { get; set; } = 30;

    public bool Activo { get; set; } = true;
}
