namespace BarberiaTurnos.Models;

public enum EstadoTurno
{
    Pendiente = 1,
    Confirmado = 2,
    Completado = 3,
    Cancelado = 4
}

public enum TipoItem
{
    Servicio = 1,
    Producto = 2
}

public static class Roles
{
    public const string Administrador = "Administrador";
    public const string Empleado = "Empleado";
    public static readonly string[] Todos = { Administrador, Empleado };
}
