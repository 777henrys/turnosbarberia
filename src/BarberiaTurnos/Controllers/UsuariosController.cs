using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace BarberiaTurnos.Controllers;

[Authorize(Roles = Roles.Administrador)]
public class UsuariosController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsuariosController(UserManager<ApplicationUser> userManager) => _userManager = userManager;

    public async Task<IActionResult> Index()
    {
        var usuarios = await _userManager.Users.OrderBy(u => u.NombreCompleto).ToListAsync();
        var lista = new List<UsuarioListaViewModel>();
        foreach (var u in usuarios)
        {
            var roles = await _userManager.GetRolesAsync(u);
            lista.Add(new UsuarioListaViewModel
            {
                Id = u.Id,
                NombreCompleto = u.NombreCompleto,
                Email = u.Email ?? u.UserName ?? string.Empty,
                Rol = string.Join(", ", roles),
                Activo = !(u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow.AddYears(50))
            });
        }
        return View(lista);
    }

    [HttpGet]
    public IActionResult Create() => View(new UsuarioCreateViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(UsuarioCreateViewModel vm)
    {
        if (!Roles.Todos.Contains(vm.Rol))
            ModelState.AddModelError(nameof(vm.Rol), "Rol no válido.");
        if (!ModelState.IsValid) return View(vm);

        var usuario = new ApplicationUser
        {
            UserName = vm.Email.Trim(),
            Email = vm.Email.Trim(),
            EmailConfirmed = true,
            NombreCompleto = vm.NombreCompleto.Trim()
        };

        var resultado = await _userManager.CreateAsync(usuario, vm.Password);
        if (!resultado.Succeeded)
        {
            foreach (var error in resultado.Errors) ModelState.AddModelError(string.Empty, TraducirError(error));
            return View(vm);
        }

        await _userManager.AddToRoleAsync(usuario, vm.Rol);
        TempData["Exito"] = "Usuario creado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Activa o desactiva una cuenta (bloqueo indefinido). No se puede desactivar la cuenta propia.</summary>
    [HttpPost]
    public async Task<IActionResult> CambiarEstado(string id)
    {
        if (id == User.FindFirstValue(ClaimTypes.NameIdentifier))
        {
            TempData["Error"] = "No puede desactivar su propia cuenta.";
            return RedirectToAction(nameof(Index));
        }

        var usuario = await _userManager.FindByIdAsync(id);
        if (usuario is null) return NotFound();

        var desactivado = usuario.LockoutEnd.HasValue && usuario.LockoutEnd > DateTimeOffset.UtcNow.AddYears(50);
        await _userManager.SetLockoutEnabledAsync(usuario, true);
        await _userManager.SetLockoutEndDateAsync(usuario, desactivado ? null : DateTimeOffset.MaxValue);
        TempData["Exito"] = desactivado ? "Usuario activado." : "Usuario desactivado.";
        return RedirectToAction(nameof(Index));
    }

    private static string TraducirError(IdentityError e) => e.Code switch
    {
        "DuplicateUserName" or "DuplicateEmail" => "Ya existe un usuario con ese correo.",
        "PasswordTooShort" => "La contraseña es demasiado corta.",
        "PasswordRequiresDigit" => "La contraseña debe incluir al menos un número.",
        "PasswordRequiresLower" => "La contraseña debe incluir al menos una minúscula.",
        "PasswordRequiresUpper" => "La contraseña debe incluir al menos una mayúscula.",
        _ => e.Description
    };
}
