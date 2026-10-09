namespace BarberiaTurnos.Services;

/// <summary>Resultado simple de una operación de negocio (éxito o mensaje de error para el usuario).</summary>
public record Resultado(bool Exito, string Mensaje, int? Id = null)
{
    public static Resultado Ok(string mensaje, int? id = null) => new(true, mensaje, id);
    public static Resultado Error(string mensaje) => new(false, mensaje);
}
