using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaBiblioteca.Models
{
    public class MaterialBibliografico
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El N° de Catálogo es obligatorio.")]
        [Display(Name = "N° Catálogo")]
        public string? NumeroCatalogo { get; set; }

        [Required(ErrorMessage = "El Título de la obra es obligatorio.")]
        [RegularExpression(@"^[a-zA-Z0-9áéíóúÁÉÍÓÚüÜñÑ .,:;¡!¿?""'()-]+$", ErrorMessage = "Solo se permiten letras.")]
        [Display(Name = "Título de la Obra")]
        public string? Titulo { get; set; }

        [Required(ErrorMessage = "El Autor es obligatorio.")]
        [RegularExpression(@"^[a-zA-Z0-9áéíóúÁÉÍÓÚüÜñÑ .,:;¡!¿?""'()-]+$", ErrorMessage = "Solo se permiten letras.")]
        public string? Autor { get; set; }

        [Required(ErrorMessage = "La Editorial es obligatoria.")]
        public string? Editorial { get; set; }

        [Display(Name = "Año de Edición")]
        [Range(0, int.MaxValue, ErrorMessage = "El año no puede ser negativo.")]
        public int? AnioEdicion { get; set; }

        [Required(ErrorMessage = "La cantidad es obligatoria.")]
        [Range(0, 9999, ErrorMessage = "La cantidad debe ser entre 0 y 9999.")]
        public int? Cantidad { get; set; }

        [Required(ErrorMessage = "La procedencia es obligatoria.")]
        public string? Procedencia { get; set; }

        // ✅ Esta es la propiedad que usará EF Core (lista de materias)
        [Required(ErrorMessage = "Debes seleccionar al menos una materia.")]
        [Display(Name = "Materia/s")]
        [NotMapped]
        public List<string> MateriasSeleccionadas { get; set; } = new List<string>();

        public string? Materias { get; set; }


        public string? SubmateriaLengua { get; set; }

        [Display(Name = "Tipo de Soporte")]
        public string? TipoSoporte { get; set; }

        public string? SubtipoSoporteLibro { get; set; }

        [Display(Name = "Grados o Ciclos")]
        public string? Grado { get; set; }

        [Required(ErrorMessage = "La Ubicación es obligatoria.")]
        public string? Ubicacion { get; set; }

        [Display(Name = "Aulas")]
        public string? LibroAula { get; set; }

        [Required(ErrorMessage = "La Fecha de alta es obligatoria.")]
        [Display(Name = "Fecha de Alta")]
        [DataType(DataType.Date)]
        public DateTime FechaAlta { get; set; }

        [Display(Name = "Fecha de Baja")]
        [DataType(DataType.Date)]
        public DateTime? FechaBaja { get; set; }

        public string Estado { get; set; } = "Disponible";

        // Auxiliares para checkboxes (no se guardan en DB)
        [NotMapped]
        public List<string> Grados { get; set; } = new List<string>();

        [NotMapped]
        public List<string> Aulas { get; set; } = new List<string>();
    }
}
