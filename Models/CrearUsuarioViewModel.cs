using System.ComponentModel.DataAnnotations;

namespace SistemaBiblioteca.Models
{
    public class CrearUsuarioViewModel
    {
        [Required(ErrorMessage = "El email es obligatorio.")]
        [EmailAddress(ErrorMessage = "El email no es válido.")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Debés seleccionar un rol.")]
        public string? Rol { get; set; }
    }
}
