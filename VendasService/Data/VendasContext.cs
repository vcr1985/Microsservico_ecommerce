using Microsoft.EntityFrameworkCore;
using VendasService.Models;

namespace VendasService.Data
{
    public class VendasContext : DbContext
    {
        public VendasContext(DbContextOptions<VendasContext> options) : base(options)
        {
        }

        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<ItemPedido> ItensPedido { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Pedido>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ClienteId).IsRequired();
                entity.Property(e => e.ValorTotal).HasPrecision(18, 2);
                entity.Property(e => e.DataPedido).IsRequired();

                entity.HasMany(e => e.Itens)
                      .WithOne(e => e.Pedido)
                      .HasForeignKey(e => e.PedidoId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ItemPedido>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ProdutoId).IsRequired();
                entity.Property(e => e.Quantidade).IsRequired();
                entity.Property(e => e.PrecoUnitario).HasPrecision(18, 2);
                entity.Property(e => e.NomeProduto).IsRequired().HasMaxLength(200);
            });
        }
    }
}