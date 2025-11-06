using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SistemaBiblioteca.Models;

namespace SistemaBiblioteca.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<MaterialBibliografico> MaterialesBibliograficos { get; set; } = default!;
        public DbSet<Prestamo> Prestamos { get; set; } = default!;
        public DbSet<Materia> Materias { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var materiasConverter = new ValueConverter<List<string>, string>(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
            );

            modelBuilder.Entity<MaterialBibliografico>()
                .Property(m => m.Materias)
                .HasConversion(materiasConverter);
        }
    }
}
