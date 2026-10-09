namespace BarberiaTurnos.Models;

public class ConfiguracionSistema
{
    public int Id { get; set; }

    [Display(Name = "Porcentaje de impuesto (%)")]
    [Range(typeof(decimal), "0", "100", ErrorMessage = "El porcentaje debe estar entre 0 y 100.")]
    public decimal PorcentajeImpuesto { get; set; }
}
