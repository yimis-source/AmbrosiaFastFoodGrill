// Contexto de EF Core: tablas del negocio e Identity en la misma base, relaciones e índices.
using Ambrosia.Nucleo.Modelos;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Ambrosia.Nucleo.Datos;

public class AmbrosiaDbContext : IdentityDbContext<IdentityUser>
{
    public AmbrosiaDbContext(DbContextOptions<AmbrosiaDbContext> options) : base(options) { }

    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Insumo> Insumos => Set<Insumo>();
    public DbSet<RecetaItem> RecetaItems => Set<RecetaItem>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<DetallePedido> DetallesPedido => Set<DetallePedido>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Categoria>(e =>
        {
            e.HasIndex(c => c.Nombre).IsUnique();
        });

        builder.Entity<Producto>(e =>
        {
            e.HasOne(p => p.Categoria)
             .WithMany(c => c.Productos)
             .HasForeignKey(p => p.CategoriaId)
             .OnDelete(DeleteBehavior.Restrict);
            e.Property(p => p.Precio).HasPrecision(12, 2);
            e.HasIndex(p => p.Nombre).IsUnique();
        });

        builder.Entity<Insumo>(e =>
        {
            e.HasIndex(i => i.Nombre).IsUnique();
            e.Property(i => i.Unidad).HasConversion<string>().HasMaxLength(15);
            e.Property(i => i.StockActual).HasPrecision(12, 2);
            e.Property(i => i.StockMinimo).HasPrecision(12, 2);
            e.Property(i => i.CostoUnitario).HasPrecision(12, 2);
        });

        builder.Entity<RecetaItem>(e =>
        {
            e.HasOne(r => r.Producto)
             .WithMany(p => p.Receta)
             .HasForeignKey(r => r.ProductoId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.Insumo)
             .WithMany()
             .HasForeignKey(r => r.InsumoId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(r => new { r.ProductoId, r.InsumoId }).IsUnique();
            e.Property(r => r.Cantidad).HasPrecision(12, 2);
        });

        builder.Entity<Pedido>(e =>
        {
            e.HasMany(p => p.Detalles)
             .WithOne(d => d.Pedido)
             .HasForeignKey(d => d.PedidoId)
             .OnDelete(DeleteBehavior.Cascade);
            e.Property(p => p.Estado).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.Tipo).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.MetodoPago).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.Subtotal).HasPrecision(12, 2);
            e.Property(p => p.Cargo).HasPrecision(12, 2);
            e.Property(p => p.Total).HasPrecision(12, 2);
            e.HasIndex(p => p.Estado);
            e.HasIndex(p => p.FechaCreacion);
        });

        builder.Entity<DetallePedido>(e =>
        {
            e.HasOne(d => d.Producto)
             .WithMany()
             .HasForeignKey(d => d.ProductoId)
             .OnDelete(DeleteBehavior.Restrict);
            e.Property(d => d.PrecioUnitario).HasPrecision(12, 2);
        });
    }
}
