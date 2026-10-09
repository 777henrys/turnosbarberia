using Microsoft.AspNetCore.Identity;

namespace BarberiaTurnos.Models;

public class ApplicationUser : IdentityUser
{
    [MaxLength(100)]
    public string NombreCompleto { get; set; } = string.Empty;
}
