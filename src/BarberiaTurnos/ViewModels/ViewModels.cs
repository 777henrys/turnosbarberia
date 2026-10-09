namespace BarberiaTurnos.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Ingrese su correo electrónico.")]
    [EmailAddress(ErrorMessage = "Correo electrónico no válido.")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese su contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Mantener sesión iniciada")]
    public bool Recordarme { get; set; }

    public string? ReturnUrl { get; set; }
}

public class DashboardViewModel
{
    public int TotalClientes { get; set; }
    public int BarberosActivos { get; set; }
    public int TurnosPendientesHoy { get; set; }
    public int CantidadVentasHoy { get; set; }
    public decimal VentasHoy { get; set; }
    public List<Producto> ProductosStockBajo { get; set; } = new();
    public List<Turno> TurnosDeHoy { get; set; } = new();
}

public class TurnoFormViewModel
{
    [Required(ErrorMessage = "Seleccione un cliente.")]
    [Display(Name = "Cliente")]
    public int? ClienteId { get; set; }

    [Required(ErrorMessage = "Seleccione un barbero.")]
    [Display(Name = "Barbero")]
    public int? BarberoId { get; set; }

    [Required(ErrorMessage = "Seleccione un servicio.")]
    [Display(Name = "Servicio")]
    public int? ServicioId { get; set; }

    [Required(ErrorMessage = "Indique la fecha y la hora.")]
    [Display(Name = "Fecha y hora")]
    public DateTime? FechaHora { get; set; }

    [StringLength(250, ErrorMessage = "Máximo 250 caracteres.")]
    public string? Notas { get; set; }

    [ValidateNever] public List<SelectListItem> Clientes { get; set; } = new();
    [ValidateNever] public List<SelectListItem> Barberos { get; set; } = new();
    [ValidateNever] public List<SelectListItem> Servicios { get; set; } = new();
}

public class ReprogramarViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Seleccione un barbero.")]
    [Display(Name = "Barbero")]
    public int? BarberoId { get; set; }

    [Required(ErrorMessage = "Indique la nueva fecha y hora.")]
    [Display(Name = "Nueva fecha y hora")]
    public DateTime? FechaHora { get; set; }

    [ValidateNever] public string Resumen { get; set; } = string.Empty;
    [ValidateNever] public List<SelectListItem> Barberos { get; set; } = new();
}

public class LineaVentaViewModel
{
    public TipoItem Tipo { get; set; }
    public int ItemId { get; set; }
    public int Cantidad { get; set; }
}

public class ItemCatalogoViewModel
{
    public string Tipo { get; set; } = string.Empty;
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public int? Stock { get; set; }
}

public class VentaCreateViewModel
{
    public Guid Token { get; set; }
    [Display(Name = "Cliente")]
    public int? ClienteId { get; set; }
    public List<LineaVentaViewModel> Lineas { get; set; } = new();

    [ValidateNever] public List<SelectListItem> Clientes { get; set; } = new();
    [ValidateNever] public List<ItemCatalogoViewModel> Catalogo { get; set; } = new();
    [ValidateNever] public decimal PorcentajeImpuesto { get; set; }
}

public class UsuarioCreateViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100)]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Correo electrónico no válido.")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmarPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Seleccione un rol.")]
    public string Rol { get; set; } = Roles.Empleado;
}

public class UsuarioListaViewModel
{
    public string Id { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public bool Activo { get; set; }
}
