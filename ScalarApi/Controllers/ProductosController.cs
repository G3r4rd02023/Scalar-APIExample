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
