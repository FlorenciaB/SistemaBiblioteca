using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaBiblioteca.Models 
{
    public class Materia
    {
        public int Id { get; set; }

        [Required]
        public string? Nombre { get; set; }

        // 🔗 relación con MaterialBibliografico
        [ForeignKey("MaterialBibliografico")]
        public int MaterialBibliograficoId { get; set; }
        public MaterialBibliografico? MaterialBibliografico { get; set; }
    }
}