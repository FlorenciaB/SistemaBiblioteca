using Azure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using SistemaBiblioteca.Data;
using SistemaBiblioteca.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TuProyecto.Models;

namespace SistemaBiblioteca.Controllers
{
    [Authorize]
    public class CuentaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        public CuentaController(ApplicationDbContext context, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [AllowAnonymous]
        public IActionResult OlvidePassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> OlvidePassword(OlvidePasswordViewModel modelo)
        {
            if (!ModelState.IsValid)
                return View(modelo);

            var usuario = await _userManager.FindByEmailAsync(modelo.Email);
            if (usuario == null)
            {
                TempData["ResetError"] = "Si el correo existe, se generará un enlace de recuperación.";
                return RedirectToAction("OlvidePassword");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);

            // Creamos el link (se mostrará en pantalla)
            var resetLink = Url.Action("ResetPassword", "Cuenta",
                new { email = modelo.Email, token = token }, Request.Scheme);

            TempData["ResetLink"] = resetLink;
            return RedirectToAction("MostrarEnlaceReset");
        }

        [AllowAnonymous]
        public IActionResult MostrarEnlaceReset()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
        {
            var model = new ResetPasswordViewModel { Email = email, Token = token };
            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel modelo)
        {
            if (!ModelState.IsValid)
                return View(modelo);

            var usuario = await _userManager.FindByEmailAsync(modelo.Email);
            if (usuario == null)
            {
                TempData["Error"] = "No se encontró un usuario con ese correo.";
                return RedirectToAction("OlvidePassword");
            }

            var resultado = await _userManager.ResetPasswordAsync(usuario, modelo.Token, modelo.Password);

            if (resultado.Succeeded)
            {
                // Logueamos automáticamente al usuario
                await _signInManager.SignInAsync(usuario, isPersistent: false);

                // Redirigimos al inicio de los libros
                return RedirectToAction("Index", "MaterialBibliografico");
            }

            // Traducimos manualmente los mensajes de Identity al español
            foreach (var error in resultado.Errors)
            {
                string mensaje = error.Description;

                if (mensaje.Contains("Passwords must be at least"))
                    mensaje = "La contraseña debe tener al menos 6 caracteres.";
                else if (mensaje.Contains("uppercase"))
                    mensaje = "La contraseña debe tener al menos una letra mayúscula.";
                else if (mensaje.Contains("digit"))
                    mensaje = "La contraseña debe tener al menos un número.";
                else if (mensaje.Contains("non alphanumeric"))
                    mensaje = "La contraseña debe tener al menos un símbolo.";
                else if (mensaje.Contains("Incorrect password"))
                    mensaje = "La contraseña es incorrecta.";

                ModelState.AddModelError("", mensaje);
            }

            return View(modelo);
        }
        public async Task<IActionResult> MiPerfil()
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return NotFound();

            var modelo = new EditarPerfilViewModel
            {
                Email = usuario.Email,
            };

            return View(modelo);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> MiPerfil(EditarPerfilViewModel modelo)
        {
            if (!ModelState.IsValid) return View(modelo);

            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return NotFound();

            usuario.Email = modelo.Email;
            usuario.NormalizedEmail = modelo.Email.ToUpper();

            var resultado = await _userManager.UpdateAsync(usuario);
            if (resultado.Succeeded)
            {
                TempData["Success"] = "Perfil actualizado correctamente.";
                return RedirectToAction("MiPerfil");
            }

            foreach (var error in resultado.Errors)
                ModelState.AddModelError("", error.Description);

            return View(modelo);
        }

        public IActionResult CambiarPassword() => View();

        [HttpPost]
        public async Task<IActionResult> CambiarPassword(CambiarPasswordViewModel modelo)
        {
            if (!ModelState.IsValid) return View(modelo);

            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return NotFound();

            var resultado = await _userManager.ChangePasswordAsync(usuario, modelo.Password, modelo.NuevaPassword);

            if (resultado.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(usuario);
                TempData["Success"] = "Contraseña actualizada correctamente.";
                return RedirectToAction("MiPerfil");
            }

            // Traducimos manualmente los mensajes de Identity al español
            foreach (var error in resultado.Errors)
            {
                string mensaje = error.Description;

                if (mensaje.Contains("Passwords must be at least"))
                    mensaje = "La contraseña debe tener al menos 6 caracteres.";
                else if (mensaje.Contains("uppercase"))
                    mensaje = "La contraseña debe tener al menos una letra mayúscula.";
                else if (mensaje.Contains("digit"))
                    mensaje = "La contraseña debe tener al menos un número.";
                else if (mensaje.Contains("non alphanumeric"))
                    mensaje = "La contraseña debe tener al menos un símbolo.";
                else if (mensaje.Contains("Incorrect password"))
                    mensaje = "La contraseña es incorrecta.";

                ModelState.AddModelError("", mensaje);
            }

            return View(modelo);
        }

        public async Task<IActionResult> Resumen()
        {
            var usuario = await _userManager.GetUserAsync(User);
            if(usuario == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(usuario);

            var modelo = new MiCuentaResumenViewModel
            {
                Email = usuario.Email,
                UserName = usuario.UserName,
                Roles = roles.ToList()
            };

            modelo.LibrosPrestados = _context.Prestamos
                .Include(p => p.MaterialBibliografico)
                .Where(p => p.UsuarioId == usuario.Id && p.FechaDevolucion == null)
                .Select(p => p.MaterialBibliografico.Titulo)
                .ToList();

            return View(modelo);
        }
    }
}