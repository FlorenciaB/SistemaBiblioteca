using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
            ViewBag.MateriasSeleccionadas = string.Join(", ", material.Materias ?? new List<string>());

            ViewBag.Procedencias = new SelectList(new[]
               {
                    "Ministerio de Educación de la Nación",
                    "Cooperadora",
                    "Donación",
                    "Compra",
                    "Otros"
                }, material.Procedencia);

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

            ViewBag.Procedencias = new SelectList(new[]
            {
                "Ministerio de Educación de la Nación",
                "Cooperadora",
                "Donación",
                "Compra",
                "Otros"
            }, material.Procedencia);

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
                TempData["ErrorEliminar"] = "⚠️ Este libro tiene préstamos activos. NO LO PODÉS ELIMINAR.";
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
            return RedirectToAction(nameof(Index));
        }

        private bool MaterialBibliograficoExists(int id)
            => _context.MaterialesBibliograficos.Any(e => e.Id == id);

        // ===================== IMPORTAR EXCEL =====================
        // GET: ImportarDesdeExcel
        [HttpGet]
        public IActionResult ImportarDesdeExcel()
        {
            return View();
        }

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
                                var numeroCatalogo = row.Cell(1).GetValue<string>()?.Trim();
                                var titulo = row.Cell(2).GetValue<string>()?.Trim();
                                var autor = row.Cell(3).GetValue<string>()?.Trim();
                                var editorial = row.Cell(4).GetValue<string>()?.Trim();

                                var anioTexto = row.Cell(5).GetValue<string>()?.Trim();
                                var anioEdicion = int.TryParse(anioTexto, out int anio) ? anio : 0;

                                var materiasTexto = row.Cell(6).GetValue<string>()?.Trim();
                                var submateriaLengua = row.Cell(7).GetValue<string>()?.Trim();
                                var tipoSoporte = row.Cell(8).GetValue<string>()?.Trim();
                                var subtipoSoporte = row.Cell(9).GetValue<string>()?.Trim();

                                var cantidadTexto = row.Cell(10).GetValue<string>()?.Trim();
                                var cantidad = int.TryParse(cantidadTexto, out int cant) ? cant : 0;

                                var procedencia = row.Cell(11).GetValue<string>()?.Trim();
                                var ubicacion = row.Cell(12).GetValue<string>()?.Trim();
                                var grado = row.Cell(13).GetValue<string>()?.Trim();
                                var aula = row.Cell(14).GetValue<string>()?.Trim();

                                var fechaAlta = row.Cell(15).IsEmpty() ? DateTime.Now : row.Cell(15).GetDateTime();
                                var fechaBaja = row.Cell(16).IsEmpty() ? null : row.Cell(16).GetValue<DateTime?>();

                                // Dividir materias separadas por coma
                                var materiasLista = materiasTexto?
                                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(m => ToCamelCase(NormalizarTexto(m)))
                                    .ToList() ?? new List<string>();

                                var libro = new MaterialBibliografico
                                {
                                    NumeroCatalogo = numeroCatalogo,
                                    Titulo = ToCamelCase(NormalizarTexto(titulo)),
                                    Autor = ToCamelCase(NormalizarTexto(autor)),
                                    Editorial = ToCamelCase(NormalizarTexto(editorial)),
                                    AnioEdicion = anioEdicion,
                                    Materias = materiasLista,
                                    SubmateriaLengua = ToCamelCase(NormalizarTexto(submateriaLengua)),
                                    TipoSoporte = ToCamelCase(NormalizarTexto(tipoSoporte)),
                                    SubtipoSoporteLibro = ToCamelCase(NormalizarTexto(subtipoSoporte)),
                                    Cantidad = cantidad,
                                    Procedencia = ToCamelCase(NormalizarProcedencia(procedencia)),
                                    Ubicacion = ToCamelCase(NormalizarTexto(ubicacion)),
                                    Grado = grado,
                                    LibroAula = aula,
                                    Estado = cantidad > 0 ? "Disponible" : "Prestado",
                                    FechaAlta = fechaAlta,
                                    FechaBaja = fechaBaja
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

            input = NormalizarTexto(input).ToLower();

            if (input.Contains("nacion") || input.Contains("nacional"))
                return "Ministerio de Educación de la Nación";

            if (input.Contains("provincia") || input.Contains("provincial"))
                return "Ministerio de Educación de la Provincia";

            if (input.Contains("cooperadora"))
                return "Cooperadora";

            if (input.Contains("donacion") || input.Contains("donación"))
                return "Donación";

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
                    "N° CATÁLOGO", "Título", "Autor", "Editorial", "Año Edición",
                    "Materia/s", "Sub Materia Lengua", "Soporte", "Subtipo Soporte",
                    "Cantidad", "Procedencia", "Ubicación", "Grado/s", "Aulas",
                    "Fecha Alta", "Fecha Baja"
                 };

                for (int i = 0; i < columnas.Length; i++)
                {
                    hoja.Cell(1, i + 1).Value = columnas[i];
                }

                // Fila 2: tipo de dato o guía
                var tipos = new string[]
                {
                    "Texto", "Texto", "Texto", "Texto", "Número", "Texto", "Texto",
                    "Texto", "Texto", "Número", "Texto", "Texto",
                    "Texto (separar por comas si son varios)", "Texto (separar por comas si son varios)",
                    "Fecha (DD/MM/AAAA)", "Fecha (DD/MM/AAAA, opcional)"
                };

                for (int i = 0; i < tipos.Length; i++)
                {
                    hoja.Cell(2, i + 1).Value = tipos[i];
                    hoja.Cell(2, i + 1).Style.Font.FontColor = XLColor.Gray; // opcional: poner en gris
                }

                // Filas de ejemplo posibles de materias, tipos de soporte, grados y aulas
                hoja.Cell(3, 6).Value = "CIENCIAS NATURALES";
                hoja.Cell(3, 7).Value = "LIBROS";
                hoja.Cell(3, 12).Value = "BIBLIOTECA";
                hoja.Cell(3, 13).Value = "NIVEL INICIAL";
                hoja.Cell(3, 14).Value = "ARDILLITAS";


                hoja.Cell(4, 6).Value = "CIENCIAS NATURALES Y TECNOLOGÍA";
                hoja.Cell(4, 7).Value = "MANUAL (VA DENTRO DE LIBROS)";
                hoja.Cell(4, 12).Value = "BIBLIOTECA DOCENTE";
                hoja.Cell(4, 13).Value = "1°";
                hoja.Cell(4, 14).Value = "SAN MARTÍN";

                hoja.Cell(5, 6).Value = "CIENCIAS SOCIALES";
                hoja.Cell(5, 7).Value = "ÁREAS INTEGRADAS (VA DENTRO DE LIBROS)";
                hoja.Cell(4, 12).Value = "AULA";
                hoja.Cell(5, 13).Value = "2°";
                hoja.Cell(5, 14).Value = "SARMIENTO";

                hoja.Cell(6, 6).Value = "EDUCACIÓN ARTÍSTICA: MÚSICA";
                hoja.Cell(6, 7).Value = "REVISTAS";
                hoja.Cell(6, 13).Value = "3°";
                hoja.Cell(6, 14).Value = "MORENO";

                hoja.Cell(7, 6).Value = "EDUCACIÓN ARTÍSTICA: PLÁSTICA";
                hoja.Cell(7, 7).Value = "ATLAS";
                hoja.Cell(7, 13).Value = "4°";
                hoja.Cell(7, 14).Value = "BELGRANO";

                hoja.Cell(8, 6).Value = "EDUCACIÓN FÍSICA";
                hoja.Cell(8, 7).Value = "ENCICLOPEDIAS";
                hoja.Cell(8, 13).Value = "5°";

                hoja.Cell(9, 6).Value = "EDUCACIÓN SEXUAL INTEGRAL";
                hoja.Cell(9, 7).Value = "VIDEOTECA";
                hoja.Cell(9, 13).Value = "6°";

                hoja.Cell(10, 6).Value = "FORMACIÓN ÉTICA Y CIUDADANA";
                hoja.Cell(10, 7).Value = "MATERIAL DIDÁCTICO";
                hoja.Cell(10, 13).Value = "7°";

                hoja.Cell(11, 6).Value = "HISTORIA";
                hoja.Cell(11, 7).Value = "COLECCIONES";
                hoja.Cell(11, 13).Value = "AVANZADO";

                hoja.Cell(12, 6).Value = "INGLÉS";
                hoja.Cell(12, 7).Value = "DICCIONARIOS";
                hoja.Cell(12, 13).Value = "NIVEL PRIMARIO";

                hoja.Cell(13, 6).Value = "LENGUA";
                hoja.Cell(13, 7).Value = "MAPOTECA (SIN PRÉSTAMO)";
                hoja.Cell(13, 13).Value = "1ER CICLO";

                hoja.Cell(14, 6).Value = "LITERATURA (VA DENTRO DE LENGUA)";
                hoja.Cell(14, 13).Value = "2DO CICLO";

                hoja.Cell(15, 6).Value = "ORTOGRAFÍA (VA DENTRO DE LENGUA)";
                hoja.Cell(15, 13).Value = "1ER Y 2DO CICLO";

                hoja.Cell(16, 6).Value = "MATEMÁTICA";
                hoja.Cell(16, 13).Value = "2DO CICLO Y 7°";

                hoja.Cell(17, 6).Value = "TECNOLOGÍA";

                hoja.Cell(18, 6).Value = "GEOGRAFÍA";

                hoja.Cell(19, 6).Value = "LEGISLACIÓN";

                hoja.Cell(20, 6).Value = "MATERIAL DIRECTIVO";

                hoja.Cell(21, 6).Value = "MATERIAL EDUCATIVO";

                hoja.Cell(22, 6).Value = "INTERÉS GENERAL";

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

        // Obtener cantidad de libros en existencia
        public async Task<IActionResult> Reportes()
        {
            // Total de libros
            var totalLibros = await _context.MaterialesBibliograficos.SumAsync(m => m.Cantidad);

            // Libros por materia
            var librosPorMateria = _context.MaterialesBibliograficos
                .AsEnumerable()
                .SelectMany(m => m.Materias)
                .GroupBy(m => m)
                .Select(g => new { Materia = g.Key, Cantidad = g.Count() })
                .ToList();

            // Libros por procedencia
            var librosPorProcedencia = await _context.MaterialesBibliograficos
                .GroupBy(m => m.Procedencia)
                .Select(g => new { Procedencia = g.Key, Cantidad = g.Sum(m => m.Cantidad) })
                .ToListAsync();

            // Préstamos activos
            var prestamosActivos = await _context.Prestamos.CountAsync(p => p.FechaDevolucion == null);

            // Enviamos todo con ViewBag
            ViewBag.TotalLibros = totalLibros;
            ViewBag.LibrosPorMateria = librosPorMateria;
            ViewBag.LibrosPorProcedencia = librosPorProcedencia;
            ViewBag.PrestamosActivos = prestamosActivos;

            return View();
        }

        //Descargar reporte cantidad en Excel
        public IActionResult ExportarExcel()
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Reporte Biblioteca");

                worksheet.Cell(1, 1).Value = "Reporte General de Biblioteca";
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 14;
                worksheet.Range("A1:D1").Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                // Títulos
                worksheet.Cell(3, 1).Value = "Materia";
                worksheet.Cell(3, 2).Value = "Cantidad";
                worksheet.Cell(3, 3).Value = "Procedencia";
                worksheet.Cell(3, 4).Value = "Cantidad por procedencia";

                var row = 4;

                // Libros por materia
                var librosPorMateria = _context.MaterialesBibliograficos
                    .AsEnumerable()
                    .SelectMany(m => m.Materias)
                    .GroupBy(m => m)
                    .Select(g => new { Materia = g.Key, Cantidad = g.Count() })
                    .ToList();

                foreach (var item in librosPorMateria)
                {
                    worksheet.Cell(row, 1).Value = item.Materia;
                    worksheet.Cell(row, 2).Value = item.Cantidad;
                    row++;
                }

                row = 4;

                // Libros por procedencia
                var porProcedencia = _context.MaterialesBibliograficos
                    .GroupBy(m => m.Procedencia)
                    .Select(g => new { Procedencia = g.Key, Cantidad = g.Sum(m => m.Cantidad) })
                    .ToList();

                foreach (var item in porProcedencia)
                {
                    worksheet.Cell(row, 3).Value = item.Procedencia;
                    worksheet.Cell(row, 4).Value = item.Cantidad;
                    row++;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content,
                                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                "ReporteBiblioteca.xlsx");
                }
            }
        }

        //Descargar reporte cantidad en PDF
        public IActionResult ExportarPDF()
        {
            using (var stream = new MemoryStream())
            {
                Document document = new Document(PageSize.A4, 40, 40, 40, 40);
                PdfWriter.GetInstance(document, stream);
                document.Open();

                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                var sectionFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                var textFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

                document.Add(new Paragraph("📚 Reporte de la Biblioteca\n\n", titleFont));

                // Total libros
                var totalLibros = _context.MaterialesBibliograficos.Sum(m => m.Cantidad);
                var prestamosActivos = _context.Prestamos.Count(p => p.FechaDevolucion == null);
                document.Add(new Paragraph($"Total de libros: {totalLibros}", textFont));
                document.Add(new Paragraph($"Préstamos activos: {prestamosActivos}\n\n", textFont));

                // Tabla por materia
                document.Add(new Paragraph("Libros por materia:", sectionFont));
                var tablaMateria = new PdfPTable(2);
                tablaMateria.AddCell("Materia");
                tablaMateria.AddCell("Cantidad");

                var librosPorMateria = _context.MaterialesBibliograficos
                    .AsEnumerable()
                    .SelectMany(m => m.Materias)
                    .GroupBy(m => m)
                    .Select(g => new { Materia = g.Key, Cantidad = g.Count() })
                    .ToList();

                foreach (var item in librosPorMateria)
                {
                    tablaMateria.AddCell(new Phrase(item.Materia, textFont));
                    tablaMateria.AddCell(new Phrase(item.Cantidad.ToString(), textFont));
                }

                document.Add(tablaMateria);
                document.Add(new Paragraph("\n"));

                // Tabla por procedencia
                document.Add(new Paragraph("Libros por procedencia:", sectionFont));
                var tablaProc = new PdfPTable(2);
                tablaProc.AddCell("Procedencia");
                tablaProc.AddCell("Cantidad");

                var porProcedencia = _context.MaterialesBibliograficos
                    .GroupBy(m => m.Procedencia)
                    .Select(g => new { Procedencia = g.Key, Cantidad = g.Sum(m => m.Cantidad) })
                    .ToList();

                foreach (var item in porProcedencia)
                {
                    tablaProc.AddCell(new Phrase(item.Procedencia ?? "Sin especificar", textFont));
                    tablaProc.AddCell(new Phrase(item.Cantidad.ToString(), textFont));
                }

                document.Add(tablaProc);

                document.Close();
                return File(stream.ToArray(), "application/pdf", "ReporteBiblioteca.pdf");
            }
        }

        //Descargar biblioteca completa en excel
        public IActionResult ExportarBibliotecaCompletaExcel()
        {
            using (var workbook = new XLWorkbook())
            {
                var hoja = workbook.Worksheets.Add("Biblioteca Completa");

                // ENCABEZADOS
                string[] columnas =
                {
            "ID", "N° Catálogo", "Título", "Autor", "Editorial", "Año Edición",
            "Materias", "Submateria Lengua", "Soporte", "Subtipo Soporte",
            "Cantidad", "Estado", "Procedencia", "Ubicación",
            "Grados", "Aulas", "Fecha Alta", "Fecha Baja"
            };

                for (int c = 0; c < columnas.Length; c++)
                {
                    hoja.Cell(1, c + 1).Value = columnas[c];
                    hoja.Cell(1, c + 1).Style.Font.Bold = true;
                }

                // OBTENER DATOS DE LA BD
                var materiales = _context.MaterialesBibliograficos.ToList();

                int row = 2;

                foreach (var m in materiales)
                {
                    hoja.Cell(row, 1).Value = m.Id;
                    hoja.Cell(row, 2).Value = m.NumeroCatalogo;
                    hoja.Cell(row, 3).Value = m.Titulo;
                    hoja.Cell(row, 4).Value = m.Autor;
                    hoja.Cell(row, 5).Value = m.Editorial;
                    hoja.Cell(row, 6).Value = m.AnioEdicion;

                    // Materias (lista → texto)
                    hoja.Cell(row, 7).Value = m.Materias != null ? string.Join(", ", m.Materias) : "";

                    hoja.Cell(row, 8).Value = m.SubmateriaLengua;
                    hoja.Cell(row, 9).Value = m.TipoSoporte;
                    hoja.Cell(row, 10).Value = m.SubtipoSoporteLibro;
                    hoja.Cell(row, 11).Value = m.Cantidad;
                    hoja.Cell(row, 12).Value = m.Estado;
                    hoja.Cell(row, 13).Value = m.Procedencia;
                    hoja.Cell(row, 14).Value = m.Ubicacion;
                    hoja.Cell(row, 15).Value = m.Grado;
                    hoja.Cell(row, 16).Value = m.LibroAula;

                    hoja.Cell(row, 17).Value = m.FechaAlta.ToString("dd/MM/yyyy");
                    hoja.Cell(row, 18).Value = m.FechaBaja?.ToString("dd/MM/yyyy");

                    row++;
                }

                hoja.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);

                    Response.Cookies.Append("excelReady", "1", new CookieOptions
                    {
                        Expires = DateTime.Now.AddSeconds(5),
                        Path = "/"
                    });

                    return File(
                        stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "BibliotecaCompleta.xlsx"
                    );
                }
            }
        }
    }
}
