using System.ComponentModel.DataAnnotations;

namespace TuProyecto.Models
{
    public class OlvidePasswordViewModel
    {
        [Required(ErrorMessage = "El email es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingresá un email válido.")]
        public string Email { get; set; }
    }
}
