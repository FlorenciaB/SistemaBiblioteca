using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaBiblioteca.Data;
using SistemaBiblioteca.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using SistemaBiblioteca.Helpers;

namespace SistemaBiblioteca.Controllers
{
    [Authorize(Roles = "Docente, Admin")]
    public class PrestamosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public PrestamosController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "Docente, Admin")]
        public async Task<IActionResult> Index(string estado)
        {
            var prestamos = _context.Prestamos
                .Include(p => p.MaterialBibliografico)
                .AsQueryable();

            if (estado == "activos")
                prestamos = prestamos.Where(p => p.FechaDevolucion == null);
            else if (estado == "devueltos")
                prestamos = prestamos.Where(p => p.FechaDevolucion != null);

            ViewBag.EstadoSeleccionado = estado;
            ViewBag.IsAdmin = User.IsInRole("Admin");
            return View(await prestamos.ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> Create(int materialId)
        {
            var material = await _context.MaterialesBibliograficos.FindAsync(materialId);
            if (material == null) return NotFound();

            ViewBag.Material = material;
            ViewBag.Grados = new List<string> {
                "Nivel inicial", "1°", "2°", "3°", "4°", "5°", "6°", "7°",
                "Avanzado", "Nivel Primario", "1er ciclo", "2do ciclo",
                "1er y 2do ciclo", "2do ciclo y 7°"
            };

            return View(new Prestamo { MaterialBibliograficoId = materialId });
        }

        [HttpPost]
        public async Task<IActionResult> Create(Prestamo prestamo)
        {
            if (prestamo.MaterialBibliograficoId == 0)
            {
                ModelState.AddModelError("", "Libro no encontrado o no seleccionado.");
                return View(prestamo);
            }

            var libro = await _context.MaterialesBibliograficos.FindAsync(prestamo.MaterialBibliograficoId);
            if (libro == null)
            {
                ModelState.AddModelError("", "Libro no encontrado o no seleccionado.");
                return View(prestamo);
            }

            if (libro.Cantidad < 1)
            {
                ModelState.AddModelError("", "No hay ejemplares disponibles para prestar.");
                ViewBag.Material = libro;
                ViewBag.Grados = new List<string> {
            "Nivel inicial", "1°", "2°", "3°", "4°", "5°", "6°", "7°",
            "Avanzado", "Nivel Primario", "1er ciclo", "2do ciclo",
            "1er y 2do ciclo", "2do ciclo y 7°"
        };
                return View(prestamo);
            }

            // =================================================
            // 🌸 NORMALIZACIÓN DE LOS CAMPOS DEL FORMULARIO 🌸
            // =================================================
            prestamo.NombreNino = NormalizeToTitleCase(prestamo.NombreNino);
            prestamo.ApellidoNino = NormalizeToTitleCase(prestamo.ApellidoNino);

            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
            {
                ModelState.AddModelError("", "Usuario no autenticado.");
                return View(prestamo);
            }

            // ✅ Restar cantidad y actualizar estado automáticamente
            libro.Cantidad -= 1;
            if (libro.Cantidad <= 0)
            {
                libro.Estado = "Prestado";
            }
            else
            {
                libro.Estado = "Disponible";
            }

            _context.Update(libro);
            await _context.SaveChangesAsync();

            // Registrar préstamo
            prestamo.UsuarioId = usuario.Id;
            prestamo.FechaPrestamo = DateTime.Now;
            prestamo.FechaLimiteDevolucion = DateTime.Now.AddDays(15);

            // Generar comprobante PDF
            var pdfBytes = PdfHelper.GenerarComprobantePrestamoPdf(prestamo);

            string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "comprobantes");

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string filePath = Path.Combine(folderPath, $"Comprobante_Prestamo_{prestamo.Id}.pdf");
            System.IO.File.WriteAllBytes(filePath, pdfBytes);

            _context.Prestamos.Add(prestamo);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Préstamo registrado exitosamente.";
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ActivosOrdenados()
        {
            var prestamos = await _context.Prestamos
                .Include(p => p.MaterialBibliografico)
                .Where(p => p.FechaDevolucion == null)
                .OrderBy(p => p.FechaLimiteDevolucion)
                .ToListAsync();

            return View(prestamos);
        }

        [HttpPost]
        public async Task<IActionResult> Devolver(int id)
        {
            var prestamo = await _context.Prestamos
                .Include(p => p.MaterialBibliografico)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (prestamo == null)
                return NotFound();

            if (prestamo.FechaDevolucion != null)
                return BadRequest("Este libro ya fue devuelto.");

            prestamo.FechaDevolucion = DateTime.Now;

            prestamo.MaterialBibliografico.Cantidad += 1;

            if (prestamo.MaterialBibliografico.Cantidad <= 0)
            {
                prestamo.MaterialBibliografico.Estado = "Prestado";
            }
            else
            {
                prestamo.MaterialBibliografico.Estado = "Disponible";
            }

            _context.Update(prestamo);
            _context.Update(prestamo.MaterialBibliografico);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Libro marcado como devuelto correctamente.";
            return RedirectToAction("Index");
        }
        //METODO AUXILIAR
        private string? NormalizeToTitleCase(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            input = input.Trim().ToLower();

            // Convierte a Title Case según cultura
            var textInfo = System.Globalization.CultureInfo.CurrentCulture.TextInfo;
            return textInfo.ToTitleCase(input);
        }
    }
}
