using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaBiblioteca.Data;
using SistemaBiblioteca.Helpers;
using System.Threading.Tasks;

namespace SistemaBiblioteca.Controllers
{
    public class ComprobantePrestamoPdfController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ComprobantePrestamoPdfController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Ver(int prestamoId)
        {
            var prestamo = await _context.Prestamos
                .Include(p => p.MaterialBibliografico)
                .FirstOrDefaultAsync(p => p.Id == prestamoId);

            if (prestamo == null)
                return NotFound("Préstamo no encontrado.");

            var pdfBytes = PdfHelper.GenerarComprobantePrestamoPdf(prestamo);

            Response.Headers.Add("Content-Disposition", $"inline; filename=Comprobante_Prestamo_{prestamo.Id}.pdf");

            return File(pdfBytes, "application/pdf");
        }

    }
}
