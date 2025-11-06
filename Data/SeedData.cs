using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistemaBiblioteca.Models;
using System;
using System.Linq;

namespace SistemaBiblioteca.Data
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                if (context.MaterialesBibliograficos.Any())
                {
                    return;
                }

                context.MaterialesBibliograficos.AddRange(
                    new MaterialBibliografico
                    {
                        NumeroCatalogo = "001",
                        Titulo = "El Principito",
                        Autor = "Antoine de Saint-Exupéry",
                        Editorial = "Emecé",
                        AnioEdicion = 1998,
                        Cantidad = 5,
                        Procedencia = "Donación",
                        Materias = new List<string> { "Lengua" },
                        SubmateriaLengua = "Literatura",
                        Grado = "4°,5°,6°",
                        Ubicacion = "Biblioteca",
                        LibroAula = "",
                        TipoSoporte = "Libro",
                        SubtipoSoporteLibro = "Sólo Libro",
                        FechaAlta = DateTime.Now,
                        Estado = "Disponible"
                    },

                    new MaterialBibliografico
                    {
                        NumeroCatalogo = "002",
                        Titulo = "Atlas Geográfico Escolar",
                        Autor = "Kapelusz",
                        Editorial = "Kapelusz",
                        AnioEdicion = 2010,
                        Cantidad = 3,
                        Procedencia = "Compra",
                        Materias = new List<string> { "Lengua" },
                        Grado = "6°,7°",
                        Ubicacion = "Biblioteca",
                        TipoSoporte = "Atlas",
                        FechaAlta = DateTime.Now,
                        Estado = "Disponible"
                    }
                );

                context.SaveChanges();
            }
        }
    }
}
