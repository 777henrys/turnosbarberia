namespace BarberiaTurnos.Models;

public class Turno
{
    public int Id { get; set; }

    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public int BarberoId { get; set; }
    public Barbero? Barbero { get; set; }

    public int ServicioId { get; set; }
    public Servicio? Servicio { get; set; }

    public DateTime FechaHoraInicio { get; set; }

    /// <summary>Duración copiada del servicio al crear el turno (para no alterar el historial si el servicio cambia).</summary>
    public int DuracionMinutos { get; set; }

    public EstadoTurno Estado { get; set; } = EstadoTurno.Pendiente;

    [StringLength(250)]
    public string? Notas { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    [NotMapped]
    public DateTime FechaHoraFin => FechaHoraInicio.AddMinutes(DuracionMinutos);
}
