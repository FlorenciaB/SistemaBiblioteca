using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SistemaBiblioteca.Models;
using System.Data;

namespace SistemaBiblioteca.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Usuarios()
        {
            var usuarios = _userManager.Users.ToList();
            var viewModel = new List<UserRolViewModel>();

            foreach (var usuario in usuarios)
            {
                var roles = await _userManager.GetRolesAsync(usuario);
                viewModel.Add(new UserRolViewModel
                {
                    UserId = usuario.Id,
                    Email = usuario.Email,
                    Roles = roles.ToList()
                });
            }

            return View(viewModel);
        }

        public IActionResult CrearUsuario()
        {
            ViewBag.Roles = new List<string> { "Docente", "Alumno" };
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearUsuario(CrearUsuarioViewModel model)
        {
            // Roles disponibles
            ViewBag.Roles = new List<string> { "Docente", "Alumno" };

            if (!ModelState.IsValid)
            {
                // Si el formulario tiene errores de validación, volver a mostrar la vista
                return View(model);
            }

            // Crear usuario
            var nuevoUsuario = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true
            };

            // Crear el usuario en Identity
            var result = await _userManager.CreateAsync(nuevoUsuario, model.Password);

            if (!result.Succeeded)
            {
                // Traducir los errores de Identity al español
                foreach (var error in result.Errors)
                {
                    switch (error.Code)
                    {
                        case "DuplicateUserName":
                            ModelState.AddModelError(string.Empty, "El email ya está registrado.");
                            break;
                        case "PasswordTooShort":
                            ModelState.AddModelError(string.Empty, "La contraseña es demasiado corta.");
                            break;
                        case "PasswordRequiresNonAlphanumeric":
                            ModelState.AddModelError(string.Empty, "La contraseña debe contener al menos un carácter especial.");
                            break;
                        case "PasswordRequiresDigit":
                            ModelState.AddModelError(string.Empty, "La contraseña debe contener al menos un número.");
                            break;
                        case "PasswordRequiresUpper":
                            ModelState.AddModelError(string.Empty, "La contraseña debe contener al menos una letra mayúscula.");
                            break;
                        case "PasswordRequiresLower":
                            ModelState.AddModelError(string.Empty, "La contraseña debe contener al menos una letra minúscula.");
                            break;
                        default:
                            ModelState.AddModelError(string.Empty, error.Description); // usar el mensaje original si no hay traducción
                            break;
                    }
                }
                return View(model);
            }

            // Crear el rol si no existe
            if (!await _roleManager.RoleExistsAsync(model.Rol))
            {
                await _roleManager.CreateAsync(new IdentityRole(model.Rol));
            }

            // Asignar rol
            await _userManager.AddToRoleAsync(nuevoUsuario, model.Rol);

            TempData["Success"] = $"Usuario {model.Email} creado con rol {model.Rol}.";
            return RedirectToAction("Usuarios");
        }


        [HttpPost]
        public async Task<IActionResult> AsignarRol(string userId, string rol)
        {
            var usuario = await _userManager.FindByIdAsync(userId);

            if (usuario != null && await _roleManager.RoleExistsAsync(rol))
            {
                await _userManager.AddToRoleAsync(usuario, rol);
            }

            return RedirectToAction("Usuarios");
        }

        [HttpPost]
        public async Task<IActionResult> QuitarRol(string userId, string rol)
        {
            var usuario = await _userManager.FindByIdAsync(userId);

            if (usuario != null && await _userManager.IsInRoleAsync(usuario, rol))
            {
                await _userManager.RemoveFromRoleAsync(usuario, rol);
            }

            return RedirectToAction("Usuarios");
        }

        public async Task<IActionResult> EditarUsuario(string Id)
        {
            var user = await _userManager.FindByIdAsync(Id);
            if (user == null) return NotFound();

            if (user.Email == "admin@biblioteca.com")
            {
                TempData["Error"] = "No se puede editar el usuario Admin principal.";
                return RedirectToAction("Usuarios");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var viewModel = new UserRolViewModel
            {
                UserId = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                Roles = roles.ToList(),
                EstaBloqueado = user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditarUsuario(UserRolViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if(user == null) return NotFound();

            user.Email = model.Email;
            user.NormalizedEmail = user.Email.ToUpper();
            user.UserName = user.UserName;
            user.NormalizedUserName = user.UserName.ToUpper();

            if (user.Email == "admin@biblioteca.com")
            {
                TempData["Error"] = "No se puede editar el usuario Admin principal.";
                return RedirectToAction("Usuarios");
            }

            var result = await _userManager.UpdateAsync(user);
            if(result.Succeeded)
            {
                return RedirectToAction("Usuarios");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Bloquear(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if(user == null) return NotFound();

            if(user.Email == User.Identity.Name)
            {
                TempData["Error"] = "No podés bloquearte a vos misma 😉";
                return RedirectToAction("Usuarios");
            }

            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            return RedirectToAction("Usuarios");
        }

        [HttpPost]
        public async Task<IActionResult> Desbloquear(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            await _userManager.SetLockoutEndDateAsync(user, null);
            return RedirectToAction("Usuarios");
        }
    }
}
