# ScalarApi — Guía paso a paso de cómo se construyó el proyecto

Este documento describe el proceso completo para crear el proyecto **ScalarApi**: una API REST de ASP.NET Core (.NET 10) con Entity Framework Core (base de datos en memoria), CRUD de productos y documentación interactiva con la UI de **Scalar**.

---

## 1. Creación del proyecto

Se creó la solución y el proyecto usando la plantilla de Web API de .NET:

```bash
dotnet new sln -n ScalarApi
dotnet new webapi -n ScalarApi -o ScalarApi
dotnet sln ScalarApi.slnx add ScalarApi/ScalarApi.csproj
```

La solución quedó definida en `ScalarApi.slnx`:

```xml
<Solution>
  <Project Path="ScalarApi/ScalarApi.csproj" />
</Solution>
```

### Archivos que vienen con la plantilla (no se modificaron)

| Archivo | Descripción |
| --- | --- |
| `Program.cs` | Punto de entrada de la aplicación (se modificó, ver paso 3). |
| `WeatherForecast.cs` | Modelo de ejemplo del endpoint de clima. |
| `Controllers/WeatherForecastController.cs` | Controlador de ejemplo. |
| `Properties/launchSettings.json` | Perfiles de arranque (http/https). |
| `appsettings.json` / `appsettings.Development.json` | Configuración de la aplicación. |

---

## 2. Paquetes NuGet instalados

La plantilla de Web API ya incluye `Microsoft.AspNetCore.OpenApi`. A partir de ahí se agregaron dos paquetes:

```bash
dotnet add package Microsoft.EntityFrameworkCore.InMemory
dotnet add package Scalar.AspNetCore
```

El archivo `ScalarApi.csproj` quedó así:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.11" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.12" />
    <PackageReference Include="Scalar.AspNetCore" Version="2.17.3" />
  </ItemGroup>

  <ItemGroup>
    <Folder Include="Services\" />
  </ItemGroup>

</Project>
```

---

## 3. Configuración de `Program.cs`

Se modificó la plantilla inicial para registrar el contexto de EF Core con la base en memoria y para exponer la UI de Scalar en desarrollo.

**Antes (plantilla):** solo registraba controladores y OpenAPI.

**Después:**

```csharp
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using ScalarApi.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("ProductosDB"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
}

app.Run();
```

Puntos clave:
- `AddDbContext<AppDbContext>` con `UseInMemoryDatabase("ProductosDB")` registra EF Core en memoria.
- `app.MapScalarApiReference()` habilita la UI de Scalar (disponible en `/scalar/v1` desde el navegador en entorno de desarrollo).
- `context.Database.EnsureCreated()` asegura que la base en memoria exista al arrancar (incluye los datos semilla).

---

## 4. Modelo `Models/Producto.cs`

Se creó la entidad que representa un producto. Contenido de `Models/Producto.cs`:

```csharp
namespace ScalarApi.Models
{
    public class Producto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
```

---

## 5. Contexto de base de datos `Data/AppDbContext.cs`

Se creó el `DbContext` de EF Core con la tabla `Productos` y dos registros semilla. Contenido de `Data/AppDbContext.cs`:

```csharp
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
```

---

## 6. DTOs `DTOs/ProductoDto.cs`

Se crearon los DTOs para separar el contrato de la API de la entidad interna. Contenido de `DTOs/ProductoDto.cs`:

```csharp
namespace ScalarApi.DTOs
{
    public class ProductoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int Stock { get; set; }
    }

    public class ProductoCreateDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int Stock { get; set; }
    }

    public class ProductoUpdateDto
    {
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public decimal? Precio { get; set; }
        public int? Stock { get; set; }
    }
}
```

---

## 7. Controlador `Controllers/ProductosController.cs`

Se creó el controlador con el CRUD completo de productos sobre `/api/productos`. Contenido de `Controllers/ProductosController.cs`:

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScalarApi.Data;
using ScalarApi.DTOs;
using ScalarApi.Models;

namespace ScalarApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductosController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductoDto>>> GetProductos()
        {
            var productos = await _context.Productos
                .Select(p => new ProductoDto
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    Descripcion = p.Descripcion,
                    Precio = p.Precio,
                    Stock = p.Stock
                })
                .ToListAsync();

            return Ok(productos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductoDto>> GetProducto(int id)
        {
            var producto = await _context.Productos.FindAsync(id);

            if (producto == null)
                return NotFound($"Producto con ID {id} no encontrado");

            var productoDto = new ProductoDto
            {
                Id = producto.Id,
                Nombre = producto.Nombre,
                Descripcion = producto.Descripcion,
                Precio = producto.Precio,
                Stock = producto.Stock
            };

            return Ok(productoDto);
        }

        [HttpPost]
        public async Task<ActionResult<ProductoDto>> CreateProducto(ProductoCreateDto createDto)
        {
            var producto = new Producto
            {
                Nombre = createDto.Nombre,
                Descripcion = createDto.Descripcion,
                Precio = createDto.Precio,
                Stock = createDto.Stock,
                FechaCreacion = DateTime.UtcNow
            };

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            var productoDto = new ProductoDto
            {
                Id = producto.Id,
                Nombre = producto.Nombre,
                Descripcion = producto.Descripcion,
                Precio = producto.Precio,
                Stock = producto.Stock
            };

            return CreatedAtAction(nameof(GetProducto), new { id = producto.Id }, productoDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProducto(int id, ProductoUpdateDto updateDto)
        {
            var producto = await _context.Productos.FindAsync(id);

            if (producto == null)
                return NotFound($"Producto con ID {id} no encontrado");

            if (!string.IsNullOrEmpty(updateDto.Nombre))
                producto.Nombre = updateDto.Nombre;

            if (!string.IsNullOrEmpty(updateDto.Descripcion))
                producto.Descripcion = updateDto.Descripcion;

            if (updateDto.Precio.HasValue)
                producto.Precio = updateDto.Precio.Value;

            if (updateDto.Stock.HasValue)
                producto.Stock = updateDto.Stock.Value;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProducto(int id)
        {
            var producto = await _context.Productos.FindAsync(id);

            if (producto == null)
                return NotFound($"Producto con ID {id} no encontrado");

            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
```

---

## 8. Endpoints disponibles

| Método | Ruta | Descripción | Códigos de respuesta |
| --- | --- | --- | --- |
| GET | `/api/productos` | Lista todos los productos | 200 |
| GET | `/api/productos/{id}` | Obtiene un producto por ID | 200 / 404 |
| POST | `/api/productos` | Crea un producto | 201 / 400 |
| PUT | `/api/productos/{id}` | Actualiza parcialmente un producto | 204 / 404 |
| DELETE | `/api/productos/{id}` | Elimina un producto | 204 / 404 |
| GET | `/weatherforecast` | Endpoint de ejemplo de la plantilla | 200 |

---

## 9. Cómo ejecutar y usar la UI de Scalar

1. Ejecutar la aplicación:

   ```bash
   dotnet run --project ScalarApi
   ```

2. Abrir en el navegador:

   - **UI de Scalar:** `https://localhost:7205/scalar/v1` (documentación interactiva donde se pueden probar los endpoints).
   - **Documento OpenAPI:** `https://localhost:7205/openapi/v1.json`.

3. Los datos semilla (Laptop y Teclado) se cargan automáticamente al iniciar gracias a `EnsureCreated()` y `HasData()`.