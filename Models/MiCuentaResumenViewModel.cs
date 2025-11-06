namespace SistemaBiblioteca.Models
{
    public class MiCuentaResumenViewModel
    {
        public string? Email { get; set; }
        public string? UserName { get; set; }
        public List<string>? Roles { get; set; }
        public List<string>? LibrosPrestados { get; set; }
    }
}
