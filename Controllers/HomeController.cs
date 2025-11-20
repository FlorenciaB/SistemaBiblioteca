using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaBiblioteca.Data;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string titulo, string autor, string materias, string grado, string estado, int? cantidadMin)
    {
        var query = _context.MaterialesBibliograficos.AsQueryable();

        if (!string.IsNullOrEmpty(titulo))
            query = query.Where(m => m.Titulo.Contains(titulo));

        if (!string.IsNullOrEmpty(autor))
            query = query.Where(m => m.Autor.Contains(autor));

        if (!string.IsNullOrEmpty(materias))
            query = query.Where(m => m.Materias.Contains(materias));

        if (!string.IsNullOrEmpty(estado))
            query = query.Where(m => m.Estado == estado);

        if (cantidadMin.HasValue)
            query = query.Where(m => m.Cantidad >= cantidadMin.Value);

        var librosFiltrados = await query.ToListAsync();

        ViewBag.Materias = await _context.Materias
        .Select(m => m.Nombre)
        .ToListAsync();

        ViewBag.MateriaSeleccionada = materias;


        return View(librosFiltrados);
    }
}
