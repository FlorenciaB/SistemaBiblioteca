using System.ComponentModel.DataAnnotations;

namespace SistemaBiblioteca.Models
{
    public class EditarPerfilViewModel
    {
        [Required]
        [EmailAddress]
        public string? Email { get; set; }
    }
}
