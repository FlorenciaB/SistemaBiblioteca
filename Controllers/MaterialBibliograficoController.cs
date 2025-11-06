using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaBiblioteca.Data;
using SistemaBiblioteca.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaBiblioteca.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MaterialBibliograficoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MaterialBibliograficoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===================== INDEX =====================
        // GET: MaterialBibliografico
        public async Task<IActionResult> Index(string materia, string estado)
        {
            var prestamosVencidos = await _context.Prestamos
                .Where(p => p.FechaDevolucion == null && p.FechaLimiteDevolucion < DateTime.Now)
                .ToListAsync();

            ViewBag.HayPrestamosVencidos = prestamosVencidos.Any();

            var librosQuery = _context.MaterialesBibliograficos.AsQueryable();

            if (!string.IsNullOrEmpty(materia))
                librosQuery = librosQuery.Where(l => l.Materias.Contains(materia));

            if (!string.IsNullOrEmpty(estado))
            {
                if (estado == "Disponible")
                    librosQuery = librosQuery.Where(l => l.Estado == "Disponible");
                else if (estado == "Prestado")
                    librosQuery = librosQuery.Where(l => l.Estado == "Prestado");
            }

            var listaLibros = await librosQuery.ToListAsync();

            ViewBag.MateriaSeleccionada = materia;
            ViewBag.EstadoSeleccionado = estado;

            // 🔹 Corregido: ahora se muestran materias individuales sin duplicados
            ViewBag.Materias = _context.MaterialesBibliograficos
                .AsEnumerable()
                .SelectMany(m => m.Materias) // antes Select(m => m.Materias)
                .Distinct()
                .OrderBy(m => m)
                .ToList();

            return View(listaLibros);
        }

        // ===================== DETALLES =====================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var material = await _context.MaterialesBibliograficos
                .FirstOrDefaultAsync(m => m.Id == id);
            if (material == null) return NotFound();

            return View(material);
        }

        // ===================== CREAR =====================
        // GET: Crear
        public IActionResult Create()
        {
            var material = new MaterialBibliografico
            {
                FechaAlta = DateTime.Today
            };

            ViewBag.Grados = ObtenerGrados();
            ViewBag.Aulas = ObtenerAulas();

            return View(material);
        }

        // POST: Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MaterialBibliografico material)
        {
            ViewBag.Grados = ObtenerGrados();
            ViewBag.Aulas = ObtenerAulas();

            material.Estado = material.Cantidad > 0 ? "Disponible" : "Prestado";

            if (material.Grados == null || !material.Grados.Any())
                ModelState.AddModelError("Grados", "Debés seleccionar al menos un grado.");

            material.Grado = material.Grados != null ? string.Join(", ", material.Grados) : null;
            material.LibroAula = material.Aulas != null ? string.Join(", ", material.Aulas) : null;

            // 🔹 Normalización de materias antes de guardar
            if (material.Materias != null && material.Materias.Any())
            {
                material.Materias = material.Materias
                    .Select(m => ToCamelCase(m ?? string.Empty))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct()
                    .ToList();
            }
            else
            {
                material.Materias = material.Materias ?? new List<string>();
            }

            if (ModelState.IsValid)
            {
                if (material.FechaAlta == DateTime.Today)
                    material.FechaAlta = DateTime.Now;

                _context.Add(material);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(material);
        }

        // ===================== EDITAR =====================
        // GET: Editar
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var material = await _context.MaterialesBibliograficos.FindAsync(id);
            if (material == null) return NotFound();

            if (!string.IsNullOrEmpty(material.Grado))
                material.Grados = material.Grado.Split(", ").ToList();

            if (!string.IsNullOrEmpty(material.LibroAula))
                material.Aulas = material.LibroAula.Split(", ").ToList();

            ViewBag.Grados = ObtenerGrados();
            ViewBag.Aulas = ObtenerAulas();

            return View(material);
        }

        // POST: Editar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MaterialBibliografico material)
        {
            if (id != material.Id) return NotFound();

            ViewBag.Grados = ObtenerGrados();
            ViewBag.Aulas = ObtenerAulas();

            if (string.IsNullOrEmpty(material.Ubicacion))
                ModelState.AddModelError("Ubicacion", "Debés seleccionar una ubicación.");

            if (material.Ubicacion == "Aula" && (material.Aulas == null || !material.Aulas.Any()))
                ModelState.AddModelError("Aulas", "Debés seleccionar al menos un aula.");

            if (material.Grados == null || !material.Grados.Any())
                ModelState.AddModelError("Grados", "Debés seleccionar el grado.");

            // 🔹 Normalización de materias
            if (material.Materias != null && material.Materias.Any())
            {
                material.Materias = material.Materias
                    .Select(m => ToCamelCase(m ?? string.Empty))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct()
                    .ToList();
            }
            else
            {
                material.Materias = material.Materias ?? new List<string>();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var materialDb = await _context.MaterialesBibliograficos.FindAsync(id);
                    if (materialDb == null) return NotFound();

                    ActualizarCampos(materialDb, material);

                    materialDb.Procedencia = NormalizarProcedencia(material.Procedencia);
                    materialDb.Titulo = ToCamelCase(material.Titulo);
                    materialDb.Autor = ToCamelCase(material.Autor);
                    materialDb.Editorial = ToCamelCase(material.Editorial);
                    materialDb.Materias = material.Materias;
                    materialDb.SubmateriaLengua = ToCamelCase(material.SubmateriaLengua);
                    materialDb.TipoSoporte = ToCamelCase(material.TipoSoporte);
                    materialDb.SubtipoSoporteLibro = ToCamelCase(material.SubtipoSoporteLibro);
                    materialDb.Estado = materialDb.Cantidad > 0 ? "Disponible" : "Prestado";

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MaterialBibliograficoExists(material.Id)) return NotFound();
                    else throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(material);
        }

        // ===================== ELIMINAR =====================
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var material = await _context.MaterialesBibliograficos
                .FirstOrDefaultAsync(m => m.Id == id);
            if (material == null) return NotFound();

            bool tienePrestamosActivos = await _context.Prestamos
                .AnyAsync(p => p.MaterialBibliograficoId == id && p.FechaDevolucion == null);

            if (tienePrestamosActivos)
            {
                TempData["ErrorEliminar"] = "⚠️ Este libro tiene préstamos activos. Solo se puede eliminar una unidad, no todas las existencias.";
            }

            return View(material);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, string eliminarCantidad)
        {
            var material = await _context.MaterialesBibliograficos.FindAsync(id);
            if (material == null) return NotFound();

            bool tienePrestamosActivos = await _context.Prestamos
                .AnyAsync(p => p.MaterialBibliograficoId == id && p.FechaDevolucion == null);

            if (tienePrestamosActivos && eliminarCantidad == "todas")
            {
                TempData["ErrorEliminar"] = "🚫 No se pueden eliminar todas las existencias porque hay préstamos activos. Solo podés eliminar una unidad.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            if (eliminarCantidad == "uno")
            {
                if (material.Cantidad <= 1)
                {
                    if (!tienePrestamosActivos)
                        _context.MaterialesBibliograficos.Remove(material);
                    else
                    {
                        TempData["ErrorEliminar"] = "🚫 No podés eliminar la última unidad mientras haya préstamos activos.";
                        return RedirectToAction(nameof(Delete), new { id });
                    }
                }
                else
                {
                    material.Cantidad -= 1;
                }
            }
            else if (eliminarCantidad == "todas")
            {
                if (!tienePrestamosActivos)
                {
                    _context.MaterialesBibliograficos.Remove(material);
                }
                else
                {
                    TempData["ErrorEliminar"] = "🚫 No se pueden eliminar todas las existencias porque hay préstamos activos.";
                    return RedirectToAction(nameof(Delete), new { id });
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "✅ Libro eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private bool MaterialBibliograficoExists(int id)
            => _context.MaterialesBibliograficos.Any(e => e.Id == id);

        // ===================== IMPORTAR EXCEL =====================
        [HttpGet]
        public IActionResult ImportarDesdeExcel() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportarExcel(IFormFile archivoExcel)
        {
            if (archivoExcel == null || archivoExcel.Length <= 0)
            {
                ModelState.AddModelError("", "El archivo es inválido.");
                return View("ImportarDesdeExcel");
            }

            var libros = new List<MaterialBibliografico>();

            try
            {
                using (var stream = new MemoryStream())
                {
                    await archivoExcel.CopyToAsync(stream);

                    using (var workbook = new XLWorkbook(stream))
                    {
                        var hoja = workbook.Worksheets.FirstOrDefault();
                        if (hoja == null)
                        {
                            ModelState.AddModelError("", "No se encontró ninguna hoja en el Excel.");
                            return View("ImportarDesdeExcel");
                        }

                        var ultimaFila = hoja.LastRowUsed()?.RowNumber() ?? 1;
                        if (ultimaFila <= 1)
                        {
                            ModelState.AddModelError("", "El archivo Excel está vacío o no contiene datos.");
                            return View("ImportarDesdeExcel");
                        }

                        for (int fila = 2; fila <= ultimaFila; fila++)
                        {
                            var row = hoja.Row(fila);
                            try
                            {
                                var materiasTexto = row.Cell(6).GetValue<string>();
                                var materiasLista = materiasTexto
                                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(m => ToCamelCase(NormalizarTexto(m)))
                                    .ToList();

                                var libro = new MaterialBibliografico
                                {
                                    NumeroCatalogo = row.Cell(1).GetValue<string>(),
                                    Titulo = ToCamelCase(NormalizarTexto(row.Cell(2).GetValue<string>())),
                                    Autor = ToCamelCase(NormalizarTexto(row.Cell(3).GetValue<string>())),
                                    Editorial = ToCamelCase(NormalizarTexto(row.Cell(4).GetValue<string>())),
                                    AnioEdicion = row.Cell(5).GetValue<int>(),
                                    Materias = materiasLista,
                                    SubmateriaLengua = ToCamelCase(NormalizarTexto(row.Cell(7).GetValue<string>())),
                                    TipoSoporte = ToCamelCase(NormalizarTexto(row.Cell(8).GetValue<string>())),
                                    SubtipoSoporteLibro = ToCamelCase(NormalizarTexto(row.Cell(9).GetValue<string>())),
                                    Cantidad = row.Cell(10).GetValue<int>(),
                                    Procedencia = ToCamelCase(NormalizarProcedencia(row.Cell(11).GetValue<string>())),
                                    Ubicacion = ToCamelCase(NormalizarTexto(row.Cell(12).GetValue<string>())),
                                    Grado = row.Cell(13).GetValue<string>(),
                                    LibroAula = row.Cell(14).GetValue<string>(),
                                    Estado = row.Cell(10).GetValue<int>() > 0 ? "Disponible" : "Prestado",
                                    FechaAlta = DateTime.Now,
                                    FechaBaja = row.Cell(15).IsEmpty() ? null : row.Cell(15).GetValue<DateTime?>()
                                };

                                libros.Add(libro);
                            }
                            catch (Exception exFila)
                            {
                                ModelState.AddModelError("", $"Error en fila {fila}: {exFila.Message}");
                                return View("ImportarDesdeExcel");
                            }
                        }
                    }
                }

                _context.MaterialesBibliograficos.AddRange(libros);
                await _context.SaveChangesAsync();

                TempData["Mensaje"] = $"Se importaron correctamente {libros.Count} libros.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al procesar el Excel: {ex.Message}");
                return View("ImportarDesdeExcel");
            }
        }

        // ===================== MÉTODOS AUXILIARES =====================
        private List<string> ObtenerGrados() => new()
        {
            "Nivel inicial", "1°", "2°", "3°", "4°", "5°", "6°", "7°", "Avanzado",
            "Nivel Primario", "1er ciclo", "2do ciclo", "1er y 2do ciclo", "2do ciclo y 7°"
        };

        private List<string> ObtenerAulas() => new()
        {
            "Ardillitas", "San Martín", "Sarmiento", "Moreno", "Belgrano"
        };

        private string ToCamelCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;
            input = input.Trim();

            return char.ToUpper(input[0]) + input.Substring(1).ToLower();
        }

        private string NormalizarTexto(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            input = input.Trim().Replace("-", " ");
            input = input.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var c in input)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }

        private string NormalizarProcedencia(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            input = NormalizarTexto(input);
            if (input.ToLower().Contains("ministerio")) return "Ministerio de la Nación";
            return input;
        }

        private void ActualizarCampos(MaterialBibliografico original, MaterialBibliografico nuevo)
        {
            original.NumeroCatalogo = nuevo.NumeroCatalogo;
            original.Titulo = nuevo.Titulo;
            original.Autor = nuevo.Autor;
            original.Editorial = nuevo.Editorial;
            original.AnioEdicion = nuevo.AnioEdicion;
            original.Materias = nuevo.Materias;
            original.SubmateriaLengua = nuevo.SubmateriaLengua;
            original.TipoSoporte = nuevo.TipoSoporte;
            original.SubtipoSoporteLibro = nuevo.SubtipoSoporteLibro;
            original.Procedencia = nuevo.Procedencia;
            original.Estado = nuevo.Estado;
            original.Ubicacion = nuevo.Ubicacion;
            original.Grado = nuevo.Grados != null ? string.Join(", ", nuevo.Grados) : null;
            original.LibroAula = nuevo.Aulas != null ? string.Join(", ", nuevo.Aulas) : null;
            original.Cantidad = nuevo.Cantidad;
            original.FechaAlta = nuevo.FechaAlta;
            original.FechaBaja = nuevo.FechaBaja;
        }
        public IActionResult DescargarModeloExcel()
        {
            using (var workbook = new XLWorkbook())
            {
                var hoja = workbook.Worksheets.Add("ModeloLibros");

                // Encabezados
                var columnas = new string[]
                {
            "Autor", "Editorial", "AnioEdicion", "Materia", "SubmateriaLengua",
            "TipoSoporte", "SubtipoSoporteLibro", "Cantidad", "Procedencia", "Ubicacion",
            "Grados", "Aulas", "FechaAlta", "FechaBaja"
                };

                for (int i = 0; i < columnas.Length; i++)
                {
                    hoja.Cell(1, i + 1).Value = columnas[i];
                }

                // Fila 2: tipo de dato o guía
                var tipos = new string[]
                {
            "Texto", "Texto", "Número", "Texto", "Texto",
            "Texto", "Texto", "Número", "Texto", "Texto",
            "Texto, separados por coma", "Texto, separados por comas", "Fecha DD/MM/AAAA", "Fecha DD/MM/AAAA"
                };

                for (int i = 0; i < tipos.Length; i++)
                {
                    hoja.Cell(2, i + 1).Value = tipos[i];
                    hoja.Cell(2, i + 1).Style.Font.FontColor = XLColor.Gray; // opcional: poner en gris
                }

                // Filas de ejemplo posibles de materias, tipos de soporte, grados y aulas
                hoja.Cell(3, 4).Value = "CIENCIAS NATURALES";
                hoja.Cell(3, 6).Value = "LIBROS";
                hoja.Cell(3, 10).Value = "BIBLIOTECA";
                hoja.Cell(3, 11).Value = "NIVEL INICIAL";
                hoja.Cell(3, 12).Value = "ARDILLITAS";


                hoja.Cell(4, 4).Value = "CIENCIAS NATURALES Y TECNOLOGÍA";
                hoja.Cell(4, 6).Value = "MANUAL (VA DENTRO DE LIBROS)";
                hoja.Cell(4, 10).Value = "AULA";
                hoja.Cell(4, 11).Value = "1°";
                hoja.Cell(4, 12).Value = "SAN MARTÍN";

                hoja.Cell(5, 4).Value = "CIENCIAS SOCIALES";
                hoja.Cell(5, 6).Value = "ÁREAS INTEGRADAS (VA DENTRO DE LIBROS)";
                hoja.Cell(5, 11).Value = "2°";
                hoja.Cell(5, 12).Value = "SARMIENTO";

                hoja.Cell(6, 4).Value = "EDUCACIÓN ARTÍSTICA: MÚSICA";
                hoja.Cell(6, 6).Value = "REVISTAS";
                hoja.Cell(6, 11).Value = "3°";
                hoja.Cell(6, 12).Value = "MORENO";

                hoja.Cell(7, 4).Value = "EDUCACIÓN ARTÍSTICA: PLÁSTICA";
                hoja.Cell(7, 6).Value = "ATLAS";
                hoja.Cell(7, 11).Value = "4°";
                hoja.Cell(7, 12).Value = "BELGRANO";

                hoja.Cell(8, 4).Value = "EDUCACIÓN FÍSICA";
                hoja.Cell(8, 6).Value = "ENCICLOPEDIAS";
                hoja.Cell(8, 11).Value = "5°";

                hoja.Cell(9, 4).Value = "EDUCACIÓN SEXUAL INTEGRAL";
                hoja.Cell(9, 6).Value = "VIDEOTECA";
                hoja.Cell(9, 11).Value = "6°";

                hoja.Cell(10, 4).Value = "FORMACIÓN ÉTICA Y CIUDADANA";
                hoja.Cell(10, 6).Value = "MATERIAL DIDÁCTICO";
                hoja.Cell(10, 11).Value = "7°";

                hoja.Cell(11, 4).Value = "HISTORIA";
                hoja.Cell(11, 6).Value = "COLECCIONES";
                hoja.Cell(11, 11).Value = "AVANZADO";

                hoja.Cell(12, 4).Value = "INGLÉS";
                hoja.Cell(12, 6).Value = "DICCIONARIOS";
                hoja.Cell(12, 11).Value = "NIVEL PRIMARIO";

                hoja.Cell(13, 4).Value = "LENGUA";
                hoja.Cell(13, 6).Value = "MAPOTECA (SIN PRÉSTAMO)";
                hoja.Cell(13, 11).Value = "1ER CICLO";

                hoja.Cell(14, 4).Value = "LITERATURA (VA DENTRO DE LENGUA)";
                hoja.Cell(14, 11).Value = "2DO CICLO";

                hoja.Cell(15, 4).Value = "ORTOGRAFÍA (VA DENTRO DE LENGUA)";
                hoja.Cell(15, 11).Value = "1ER Y 2DO CICLO";

                hoja.Cell(16, 4).Value = "MATEMÁTICA";
                hoja.Cell(16, 11).Value = "2DO CICLO Y 7°";

                hoja.Cell(17, 4).Value = "TECNOLOGÍA";

                // Ajuste de ancho de columnas automático
                hoja.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "ModeloLibros.xlsx");
                }
            }
        }
    }
}
