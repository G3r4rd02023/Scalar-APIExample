using Microsoft.EntityFrameworkCore;
using ScalarApi.Models;

namespace ScalarApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Producto> Productos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Datos semilla
            modelBuilder.Entity<Producto>().HasData(
                new Producto { Id = 1, Nombre = "Laptop", Descripcion = "Descripción 1", Precio = 1200.50m, Stock = 10 },
                new Producto { Id = 2, Nombre = "Teclado", Descripcion = "Descripción 2", Precio = 200.75m, Stock = 5 }
            );
        }
    }
}
