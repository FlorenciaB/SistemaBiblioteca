using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaBiblioteca.Models
{
    public class Prestamo
    {
        public int Id { get; set; }

        [Required]
        public int MaterialBibliograficoId { get; set; }

        [ForeignKey("MaterialBibliograficoId")]
        public MaterialBibliografico? MaterialBibliografico { get; set; }

        [Required]
        public string? UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public IdentityUser? Usuario { get; set; }

        [Required(ErrorMessage = "Debes ingresar el nombre del Alumno.")]
        [Display(Name = "Nombre del alumno")]
        [RegularExpression("^[a-zA-ZáéíóúÁÉÍÓÚüÜñÑ ]+$", ErrorMessage = "Solo se permiten letras.")]
        public string NombreNino { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debes ingresar el apellido del Alumno.")]
        [Display(Name = "Apellido del alumno")]
        [RegularExpression("^[a-zA-ZáéíóúÁÉÍÓÚüÜñÑ ]+$", ErrorMessage = "Solo se permiten letras.")]
        public string ApellidoNino { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debes seleccionar un grado.")]
        [Display(Name = "Grado del alumno")]
        public string GradoNino { get; set; } = string.Empty;

        public DateTime FechaPrestamo { get; set; } = DateTime.Now;

        public DateTime FechaLimiteDevolucion { get; set; } = DateTime.Now.AddDays(15);

        public DateTime? FechaDevolucion { get; set; }

        public bool Devuelto { get; set; }
    }
}