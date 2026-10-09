using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace BarberiaTurnos.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Barbero> Barberos => Set<Barbero>();
    public DbSet<Servicio> Servicios => Set<Servicio>();
    public DbSet<Turno> Turnos => Set<Turno>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<DetalleVenta> DetallesVenta => Set<DetalleVenta>();
    public DbSet<ConfiguracionSistema> Configuraciones => Set<ConfiguracionSistema>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<ApplicationUser>().Property(u => u.NombreCompleto).HasMaxLength(100);

        b.Entity<Cliente>(e =>
        {
            e.HasIndex(c => c.Nombre);
        });

        b.Entity<Barbero>(e =>
        {
            e.HasIndex(x => x.Nombre);
        });

        b.Entity<Servicio>(e =>
        {
            e.HasIndex(s => s.Nombre).IsUnique();
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Servicios_Precio", "[Precio] > 0");
                t.HasCheckConstraint("CK_Servicios_Duracion", "[DuracionMinutos] > 0");
            });
        });

        b.Entity<Producto>(e =>
        {
            e.HasIndex(p => p.Nombre).IsUnique();
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Productos_Precio", "[Precio] > 0");
                t.HasCheckConstraint("CK_Productos_Cantidad", "[Cantidad] >= 0");
                t.HasCheckConstraint("CK_Productos_StockMinimo", "[StockMinimo] >= 0");
            });
        });

        b.Entity<Turno>(e =>
        {
            e.HasOne(t => t.Cliente).WithMany(c => c.Turnos).HasForeignKey(t => t.ClienteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.Barbero).WithMany(x => x.Turnos).HasForeignKey(t => t.BarberoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.Servicio).WithMany().HasForeignKey(t => t.ServicioId).OnDelete(DeleteBehavior.Restrict);
            e.Property(t => t.Estado).HasConversion<int>();
            e.HasIndex(t => new { t.BarberoId, t.FechaHoraInicio });
            e.HasIndex(t => t.FechaHoraInicio);
        });

        b.Entity<Venta>(e =>
        {
            e.HasIndex(v => v.TokenOperacion).IsUnique();
            e.HasIndex(v => v.Fecha);
            e.HasOne(v => v.Usuario).WithMany().HasForeignKey(v => v.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(v => v.Cliente).WithMany(c => c.Ventas).HasForeignKey(v => v.ClienteId).OnDelete(DeleteBehavior.Restrict);
            e.Property(v => v.PorcentajeImpuesto).HasPrecision(5, 2);
        });

        b.Entity<DetalleVenta>(e =>
        {
            e.HasOne(d => d.Venta).WithMany(v => v.Detalles).HasForeignKey(d => d.VentaId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(d => d.Servicio).WithMany().HasForeignKey(d => d.ServicioId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(d => d.Producto).WithMany().HasForeignKey(d => d.ProductoId).OnDelete(DeleteBehavior.Restrict);
            e.Property(d => d.Tipo).HasConversion<int>();
            e.ToTable(t => t.HasCheckConstraint("CK_DetallesVenta_Cantidad", "[Cantidad] > 0"));
        });

        b.Entity<ConfiguracionSistema>(e =>
        {
            e.Property(c => c.PorcentajeImpuesto).HasPrecision(5, 2);
        });
    }
}
