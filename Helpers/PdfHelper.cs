using iTextSharp.text;
using iTextSharp.text.pdf;
using SistemaBiblioteca.Models;
using System;
using System.IO;

namespace SistemaBiblioteca.Helpers
{
    public static class PdfHelper
    {
        public static byte[] GenerarComprobantePrestamoPdf(Prestamo prestamo)
        {
            using (var ms = new MemoryStream())
            {
                Document doc = new Document(PageSize.A4, 25, 25, 30, 30);
                PdfWriter.GetInstance(doc, ms);
                doc.Open();

                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);

                // Título
                doc.Add(new Paragraph("Comprobante de Préstamo", titleFont));
                doc.Add(new Paragraph(" ")); // espacio

                // Detalles del préstamo
                doc.Add(new Paragraph($"ID Préstamo: {prestamo.Id}", normalFont));
                doc.Add(new Paragraph($"Fecha del préstamo: {prestamo.FechaPrestamo.ToShortDateString()}", normalFont));
                doc.Add(new Paragraph($"Fecha de devolución: {(prestamo.FechaDevolucion.HasValue ? prestamo.FechaDevolucion.Value.ToShortDateString() : "No devuelto")}", normalFont));
                doc.Add(new Paragraph($"Fecha límite de devolución: {prestamo.FechaLimiteDevolucion.ToShortDateString()}", normalFont));
                doc.Add(new Paragraph($"Libro: {prestamo.MaterialBibliografico?.Titulo}", normalFont));
                doc.Add(new Paragraph($"Autor: {prestamo.MaterialBibliografico?.Autor}", normalFont));
                doc.Add(new Paragraph($"Usuario: {prestamo.NombreNino} {prestamo.ApellidoNino}", normalFont));
                doc.Add(new Paragraph($"Grado: {prestamo.GradoNino}", normalFont));

                doc.Close();
                return ms.ToArray();
            }
        }
    }
}
