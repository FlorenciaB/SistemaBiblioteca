namespace SistemaBiblioteca.Models
{
    public class UserRolViewModel
    {
        public string? UserId { get; set; }
        public string? Email { get; set; }
        public string? UserName { get; set; }
        public List<string>? Roles { get; set; }
        public bool EstaBloqueado { get; set; }
    }
}
