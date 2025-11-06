using System.ComponentModel.DataAnnotations;

namespace SistemaBiblioteca.Models
{
    public class CambiarPasswordViewModel
    {
        [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
        [DataType (DataType.Password)]
        public string? NuevaPassword { get; set; }

        [Required(ErrorMessage = "Debe confirmar la nueva contraseña.")]
        [Compare("NuevaPassword", ErrorMessage = "Las contraseñas no coinciden.")]
        [DataType(DataType.Password)]
        public string? ConfirmarPassword { get; set; }
    }
}
